using System.Drawing.Drawing2D;
namespace Volt;

sealed class VaultActionButton : Button
{
    internal bool Dark,Primary;
    bool hot;
    public VaultActionButton(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
    protected override void OnMouseEnter(EventArgs e){hot=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hot=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(BackColor);
        using var shape=ChromeShape.Rounded(new RectangleF(1,1,Width-2,Height-2),10*DeviceDpi/96f);
        Color fill=Primary?(Dark?(hot?Color.White:Color.FromArgb(215,230,247)):Color.FromArgb(hot?47:34,hot?67:49,hot?94:71)):(Dark?Color.FromArgb(hot?46:34,hot?59:43,hot?76:55):Color.FromArgb(hot?224:234,hot?234:241,hot?245:248));
        Color text=Primary?(Dark?Color.FromArgb(24,35,49):Color.White):ForeColor;
        if(!Enabled){fill=Dark?Color.FromArgb(29,36,46):Color.FromArgb(238,242,247);text=Dark?Color.FromArgb(111,126,145):Color.FromArgb(139,153,169);}
        using var brush=new SolidBrush(fill);g.FillPath(brush,shape);
        if(Focused){using var pen=new Pen(Dark?Color.FromArgb(132,173,218):Color.FromArgb(85,126,174),2);g.DrawPath(pen,shape);}
        TextRenderer.DrawText(g,Text,Font,ClientRectangle,text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
    }
}
sealed class VaultEmptyState : Control
{
    internal bool Dark;
    public VaultEmptyState(){DoubleBuffered=true;ResizeRedraw=true;AccessibleName="Seu cofre está vazio. Importe senhas para começar.";}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        float scale=DeviceDpi/96f,cx=Width/2f,y=Math.Max(8*scale,(Height-248*scale)/2);
        using var circle=new SolidBrush(Dark?Color.FromArgb(33,43,57):Color.FromArgb(229,237,247));g.FillEllipse(circle,cx-29*scale,y,58*scale,58*scale);
        using var pen=new Pen(Dark?Color.FromArgb(175,201,231):Color.FromArgb(75,104,139),1.8f*scale){StartCap=LineCap.Round,EndCap=LineCap.Round};
        g.DrawArc(pen,cx-7*scale,y+13*scale,14*scale,19*scale,180,180);
        using var body=ChromeShape.Rounded(new RectangleF(cx-12*scale,y+25*scale,24*scale,20*scale),4*scale);g.DrawPath(pen,body);g.DrawLine(pen,cx,y+32*scale,cx,y+37*scale);
        using var title=new Font("Segoe UI",15,FontStyle.Regular);using var note=new Font("Segoe UI",10);
        Color fg=Dark?Color.FromArgb(231,238,247):Color.FromArgb(34,49,68),muted=Dark?Color.FromArgb(148,166,188):Color.FromArgb(100,119,142);
        TextRenderer.DrawText(g,"Seus acessos, em um só lugar",title,new Rectangle(0,(int)(y+77*scale),Width,(int)(30*scale)),fg,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(g,"Importe as senhas do seu navegador para começar.",note,new Rectangle(0,(int)(y+113*scale),Width,(int)(26*scale)),muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(g,"Chrome · Opera · Edge · Firefox · Bitwarden",note,new Rectangle(0,(int)(y+208*scale),Width,(int)(24*scale)),muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
    }
    internal int ButtonTop => (int)(Math.Max(8*DeviceDpi/96f,(Height-248*DeviceDpi/96f)/2)+155*DeviceDpi/96f);
}
