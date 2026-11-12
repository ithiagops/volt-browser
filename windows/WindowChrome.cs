using System.Runtime.InteropServices;
using System.Text.Json;

namespace Volt;

sealed partial class BrowserWindow
{
    [DllImport("user32.dll")] static extern bool ReleaseCapture();
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr w, IntPtr l);
    readonly FlowLayoutPanel bookmarkBar = new() { Dock=DockStyle.Top, Height=28, Padding=new(10,0,10,0), WrapContents=false, AutoScroll=true, Tag="chrome" };
    readonly List<SavedLink> bookmarks = new();
    record SavedLink(string Title, string Url);
    string BookmarkFile => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"VOLT","bookmarks.json");
    void InitializeBookmarks()
    {
        try { if(File.Exists(BookmarkFile)) bookmarks.AddRange(JsonSerializer.Deserialize<List<SavedLink>>(File.ReadAllText(BookmarkFile)) ?? []); }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or JsonException) { }
        RenderBookmarks();
    }
    void RenderBookmarks()
    {
        foreach(Control c in bookmarkBar.Controls.Cast<Control>().ToArray()) c.Dispose();
        foreach(var item in bookmarks)
        {
            if(!ResourcePolicy.IsHttp(item.Url)) continue;
            var b = new ChromeButton {Text=item.Title,Kind="",Height=24,Width=Math.Clamp(TextRenderer.MeasureText(item.Title,Font).Width+18,60,170),Font=new("Segoe UI",8.5f),Margin=new(0,0,6,0),Tag="chrome",AccessibleName=item.Title};
            b.Click+=(_,_)=>{ if(Current is { } tab) _=NavigateTab(tab,item.Url); };
            var context=new ContextMenuStrip(); context.Items.Add("Remover favorito",null,(_,_)=>{bookmarks.Remove(item);SaveBookmarks();RenderBookmarks();});
            b.ContextMenuStrip=context;b.Disposed+=(_,_)=>context.Dispose();
            bookmarkBar.Controls.Add(b);BrowserTheme.Apply(b,darkMode);
        }
        if(bookmarks.Count==0)
            bookmarkBar.Controls.Add(new Label {Text="Seus favoritos ficam aqui. Use o coração para salvar uma página.",AutoSize=true,Font=new("Segoe UI",8.5f),ForeColor=Color.FromArgb(112,127,145),Margin=new(3,5,0,0)});
        BrowserTheme.Apply(bookmarkBar,darkMode);
    }
    void SaveBookmarks()
    {
        try { Directory.CreateDirectory(Path.GetDirectoryName(BookmarkFile)!);File.WriteAllText(BookmarkFile,JsonSerializer.Serialize(bookmarks)); }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){MessageBox.Show(this,"Não foi possível salvar os favoritos.","VOLT");}
    }
    void AddBookmark()
    {
        if(Current is not { Url: { } url } tab || !ResourcePolicy.IsHttp(url)) return;
        if(bookmarks.Any(x=>x.Url==url)) return;
        bookmarks.Add(new(tab.Title,url));SaveBookmarks();RenderBookmarks();
    }
    void ToggleMaximize()
    {
        MaximizedBounds=Screen.FromHandle(Handle).WorkingArea;
        WindowState=WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized;
    }
    void DragWindow()
    {
        ReleaseCapture();SendMessage(Handle,0xA1,(IntPtr)2,IntPtr.Zero);
    }
    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if(m.Msg!=0x84 || WindowState!=FormWindowState.Normal || (int)m.Result!=1) return;
        long raw=m.LParam.ToInt64();var pt=PointToClient(new Point((short)(raw&0xffff),(short)((raw>>16)&0xffff)));
        int edge=(int)Math.Ceiling(6*DeviceDpi/96f);bool l=pt.X<edge,r=pt.X>=ClientSize.Width-edge,t=pt.Y<edge,b=pt.Y>=ClientSize.Height-edge;
        m.Result=(IntPtr)(t?(l?13:r?14:12):b?(l?16:r?17:15):l?10:r?11:1);
    }
}
