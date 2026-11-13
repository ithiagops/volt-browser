using System.Drawing.Drawing2D;

namespace Volt;

// One small native popup; no WebView, extra browser process or global mouse hook.
sealed class AddressSuggestions : IDisposable, IMessageFilter
{
    sealed class SuggestionWindow : Form
    {
        public SuggestionWindow() { FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;Padding=new(6);DoubleBuffered=true; }
        protected override bool ShowWithoutActivation => true;
        protected override CreateParams CreateParams { get { var p=base.CreateParams;p.ExStyle|=0x08000000|0x80;return p; } }
        protected override void WndProc(ref Message m)
        { if(m.Msg==0x21){m.Result=(IntPtr)3;return;}base.WndProc(ref m); }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);if(Width<1||Height<1)return;
            using var shape=ChromeShape.Rounded(ClientRectangle,10*DeviceDpi/96f);
            var old=Region;Region=new Region(shape);old?.Dispose();
        }
    }
    readonly Form owner;
    readonly TextBox text;
    readonly Action<string> navigate;
    readonly SuggestionWindow popup=new();
    readonly ListBox list=new() { Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,DrawMode=DrawMode.OwnerDrawFixed,IntegralHeight=false,TabStop=false,AccessibleName="Sugestões de endereços" };
    string[] values=[];
    bool dark,filtered,setting,disposed;
    public AddressSuggestions(Form owner,TextBox text,Action<string> navigate,bool dark)
    {
        this.owner=owner;this.text=text;this.navigate=navigate;
        popup.Controls.Add(list);list.Font=text.Font;list.ItemHeight=(int)Math.Ceiling(36*text.DeviceDpi/96f);
        list.DrawItem+=DrawItem;list.MouseMove+=Hover;list.MouseClick+=Choose;
        text.TextChanged+=Changed;text.LostFocus+=LostFocus;
        owner.Deactivate+=OwnerChanged;owner.LocationChanged+=OwnerChanged;owner.SizeChanged+=OwnerChanged;
        ApplyTheme(dark);
    }
    public void ApplyTheme(bool value)
    {
        dark=value;popup.BackColor=list.BackColor=dark?Color.FromArgb(35,43,54):Color.White;
        list.ForeColor=dark?Color.FromArgb(229,236,244):Color.FromArgb(37,51,67);list.Invalidate();popup.Invalidate();
    }
    public void SetValues(IEnumerable<string> source)
    { values=source.Distinct(StringComparer.OrdinalIgnoreCase).Take(500).ToArray();if(popup.Visible)ShowMatches(); }
    void Changed(object? sender,EventArgs e) {if(!setting && text.Focused && text.Modified)ShowMatches();else Hide();}
    void LostFocus(object? sender,EventArgs e) => Hide();
    void OwnerChanged(object? sender,EventArgs e) => Hide();
    void ShowMatches()
    {
        string query=text.Text.Trim();
        var matches=query.Length==0?[]:values.Where(v=>v.Contains(query,StringComparison.OrdinalIgnoreCase)).Take(8).ToArray();
        list.BeginUpdate();list.Items.Clear();list.Items.AddRange(matches);list.SelectedIndex=-1;list.EndUpdate();
        if(matches.Length==0){Hide();return;}
        var anchor=text.Parent?.Parent??text;var at=anchor.PointToScreen(new Point(0,anchor.Height+4));
        var area=Screen.FromControl(text).WorkingArea;
        int width=Math.Min(area.Width,Math.Max(280,anchor.Width));int height=list.ItemHeight*matches.Length+popup.Padding.Vertical;
        at.X=Math.Clamp(at.X,area.Left,Math.Max(area.Left,area.Right-width));
        if(at.Y+height>area.Bottom)at.Y=anchor.PointToScreen(Point.Empty).Y-height-4;
        popup.Bounds=new Rectangle(at.X,Math.Max(area.Top,at.Y),width,height);
        if(!popup.Visible)popup.Show(owner);
        if(!filtered){Application.AddMessageFilter(this);filtered=true;}
    }
    void DrawItem(object? sender,DrawItemEventArgs e)
    {
        if(e.Index<0)return;bool selected=(e.State&DrawItemState.Selected)!=0;
        using var bg=new SolidBrush(list.BackColor);e.Graphics.FillRectangle(bg,e.Bounds);
        if(selected)
        {
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            using var shape=ChromeShape.Rounded(Rectangle.Inflate(e.Bounds,-2,-2),7);
            using var brush=new SolidBrush(dark?Color.FromArgb(52,67,84):Color.FromArgb(227,237,247));e.Graphics.FillPath(brush,shape);
        }
        var bounds=Rectangle.Inflate(e.Bounds,-12,0);
        TextRenderer.DrawText(e.Graphics,list.Items[e.Index].ToString(),list.Font,bounds,list.ForeColor,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
    }
    void Hover(object? sender,MouseEventArgs e) {int i=list.IndexFromPoint(e.Location);if(i>=0)list.SelectedIndex=i;}
    void Choose(object? sender,MouseEventArgs e) {if(e.Button==MouseButtons.Left && list.IndexFromPoint(e.Location)>=0)Accept();}
    void Accept()
    {
        if(list.SelectedItem is not string value)return;
        setting=true;try{text.Text=value;text.SelectionStart=text.Text.Length;}finally{setting=false;}
        Hide();navigate(value);
    }
    public bool HandleKey(Keys key)
    {
        if(!popup.Visible || !text.Focused)return false;
        if(key==Keys.Escape){Hide();return true;}
        if(key is Keys.Down or Keys.Up)
        {int i=list.SelectedIndex;list.SelectedIndex=i<0?(key==Keys.Down?0:list.Items.Count-1):(i+(key==Keys.Down?1:-1)+list.Items.Count)%list.Items.Count;return true;}
        if(key==Keys.Enter && list.SelectedIndex>=0){Accept();return true;}
        if(key==Keys.Enter)Hide();
        return false;
    }
    public bool PreFilterMessage(ref Message m)
    {
        if(m.Msg is 0x201 or 0x204 or 0x207 or 0xA1)
        {var p=Cursor.Position;if(!popup.Bounds.Contains(p) && !text.RectangleToScreen(text.ClientRectangle).Contains(p))Hide();}
        return false;
    }
    public void Hide() {if(disposed)return;popup.Hide();if(filtered){Application.RemoveMessageFilter(this);filtered=false;}}
    public void Dispose()
    {
        if(disposed)return;Hide();disposed=true;
        text.TextChanged-=Changed;text.LostFocus-=LostFocus;
        owner.Deactivate-=OwnerChanged;owner.LocationChanged-=OwnerChanged;owner.SizeChanged-=OwnerChanged;popup.Dispose();
    }
}
