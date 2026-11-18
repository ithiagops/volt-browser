using System.Runtime.CompilerServices;
namespace Volt;

// Batch layout only. Never capture the desktop, create an overlay window, or change
// WM_SETREDRAW: those operations can preserve stale pixels/restore hidden child HWNDs.
sealed class VisualUpdate : IDisposable
{
    sealed class Batch {public int Depth;}
    static readonly ConditionalWeakTable<Control,Batch> batches=new();
    readonly Control root;
    readonly Batch batch;
    bool disposed;
    public VisualUpdate(Control root,Func<Control,bool>? exclude=null)
    {
        this.root=root;batch=batches.GetOrCreateValue(root);
        if(batch.Depth++==0)root.SuspendLayout();
    }
    public void Dispose()
    {
        if(disposed)return;disposed=true;
        if(--batch.Depth!=0 || root.IsDisposed)return;
        root.ResumeLayout(true);root.Invalidate(true);
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
