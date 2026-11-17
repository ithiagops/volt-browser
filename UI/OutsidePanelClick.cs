using System.Runtime.InteropServices;

namespace Volt;

// WebView2 receives mouse input in a separate native child process, outside IMessageFilter.
// Observe presses only while the panel is open; never consume, store or replay input.
sealed class OutsidePanelClick : IDisposable
{
    delegate IntPtr MouseProc(int code,IntPtr message,IntPtr data);
    [StructLayout(LayoutKind.Sequential)] struct MouseData
    {
        public Point Point;
        public uint Mouse,Flags,Time;
        public UIntPtr Extra;
    }
    [DllImport("user32.dll",SetLastError=true)] static extern IntPtr SetWindowsHookEx(int id,MouseProc callback,IntPtr module,uint thread);
    [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr window,uint flags);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern IntPtr GetModuleHandle(string? name);
    readonly Form owner;
    readonly Func<Point,bool> outside;
    readonly Action dismiss;
    readonly MouseProc callback;
    IntPtr hook;
    int generation;
    bool pending;
    public OutsidePanelClick(Form owner,Func<Point,bool> outside,Action dismiss)
    {this.owner=owner;this.outside=outside;this.dismiss=dismiss;callback=Observe;}
    public void Start()
    {
        if(hook!=IntPtr.Zero)return;
        generation++;pending=false;
        hook=SetWindowsHookEx(14,callback,GetModuleHandle(null),0); // WH_MOUSE_LL
    }
    public void Stop()
    {
        generation++;pending=false;
        if(hook!=IntPtr.Zero){UnhookWindowsHookEx(hook);hook=IntPtr.Zero;}
    }
    IntPtr Observe(int code,IntPtr message,IntPtr data)
    {
        try
        {
            long msg=message.ToInt64();
            if(code>=0 && !pending && (msg==0x0201 || msg==0x0204 || msg==0x0207)
                && !owner.IsDisposed && owner.IsHandleCreated && owner.Visible)
            {
                var foreground=GetForegroundWindow();
                if((foreground==owner.Handle || GetAncestor(foreground,3)==owner.Handle)
                    && outside(Marshal.PtrToStructure<MouseData>(data).Point))
                {
                    pending=true;int current=generation;
                    owner.BeginInvoke(new Action(()=>
                    {
                        if(current!=generation || owner.IsDisposed)return;
                        pending=false;dismiss();
                    }));
                }
            }
        }
        catch(Exception ex) when(ex is InvalidOperationException or System.Runtime.InteropServices.ExternalException){}
        return CallNextHookEx(hook,code,message,data);
    }
    public void Dispose()=>Stop();
}
