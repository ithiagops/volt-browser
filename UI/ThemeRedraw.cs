using System.Runtime.InteropServices;

namespace Volt;

// Freeze only already-visible native controls, never an embedded browser controller.
sealed class ThemeRedraw : IDisposable
{
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hwnd,int msg,IntPtr w,IntPtr l);
    [DllImport("user32.dll")] static extern bool RedrawWindow(IntPtr hwnd,IntPtr rect,IntPtr region,uint flags);
    [DllImport("dwmapi.dll")] static extern int DwmFlush();
    sealed class ThemeCover : Form
    {
        public ThemeCover() { DoubleBuffered=true; }
        protected override bool ShowWithoutActivation=>true;
        protected override CreateParams CreateParams
        {
            get { var p=base.CreateParams;p.ExStyle|=0x08000000|0x00000080;return p; }
        }
    }
    readonly ThemeCover? cover;
    readonly Bitmap? snapshot;
    readonly Control root;
    readonly List<Control> controls=[];
    public ThemeRedraw(Control root, Func<Control,bool>? exclude = null)
    {
        this.root=root;
        // A temporary independent compositor surface covers the native child-window repaints.
        // Capture stays in RAM only and is disposed at the end of this synchronous switch.
        if(root is Form owner && owner.Visible && owner.WindowState!=FormWindowState.Minimized
            && root.ClientSize.Width>0 && root.ClientSize.Height>0)
        {
            Bitmap? captured=null;ThemeCover? overlay=null;
            try
            {
                var bounds=new Rectangle(root.PointToScreen(Point.Empty),root.ClientSize);
                captured=new Bitmap(bounds.Width,bounds.Height);
                using(var g=Graphics.FromImage(captured))g.CopyFromScreen(bounds.Location,Point.Empty,bounds.Size,CopyPixelOperation.SourceCopy);
                overlay=new ThemeCover {FormBorderStyle=FormBorderStyle.None,ShowInTaskbar=false,StartPosition=FormStartPosition.Manual,
                    AutoScaleMode=AutoScaleMode.None,Bounds=bounds,BackgroundImage=captured,BackgroundImageLayout=ImageLayout.None};
                overlay.Show(owner);overlay.Update();DwmFlush();
                snapshot=captured;cover=overlay;
            }
            catch(Exception ex) when(ex is System.ComponentModel.Win32Exception or ArgumentException or System.Runtime.InteropServices.ExternalException)
            {overlay?.Dispose();captured?.Dispose();}
        }
        void Visit(Control c)
        {
            if(c is BrowserView || exclude?.Invoke(c)==true) return;
            if(c.IsHandleCreated && c.Visible) controls.Add(c);
            foreach(Control child in c.Controls) Visit(child);
        }
        Visit(root);
        root.SuspendLayout();
        foreach(var c in controls) SendMessage(c.Handle,0x000B,IntPtr.Zero,IntPtr.Zero);
    }
    public void Dispose()
    {
        try
        {
            root.ResumeLayout(true);
            foreach(var c in controls.AsEnumerable().Reverse())
                if(!c.IsDisposed && c.IsHandleCreated) SendMessage(c.Handle,0x000B,(IntPtr)1,IntPtr.Zero);
            if(!root.IsDisposed && root.IsHandleCreated)
            {
                RedrawWindow(root.Handle,IntPtr.Zero,IntPtr.Zero,0x185);
                DwmFlush();
            }
        }
        finally
        {
            if(cover is not null){cover.BackgroundImage=null;cover.Dispose();}
            snapshot?.Dispose();
        }
    }
}

sealed class CenteredTextHost : Panel
{
    readonly TextBox text;
    public CenteredTextHost(TextBox text)
    {
        this.text=text; DoubleBuffered=true;
        text.Dock=DockStyle.None; text.AutoSize=true;
        Controls.Add(text);
        text.FontChanged+=(_,_)=>PerformLayout();
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        if(text is null) return;
        int height=text.PreferredHeight;
        text.SetBounds(Padding.Left,Padding.Top+Math.Max(0,(ClientSize.Height-Padding.Vertical-height)/2),
            Math.Max(1,ClientSize.Width-Padding.Horizontal),height);
    }
    protected override void OnDpiChangedAfterParent(EventArgs e){base.OnDpiChangedAfterParent(e);PerformLayout();}
}
