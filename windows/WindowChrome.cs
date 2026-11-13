using System.Runtime.InteropServices;
using System.Text.Json;

namespace Volt;

sealed partial class BrowserWindow
{
    [DllImport("user32.dll")] static extern bool ReleaseCapture();
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr w, IntPtr l);
    BrowserTabsHeader? browserTabHeader;
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
        RefreshAddressSuggestions();
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
        WindowState=WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized;
    }
    void DragWindow()
    {
        BeginWindowDrag(this);
    }
    static void BeginWindowDrag(Form window)
    {
        var cursor=Cursor.Position;ReleaseCapture();
        SendMessage(window.Handle,0xA1,(IntPtr)2,(IntPtr)WindowPlacement.PackPoint(cursor));
    }
    // Keep the shell's resizing/snap contract while painting our own title bar.
    protected override CreateParams CreateParams
    {
        get {var p=base.CreateParams;p.Style|=0x00C00000|0x00040000|0x00080000|0x00020000|0x00010000;return p;}
    }
    [StructLayout(LayoutKind.Sequential)] struct NativePoint {public int X,Y;}
    [StructLayout(LayoutKind.Sequential)] struct MinMaxInfo {public NativePoint Reserved,MaxSize,MaxPosition,MinTrackSize,MaxTrackSize;}
    [StructLayout(LayoutKind.Sequential)] struct NativeRect {public int Left,Top,Right,Bottom;public Rectangle Rect=>Rectangle.FromLTRB(Left,Top,Right,Bottom);}
    [StructLayout(LayoutKind.Sequential)] struct MonitorInfo {public int Size;public NativeRect Monitor,Work;public uint Flags;}
    [DllImport("user32.dll")] static extern bool IsZoomed(IntPtr window);
    [DllImport("user32.dll")] static extern IntPtr MonitorFromRect(ref NativeRect rect,uint flags);
    [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr window,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Auto)] static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
    void InitializeWindowPlacement()
    {
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged+=DisplaysChanged;
        FormClosed+=(_,_)=>Microsoft.Win32.SystemEvents.DisplaySettingsChanged-=DisplaysChanged;
        Shown+=(_,_)=>RecoverWindowPlacement();ResizeEnd+=(_,_)=>RecoverWindowPlacement();
    }
    void DisplaysChanged(object? sender,EventArgs e)
    {if(!closing && IsHandleCreated)try{BeginInvoke(new Action(RecoverWindowPlacement));}catch(InvalidOperationException){}}
    void RecoverWindowPlacement()
    {
        if(closing || WindowState==FormWindowState.Minimized)return;
        var screens=Screen.AllScreens;var preferred=Screen.FromPoint(Cursor.Position).WorkingArea;
        var safe=WindowPlacement.EnsureVisible(Bounds,screens.Select(s=>s.WorkingArea),preferred);
        if(safe==Bounds)return;
        WindowState=FormWindowState.Normal;Bounds=safe;
    }
    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        if(IsHandleCreated && !closing)BeginInvoke(new Action(RecoverWindowPlacement));
    }
    protected override void WndProc(ref Message m)
    {
        if(m.Msg==0x83)
        {
            // Maximized thick frames can extend outside the desktop. Keep all custom
            // chrome inside the monitor work area instead of painting into that overscan.
            if(m.WParam!=IntPtr.Zero && m.LParam!=IntPtr.Zero && IsZoomed(m.HWnd))
            {
                var proposed=Marshal.PtrToStructure<NativeRect>(m.LParam);
                var info=new MonitorInfo {Size=Marshal.SizeOf<MonitorInfo>()};
                if(GetMonitorInfo(MonitorFromRect(ref proposed,2),ref info))
                {
                    var client=WindowPlacement.MaximizedClient(proposed.Rect,info.Work.Rect);
                    proposed.Left=client.Left;proposed.Top=client.Top;proposed.Right=client.Right;proposed.Bottom=client.Bottom;
                    Marshal.StructureToPtr(proposed,m.LParam,false);
                }
            }
            m.Result=IntPtr.Zero;return;
        }
        base.WndProc(ref m);
        if(m.Msg==0x24 && m.LParam!=IntPtr.Zero)
        {
            var info=new MonitorInfo {Size=Marshal.SizeOf<MonitorInfo>()};
            if(GetMonitorInfo(MonitorFromWindow(Handle,2),ref info))
            {
                var limits=Marshal.PtrToStructure<MinMaxInfo>(m.LParam);
                var max=WindowPlacement.Maximized(info.Monitor.Rect,info.Work.Rect);
                limits.MaxPosition=new NativePoint {X=max.X,Y=max.Y};limits.MaxSize=new NativePoint {X=max.Width,Y=max.Height};
                // Half-screen snap must remain possible even on smaller or scaled monitors.
                limits.MinTrackSize.X=Math.Min(limits.MinTrackSize.X,Math.Max(1,info.Work.Rect.Width/2));
                limits.MinTrackSize.Y=Math.Min(limits.MinTrackSize.Y,Math.Max(1,info.Work.Rect.Height/2));
                Marshal.StructureToPtr(limits,m.LParam,false);m.Result=IntPtr.Zero;
            }
            return;
        }
        if(m.Msg!=0x84 || WindowState!=FormWindowState.Normal || (int)m.Result!=1) return;
        long raw=m.LParam.ToInt64();var pt=PointToClient(new Point((short)(raw&0xffff),(short)((raw>>16)&0xffff)));
        int edge=(int)Math.Ceiling(6*DeviceDpi/96f);bool l=pt.X<edge,r=pt.X>=ClientSize.Width-edge,t=pt.Y<edge,b=pt.Y>=ClientSize.Height-edge;
        // Keep the outer resize border, but do not let its inner padding steal tab presses.
        if(t && !l && !r && pt.Y>=Padding.Top && browserTabHeader?.HasTabAtScreenPoint(PointToScreen(pt))==true)t=false;
        m.Result=(IntPtr)(t?(l?13:r?14:12):b?(l?16:r?17:15):l?10:r?11:1);
    }
}
