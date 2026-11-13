using System.Drawing;
namespace Volt;

static class WindowPlacement
{
    public static int PackPoint(Point point) => unchecked((point.X & 0xffff) | ((point.Y & 0xffff) << 16));
    // Native max position is a monitor-relative offset, not a virtual-desktop coordinate.
    public static Rectangle Maximized(Rectangle monitor,Rectangle work)
        => new(work.Left-monitor.Left,work.Top-monitor.Top,work.Width,work.Height);
    public static Rectangle EnsureVisible(Rectangle window,IEnumerable<Rectangle> workAreas,Rectangle preferred)
    {
        var title=new Rectangle(window.X,window.Y,window.Width,Math.Min(36,window.Height));
        if(workAreas.Any(area=>{var visible=Rectangle.Intersect(title,area);return visible.Width>=Math.Min(96,title.Width)&&visible.Height>=Math.Min(24,title.Height);}))return window;
        int width=Math.Min(Math.Max(1,window.Width),preferred.Width),height=Math.Min(Math.Max(1,window.Height),preferred.Height);
        return new(preferred.Left+(preferred.Width-width)/2,preferred.Top+(preferred.Height-height)/2,width,height);
    }
}
