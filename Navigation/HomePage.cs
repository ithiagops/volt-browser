using System.Drawing.Drawing2D;
namespace Volt;

// Brand, subtitle and background share one opaque buffered surface. Only editable
// text and interactive buttons keep child HWNDs, so restoration cannot replay old labels.
sealed class HomePage : Panel
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal bool Dark {get;set;}
    readonly TextBox search;
    readonly RoundedSurface searchShell;
    readonly ChromeButton glass,go;
    readonly List<ChromeButton> links=[];
    readonly Font brandFont=new("Segoe UI",34),subtitleFont=new("Segoe UI",10);
    Rectangle titleBounds,subtitleBounds;
    bool positioning;
    public void FocusSearch(){search.Focus();search.SelectAll();}
    public HomePage(Action<string> navigate)
    {
        Dock=DockStyle.Fill;DoubleBuffered=true;ResizeRedraw=true;
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.Opaque,true);
        search=new TextBox {PlaceholderText="Pesquise na web...",BorderStyle=BorderStyle.None,Font=new("Segoe UI",12),Margin=Padding.Empty,Tag="search"};
        searchShell=new RoundedSurface {BackColor=Color.White,Padding=new(8,4,8,4),Tag="search"};
        var host=new CenteredTextHost(search){Dock=DockStyle.Fill,BackColor=Color.White,Tag="search",Padding=new(8,0,8,0)};
        glass=new ChromeButton {Kind="search",Dock=DockStyle.Left,Width=36,BackColor=Color.White,ForeColor=Color.FromArgb(101,117,136),Tag="search",AccessibleName="Pesquisar"};
        go=new ChromeButton {Kind="lens",Dock=DockStyle.Right,Width=36,BackColor=Color.White,ForeColor=Color.FromArgb(101,117,136),Tag="search",AccessibleName="Pesquisar imagem no Google Lens"};
        void Submit(){if(!string.IsNullOrWhiteSpace(search.Text))navigate(search.Text);}
        go.Click+=(_,_)=>navigate("https://lens.google/");glass.Click+=(_,_)=>Submit();
        search.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;Submit();}};
        searchShell.Controls.Add(host);searchShell.Controls.Add(go);searchShell.Controls.Add(glass);Controls.Add(searchShell);
        foreach(var(name,url)in new[]{("YouTube","https://www.youtube.com"),("Google","https://www.google.com"),("ChatGPT","https://chatgpt.com"),("Outlook","https://outlook.live.com")})
        {
            var button=new ChromeButton {Text=name,Pill=true,Font=new("Segoe UI",9),BackColor=Color.FromArgb(235,240,244),ForeColor=Color.FromArgb(37,51,67),AccessibleName=name};
            button.Click+=(_,_)=>navigate(url);links.Add(button);Controls.Add(button);
        }
        LayoutContent();
    }
    protected override void OnLayout(LayoutEventArgs e){base.OnLayout(e);LayoutContent();}
    protected override void OnDpiChangedAfterParent(EventArgs e){base.OnDpiChangedAfterParent(e);LayoutContent();Invalidate(true);}
    internal void RefreshAfterRestore(){LayoutContent();Invalidate(true);}
    void LayoutContent()
    {
        if(searchShell is null || positioning || ClientSize.Width<1 || ClientSize.Height<1 || FindForm()?.WindowState==FormWindowState.Minimized)return;
        positioning=true;
        try
        {
            float scale=DeviceDpi/96f;int Px(float value)=>(int)Math.Round(value*scale);
            int width=Math.Max(1,Math.Min(Px(520),ClientSize.Width-Px(40))),height=Px(242);
            int x=Math.Max(0,(ClientSize.Width-width)/2),y=Math.Max(Px(20),(ClientSize.Height-height)/2-Px(35));
            titleBounds=new(x,y,width,Px(80));subtitleBounds=new(x,y+Px(80),width,Px(38));
            searchShell.Bounds=new(x+Px(2),y+Px(119),Math.Max(1,width-Px(4)),Px(54));
            glass.Width=go.Width=Px(36);
            int pitch=Math.Min(Px(100),Math.Max(1,width/4)),first=x+(width-pitch*4)/2;
            for(int i=0;i<links.Count;i++)links[i].Bounds=new(first+i*pitch+Px(4),y+Px(188),Math.Max(1,pitch-Px(8)),Px(40));
        }
        finally{positioning=false;}
    }
    protected override void OnPaintBackground(PaintEventArgs e) { }
    protected override void OnPaint(PaintEventArgs e)
    {
        if(ClientSize.Width<1||ClientSize.Height<1)return;
        using var bg=new LinearGradientBrush(ClientRectangle,Dark?Color.FromArgb(27,37,51):Color.FromArgb(223,233,243),Dark?Color.FromArgb(36,33,45):Color.FromArgb(236,215,208),75f);
        e.Graphics.FillRectangle(bg,ClientRectangle);
        using var hills=new SolidBrush(Dark?Color.FromArgb(44,51,70):Color.FromArgb(112,145,155));
        int w=ClientSize.Width,h=ClientSize.Height;
        e.Graphics.FillPolygon(hills,[new(0,h),new(0,h*3/4),new(w/5,h*3/5),new(w/2,h*4/5),new(w*4/5,h*2/3),new(w,h*3/4),new(w,h)]);
        TextRenderer.DrawText(e.Graphics,"V O L T",brandFont,titleBounds,Dark?Color.FromArgb(229,236,244):Color.FromArgb(37,51,67),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(e.Graphics,"Navegue sem peso.",subtitleFont,subtitleBounds,Dark?Color.FromArgb(180,193,210):Color.FromArgb(74,94,108),TextFormatFlags.HorizontalCenter|TextFormatFlags.Top|TextFormatFlags.NoPrefix);
    }
    protected override void Dispose(bool disposing){if(disposing){brandFont.Dispose();subtitleFont.Dispose();}base.Dispose(disposing);}
}
