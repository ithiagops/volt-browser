using System.Runtime.InteropServices;
namespace Volt;

sealed partial class BrowserWindow
{
    readonly RestoreRefreshGate restoreRefresh=new();
    bool restoreWasMinimized;
    [DllImport("user32.dll")] static extern bool RedrawWindow(IntPtr window,IntPtr rect,IntPtr region,uint flags);
    bool CanRefreshWindow => !closing && !IsDisposed && IsHandleCreated && Visible && WindowState!=FormWindowState.Minimized;
    void RequestRestoreRefresh()
    {
        if(!restoreRefresh.TrySchedule(CanRefreshWindow,out long ticket))return;
        try
        {
            BeginInvoke(new Action(()=>
            {
                if(!restoreRefresh.TryComplete(ticket,CanRefreshWindow))return;
                // Re-read native geometry after WM_SIZE/DPI/activation have settled.
                // Never restore a screenshot or re-enable redraw on hidden pages.
                UpdateBounds();RecoverWindowPlacement();
                if(!CanRefreshWindow || ClientSize.Width<1 || ClientSize.Height<1)return;
                void LayoutVisible(Control control)
                {
                    control.PerformLayout();
                    foreach(Control child in control.Controls)if(child.Visible && child is not BrowserView)LayoutVisible(child);
                }
                LayoutVisible(this);browserTabHeader?.RefreshAfterRestore();
                if(Current is { } tab)
                {
                    foreach(var home in tab.Page.Controls.OfType<HomePage>())home.RefreshAfterRestore();
                    if(tab.View is { } web){web.SetPageVisible(true,true);web.Invalidate();}
                }
                if(whatsAppPanel.Visible && whatsAppView is { } whatsapp)whatsapp.SetPageVisible(true,true);
                Invalidate(true);
                // Erase, invalidate and repaint the frame and all visible native children.
                RedrawWindow(Handle,IntPtr.Zero,IntPtr.Zero,0x585);
            }));
        }
        catch(InvalidOperationException){restoreRefresh.Cancel();}
    }
    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);RequestRestoreRefresh();
    }
}
