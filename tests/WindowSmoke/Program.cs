using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Net;
using System.Net.Sockets;
using Volt;

internal static class WindowSmoke
{
    [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr window,out Rect rect);
    [StructLayout(LayoutKind.Sequential)] struct Rect{public int Left,Top,Right,Bottom;}
    static readonly List<string> checks=[];
    static string output="";
    static BrowserWindow window=null!;
    static IEnumerable<Control> Descendants(Control root){foreach(Control c in root.Controls){yield return c;foreach(var nested in Descendants(c))yield return nested;}}
    static void Check(bool ok,string label){if(!ok)throw new InvalidOperationException(label);checks.Add(label);}
    static object? Call(string name,params object?[] args)=>typeof(BrowserWindow).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(window,args);
    static async Task Add(string? url=null)=>await (Task)Call("AddTab",new object?[]{url})!;
    static HoverTabs Host=>Descendants(window).OfType<HoverTabs>().Single();
    static async Task Wait(Func<bool> ready)
    {var until=DateTime.UtcNow.AddSeconds(25);while(!ready()){if(DateTime.UtcNow>until)throw new TimeoutException("Janela ou página não estabilizou.");await Task.Delay(60);}}
    static Bitmap Capture()
    {
        var rect=window.RectangleToScreen(window.ClientRectangle);
        var image=new Bitmap(rect.Width,rect.Height);using var g=Graphics.FromImage(image);g.CopyFromScreen(rect.Location,Point.Empty,rect.Size);return image;
    }
    static void Frame(string name,bool screenshot=false)
    {
        Check(GetClientRect(window.Handle,out var native)&&native.Right==window.ClientSize.Width&&native.Bottom==window.ClientSize.Height,name+": client cache matches HWND");
        Check(Host.SelectedTab is not null && Host.TabPages.Count(p=>p.Visible)==1,name+": one visible page");
        var page=Host.SelectedTab!;Check(page.Bounds==Host.DisplayRectangle,name+": page fills managed host");
        using var image=Capture();
        if(page.Controls.OfType<HomePage>().FirstOrDefault() is { } home)
        {
            var location=window.PointToClient(home.PointToScreen(Point.Empty));
            foreach(int x in new[]{12,Math.Max(12,home.Width-13)})
            foreach(int y in new[]{12,Math.Max(12,home.Height/3),Math.Max(12,home.Height-13)})
            {
                var pixel=image.GetPixel(Math.Clamp(location.X+x,0,image.Width-1),Math.Clamp(location.Y+y,0,image.Height-1));
                bool good=home.Dark?pixel.R>8&&pixel.B>20&&pixel.B<110:pixel.R>90&&pixel.G>90&&pixel.B>90&&pixel.R<252;
                Check(good,name+": actual screen has no black/white stale home blocks");
            }
            var footer=Descendants(window).OfType<Label>().Single(l=>l.Text.StartsWith("VOLT 0.6."));
            Check(home.Dark?footer.ForeColor.GetBrightness()>0.65:footer.ForeColor.GetBrightness()<0.45,name+": footer text follows theme contrast");
            var bookmarkHint=Descendants(window).OfType<Label>().Single(l=>l.Text.StartsWith("Seus favoritos ficam aqui"));
            Check(home.Dark?bookmarkHint.BackColor.GetBrightness()<0.3:bookmarkHint.BackColor.GetBrightness()>0.7,name+": inherited label background follows theme");
            var search=home.Controls.OfType<RoundedSurface>().Single();Check(home.ClientRectangle.Contains(search.Bounds),name+": search is inside viewport");
        }
        if(screenshot)image.Save(Path.Combine(output,name+".png"),ImageFormat.Png);
    }
    static async Task Run()
    {
        await Task.Delay(250);window.TopMost=true;window.Activate();await Task.Delay(150);
        Frame("initial",true);
        // Same native commands as the taskbar: minimized -> maximized / restored.
        for(int i=0;i<12;i++)
        {
            window.WindowState=FormWindowState.Minimized;await Task.Delay(80);
            window.WindowState=i%2==0?FormWindowState.Maximized:FormWindowState.Normal;
            window.Activate();await Task.Delay(180);Frame("restore-"+i,i==11);
        }
        var selected=Host.SelectedTab!;var pageHandle=selected.Handle;
        for(int i=0;i<8;i++)await Add();await Task.Delay(220);Frame("tabs-open",true);
        var active=Host.SelectedTab!;var activeHandle=active.Handle;
        Call("MoveTab",active,0);await Task.Delay(220);
        Check(Host.SelectedTab==active&&active.Handle==activeHandle,"reorder keeps active handle attached");Frame("tabs-reorder");
        for(int i=0;i<6;i++)Call("CloseTab",new object?[]{null});await Task.Delay(220);Frame("tabs-close",true);
        var theme=Descendants(window).OfType<Button>().Single(b=>b.AccessibleName is "Ativar modo claro" or "Ativar modo escuro");
        for(int i=0;i<4;i++){theme.PerformClick();await Task.Delay(130);Frame("theme-"+i,i==0);}
        var bounds=Host.Bounds;Call("SetWhatsAppLayout",true);await Task.Delay(200);
        Check(Host.Bounds==bounds,"overlay opening does not resize page");
        Call("SetWhatsAppLayout",false);await Task.Delay(40);Call("SetWhatsAppLayout",true);await Task.Delay(200);
        Call("SetWhatsAppLayout",false);await Task.Delay(200);Frame("overlay-close",true);
        Check(Host.Bounds==bounds,"overlay reversal and closing preserve viewport");
        // Exercise a real WebView2 controller with a local-only HTTP fixture.
        var server=new TcpListener(IPAddress.Loopback,0);server.Start();int port=((IPEndPoint)server.LocalEndpoint).Port;
        var serve=Task.Run(async()=>
        {
            try
            {
                while(true)
                {
                    using var client=await server.AcceptTcpClientAsync();using var stream=client.GetStream();
                    using var reader=new StreamReader(stream,System.Text.Encoding.ASCII,false,1024,true);
                    while(!string.IsNullOrEmpty(await reader.ReadLineAsync())){}
                    var body=System.Text.Encoding.UTF8.GetBytes("<html style='background:#112233'><head><title>VOLT native test</title></head><body style='color:white'>Local render test</body></html>");
                    var header=System.Text.Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: text/html\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
                    await stream.WriteAsync(header);await stream.WriteAsync(body);
                }
            }
            catch(SocketException){}catch(ObjectDisposedException){}
        });
        await Add($"http://127.0.0.1:{port}/");var tab=(BrowserTab)Host.SelectedTab!.Tag!;
        await Wait(()=>tab.Ready&&!tab.Loading&&tab.DocumentReady);await Task.Delay(300);
        var controller=tab.View!.CoreWebView2;var webHandle=tab.View.Handle;
        for(int i=0;i<4;i++)
        {
            window.WindowState=FormWindowState.Minimized;await Task.Delay(80);window.WindowState=FormWindowState.Maximized;window.Activate();await Task.Delay(220);
            Check(tab.View!.Handle==webHandle&&ReferenceEquals(controller,tab.View.CoreWebView2),"restore preserves WebView controller");Frame("web-restore-"+i);
            using var rendered=Capture();var at=window.PointToClient(tab.View.PointToScreen(new Point(35,70)));var pixel=rendered.GetPixel(at.X,at.Y);
            Check(Math.Abs(pixel.R-17)+Math.Abs(pixel.G-34)+Math.Abs(pixel.B-51)<35,"actual restored WebView pixels match local fixture");
        }
        var before=tab.Page;Call("MoveTab",before,0);await Task.Delay(220);Check(tab.View!.Handle==webHandle,"loaded reorder keeps WebView handle");Frame("web-final",true);
        server.Stop();
        File.WriteAllText(Path.Combine(output,"result.json"),JsonSerializer.Serialize(new{passed=checks.Count,checks}));
        Console.WriteLine($"WINDOW_SMOKE_OK {checks.Count} checks");window.Close();
    }
    [STAThread]
    static int Main(string[] args)
    {
        output=Path.GetFullPath(args.FirstOrDefault()??"window-smoke-results");Directory.CreateDirectory(output);
        // Isolate all settings/profile from a real user, using the existing portable mode.
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"portable.txt"),"isolated native smoke test");
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        window=new BrowserWindow {StartPosition=FormStartPosition.Manual,Bounds=new Rectangle(30,30,900,620)};
        int exit=0;
        window.Shown+=async(_,_)=>{try{await Run();}catch(Exception ex){exit=1;Console.Error.WriteLine(ex);File.WriteAllText(Path.Combine(output,"failure.txt"),ex.ToString());try{using var image=Capture();image.Save(Path.Combine(output,"failure.png"));}catch{}window.Close();}};
        Application.Run(window);window.Dispose();return exit;
    }
}
