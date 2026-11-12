using System.Drawing.Drawing2D;

namespace Volt;

sealed class HomePage : Panel
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal bool Dark { get; set; }
    readonly TableLayoutPanel card;
    readonly TextBox search;
    public void FocusSearch() { search.Focus(); search.SelectAll(); }
    public HomePage(Action<string> navigate)
    {
        Dock = DockStyle.Fill;
        DoubleBuffered = true;
        ResizeRedraw = true;
        card = new TableLayoutPanel { ColumnCount=1, RowCount=4, Size=new(520,242), BackColor=Color.Transparent, Margin=Padding.Empty };
        foreach(int height in new[]{80,38,58,62})card.RowStyles.Add(new RowStyle(SizeType.Absolute,height));
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        var title=new Label {Text="V O L T",Dock=DockStyle.Fill,Font=new("Segoe UI",34,FontStyle.Regular),ForeColor=Color.FromArgb(37,51,67),TextAlign=ContentAlignment.MiddleCenter};
        var subtitle=new Label {Text="Navegue sem peso.",Dock=DockStyle.Fill,Font=new("Segoe UI",10),ForeColor=Color.FromArgb(74,94,108),TextAlign=ContentAlignment.TopCenter};
        search=new TextBox {PlaceholderText="Pesquise na web...",BorderStyle=BorderStyle.None,Font=new("Segoe UI",12),Margin=Padding.Empty,Tag="search"};
        var searchShell=new RoundedSurface {Dock=DockStyle.Fill,BackColor=Color.White,Padding=new(8,4,8,4),Margin=new(2,1,2,3),Tag="search"};
        var searchHost=new CenteredTextHost(search) {Dock=DockStyle.Fill,BackColor=Color.White,Tag="search",Padding=new(8,0,8,0)};
        var glass=new ChromeButton {Kind="search",Dock=DockStyle.Left,Width=36,BackColor=Color.White,ForeColor=Color.FromArgb(101,117,136),Tag="search",AccessibleName="Pesquisar"};
        var go=new ChromeButton {Kind="lens",Dock=DockStyle.Right,Width=36,BackColor=Color.White,ForeColor=Color.FromArgb(101,117,136),Tag="search",AccessibleName="Pesquisar imagem no Google Lens"};
        void Submit(){if(!string.IsNullOrWhiteSpace(search.Text))navigate(search.Text);}
        go.Click+=(_,_)=>navigate("https://lens.google/");glass.Click+=(_,_)=>Submit();
        search.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;Submit();}};
        searchShell.Controls.Add(searchHost);searchShell.Controls.Add(go);searchShell.Controls.Add(glass);
        var links=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=6,RowCount=1,BackColor=Color.Transparent,Padding=new(0,12,0,10),Margin=Padding.Empty};
        links.RowStyles.Add(new(SizeType.Percent,100));links.ColumnStyles.Add(new(SizeType.Percent,50));
        foreach(int i in new[]{0,1,2,3})links.ColumnStyles.Add(new(SizeType.Absolute,100));
        links.ColumnStyles.Add(new(SizeType.Percent,50));
        int column=1;
        foreach(var(name,url)in new[]{("YouTube","https://www.youtube.com"),("Google","https://www.google.com"),("ChatGPT","https://chatgpt.com"),("Outlook","https://outlook.live.com")})
        {
            var button=new ChromeButton {Text=name,Pill=true,Dock=DockStyle.Fill,Font=new("Segoe UI",9),BackColor=Color.FromArgb(235,240,244),ForeColor=Color.FromArgb(37,51,67),Margin=new(4,0,4,0),AccessibleName=name};
            button.Click+=(_,_)=>navigate(url);links.Controls.Add(button,column++,0);
        }
        card.Controls.Add(title,0,0);card.Controls.Add(subtitle,0,1);card.Controls.Add(searchShell,0,2);card.Controls.Add(links,0,3);
        Controls.Add(card);
        LayoutCard();
    }
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        LayoutCard();
        Invalidate(true);
    }
    void LayoutCard()
    {
        if (card is null) return;
        card.Width = Math.Min((int)(520*DeviceDpi/96f), Math.Max(320, ClientSize.Width - 40));
        card.Location = new(Math.Max(0, (ClientSize.Width - card.Width) / 2), Math.Max(20, (ClientSize.Height - card.Height) / 2 - 35));
    }
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
        using var bg = new LinearGradientBrush(ClientRectangle, Dark ? Color.FromArgb(27, 37, 51) : Color.FromArgb(223, 233, 243), Dark ? Color.FromArgb(36, 33, 45) : Color.FromArgb(236, 215, 208), 75f);
        e.Graphics.FillRectangle(bg, ClientRectangle);
        using var hills = new SolidBrush(Dark ? Color.FromArgb(44, 51, 70) : Color.FromArgb(55, 112, 145, 155));
        var w = ClientSize.Width; var h = ClientSize.Height;
        e.Graphics.FillPolygon(hills, [new(0, h), new(0, h * 3 / 4), new(w / 5, h * 3 / 5), new(w / 2, h * 4 / 5), new(w * 4 / 5, h * 2 / 3), new(w, h * 3 / 4), new(w, h)]);
    }
}
