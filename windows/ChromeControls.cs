using System.Drawing.Drawing2D;

namespace Volt;

static class ChromeShape
{
    public static GraphicsPath Rounded(RectangleF r, float radius)
    {
        var p = new GraphicsPath(); float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        if (d <= 0) { p.AddRectangle(r); return p; }
        p.AddArc(r.Left, r.Top, d, d, 180, 90); p.AddArc(r.Right-d, r.Top, d, d, 270, 90);
        p.AddArc(r.Right-d, r.Bottom-d, d, d, 0, 90); p.AddArc(r.Left, r.Bottom-d, d, d, 90, 90); p.CloseFigure(); return p;
    }
}

// A resize changes every gradient coordinate, not just the newly exposed strip.
sealed class BrowserContentPanel : Panel
{
    public BrowserContentPanel() { DoubleBuffered=true; ResizeRedraw=true; }
    protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); Invalidate(true); }
}

sealed class RoundedSurface : Panel
{
    public RoundedSurface() { DoubleBuffered = true; ResizeRedraw = true; }
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (Width < 1 || Height < 1) return;
        using var shape = ChromeShape.Rounded(new(0, 0, Width, Height), Height / 2f);
        var old = Region; Region = new Region(shape); old?.Dispose();
    }
}

sealed class ChromeButton : Button
{
    static readonly Lazy<Image> WhatsAppIcon=new(()=>
    {
        using var stream=typeof(ChromeButton).Assembly.GetManifestResourceStream("Volt.WhatsAppIcon")!;
        using var image=Image.FromStream(stream);
        return new Bitmap(image);
    });
    internal string Kind = "";
    internal bool Pill;
    internal bool IsOn;
    internal bool DarkTheme;
    bool hot;
    public ChromeButton()
    {
        FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0;
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Cursor = Cursors.Hand;
    }
    void UpdateShape()
    {
        if(!Pill || Width<1 || Height<1) return;
        using var capsule=ChromeShape.Rounded(new(0,0,Width,Height),Height/2f);
        var previous=Region;Region=new Region(capsule);previous?.Dispose();
    }
    protected override void OnResize(EventArgs e){base.OnResize(e);UpdateShape();}
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);UpdateShape();}
    protected override void OnMouseEnter(EventArgs e) { hot = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hot = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);
        bool dark = DarkTheme;
        var r = new RectangleF(1, 1, Width-2, Height-2);
        if (hot || Pill || Focused)
        {
            using var path = ChromeShape.Rounded(r, Pill ? Height/2f : 6);
            if(Pill)
            {
                using var gradient=new LinearGradientBrush(r,
                    dark ? Color.FromArgb(hot?35:23,hot?48:31,hot?66:43) : Color.FromArgb(252,254,255),
                    dark ? Color.FromArgb(hot?51:39,hot?67:47,hot?89:63) : Color.FromArgb(hot?204:223,hot?220:233,hot?240:244),90f);
                g.FillPath(gradient,path);
            }
            else
            {
                using var brush=new SolidBrush(Kind=="close" && hot ? Color.FromArgb(182,55,66)
                    : dark ? Color.FromArgb(49,61,77) : Color.FromArgb(215,225,235));
                g.FillPath(brush,path);
            }
        }

        var color = Enabled ? ForeColor : dark ? Color.FromArgb(89,101,118) : Color.FromArgb(150,160,170);
        using var pen = new Pen(color, Math.Max(1.2f, DeviceDpi / 96f * 1.25f)) { StartCap=LineCap.Round, EndCap=LineCap.Round, LineJoin=LineJoin.Round };
        var state=g.Save(); float scale=DeviceDpi/96f; g.TranslateTransform(Width/2f, Height/2f); g.ScaleTransform(scale,scale);
        switch (Kind)
        {
            case "back": g.DrawLines(pen,new PointF[]{new(2,-5),new(-3,0),new(2,5)}); break;
            case "forward": g.DrawLines(pen,new PointF[]{new(-2,-5),new(3,0),new(-2,5)}); break;
            case "reload": g.DrawArc(pen,-5,-5,10,10,35,290); g.DrawLines(pen,new PointF[]{new(5,-5),new(5,-1),new(1,-1)}); break;
            case "search": g.DrawEllipse(pen,-6,-6,9,9);g.DrawLine(pen,1,1,6,6);break;
            case "whatsapp":
                g.InterpolationMode=InterpolationMode.HighQualityBicubic;
                g.DrawImage(WhatsAppIcon.Value,new RectangleF(-14,-14,28,28));
                break;
            case "lens":
                g.DrawLines(pen,new PointF[]{new(-6,-2),new(-6,-5),new(-2,-5)});
                g.DrawLines(pen,new PointF[]{new(2,-5),new(6,-5),new(6,-2)});
                g.DrawLines(pen,new PointF[]{new(-6,2),new(-6,5),new(-2,5)});
                g.DrawLines(pen,new PointF[]{new(2,5),new(6,5),new(6,2)});
                g.DrawEllipse(pen,-2,-2,4,4);break;
            case "menu": for(int y=-4;y<=4;y+=4)g.DrawLine(pen,-6,y,6,y);break;
            case "download": g.DrawLine(pen,0,-6,0,3);g.DrawLines(pen,new PointF[]{new(-4,0),new(0,4),new(4,0)});g.DrawLines(pen,new PointF[]{new(-6,3),new(-6,7),new(6,7),new(6,3)});break;
            case "favorite":
                PointF[] heart=[new(0,6),new(-6,0),new(-6,-3),new(-3,-6),new(0,-3),new(3,-6),new(6,-3),new(6,0),new(0,6)];
                g.DrawLines(pen,heart);break;
            case "theme":
                using(var fill=new SolidBrush(IsOn ? Color.FromArgb(117,159,199) : Color.FromArgb(104,118,136)))
                using(var path=ChromeShape.Rounded(new(-12,-6,24,12),6))g.FillPath(fill,path);
                using(var knob=new SolidBrush(Color.FromArgb(231,238,245)))g.FillEllipse(knob,IsOn ? 1 : -10,-4,8,8);break;
            case "minimize":g.DrawLine(pen,-6,3,6,3);break;
            case "maximize":g.DrawRectangle(pen,-5,-5,10,10);break;
            case "restore":g.DrawRectangle(pen,-5,-2,8,8);g.DrawLines(pen,new PointF[]{new(-2,-5),new(6,-5),new(6,3)});break;
            case "close":g.DrawLine(pen,-4,-4,4,4);g.DrawLine(pen,4,-4,-4,4);break;
            case "home":g.DrawLines(pen,new PointF[]{new(-6,0),new(0,-6),new(6,0)});g.DrawLines(pen,new PointF[]{new(-4,-1),new(-4,6),new(4,6),new(4,-1)});break;
        }
        g.Restore(state);
        if (Kind.Length == 0) TextRenderer.DrawText(g,Text,Font,ClientRectangle,color,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
    }
}
