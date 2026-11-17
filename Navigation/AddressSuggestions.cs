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
    bool dark,filtered,setting,deleting,disposed;

    public AddressSuggestions(Form owner,TextBox text,Action<string> navigate,bool dark)
    {
        this.owner=owner;this.text=text;this.navigate=navigate;
        popup.Controls.Add(list);list.Font=text.Font;list.ItemHeight=(int)Math.Ceiling(36*text.DeviceDpi/96f);
        list.DrawItem+=DrawItem;list.MouseMove+=Hover;list.MouseClick+=Choose;
        text.TextChanged+=Changed;text.KeyDown+=TextKeyDown;text.LostFocus+=LostFocus;
        owner.Deactivate+=OwnerChanged;owner.LocationChanged+=OwnerChanged;owner.SizeChanged+=OwnerChanged;
        ApplyTheme(dark);
    }

    public void ApplyTheme(bool value)
    {
        dark=value;popup.BackColor=list.BackColor=dark?Color.FromArgb(35,43,54):Color.White;
        list.ForeColor=dark?Color.FromArgb(229,236,244):Color.FromArgb(37,51,67);list.Invalidate();popup.Invalidate();
    }

    public void SetValues(IEnumerable<string> source)
    {
        values=source.Distinct(StringComparer.OrdinalIgnoreCase).Take(500).ToArray();
        if(popup.Visible)ShowMatches();
    }

    void TextKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Back or Keys.Delete)
        {
            deleting = true;
        }
        else if (e.KeyCode is Keys.Right or Keys.Tab)
        {
            if (text.SelectionLength > 0 && text.SelectionStart + text.SelectionLength == text.Text.Length)
            {
                text.SelectionStart = text.Text.Length;
                text.SelectionLength = 0;
                e.Handled = true;
                e.SuppressKeyPress = true;
                ShowMatches();
            }
        }
    }

    void Changed(object? sender,EventArgs e)
    {
        if (setting) return;
        if (text.Focused && text.Modified)
        {
            ShowMatches();
        }
        else
        {
            Hide();
        }
    }

    void LostFocus(object? sender,EventArgs e) => Hide();
    void OwnerChanged(object? sender,EventArgs e) => Hide();

    void ShowMatches()
    {
        if (setting) return;
        int typedLength = text.SelectionLength > 0 ? text.SelectionStart : text.Text.Length;
        string query = text.Text[..Math.Min(text.Text.Length, typedLength)].Trim();

        if (query.Length == 0)
        {
            Hide();
            return;
        }

        var matches = SiteCatalog.FindCompletions(query, values, 8);

        // Inline autocompletion: if not deleting and cursor at typed end, complete the top prefix match
        if (!deleting && text.Focused && text.SelectionLength == 0)
        {
            string cleanQ = SiteCatalog.Normalize(query);
            var prefixMatch = matches.FirstOrDefault(m => SiteCatalog.Normalize(m).StartsWith(cleanQ, StringComparison.OrdinalIgnoreCase));
            if (prefixMatch is not null && prefixMatch.Length > query.Length)
            {
                setting = true;
                try
                {
                    int start = query.Length;
                    string completion = prefixMatch;
                    if (!prefixMatch.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                    {
                        // Match normalized prefix to maintain user's typed casing/prefix
                        completion = query + prefixMatch[cleanQ.Length..];
                    }
                    text.Text = completion;
                    text.SelectionStart = start;
                    text.SelectionLength = completion.Length - start;
                }
                finally { setting = false; }
            }
        }
        deleting = false;

        list.BeginUpdate();
        list.Items.Clear();
        foreach (var m in matches) list.Items.Add(m);
        list.SelectedIndex = -1;
        list.EndUpdate();

        if (matches.Count == 0)
        {
            Hide();
            return;
        }

        var anchor = text.Parent?.Parent ?? text;
        var at = anchor.PointToScreen(new Point(0, anchor.Height + 4));
        var area = Screen.FromControl(text).WorkingArea;
        int width = Math.Min(area.Width, Math.Max(280, anchor.Width));
        int height = list.ItemHeight * matches.Count + popup.Padding.Vertical;
        at.X = Math.Clamp(at.X, area.Left, Math.Max(area.Left, area.Right - width));
        if (at.Y + height > area.Bottom) at.Y = anchor.PointToScreen(Point.Empty).Y - height - 4;
        popup.Bounds = new Rectangle(at.X, Math.Max(area.Top, at.Y), width, height);

        if (!popup.Visible) popup.Show(owner);
        if (!filtered) { Application.AddMessageFilter(this); filtered = true; }
    }

    void DrawItem(object? sender,DrawItemEventArgs e)
    {
        if(e.Index<0 || e.Index >= list.Items.Count)return;
        bool selected=(e.State&DrawItemState.Selected)!=0;
        using var bg=new SolidBrush(list.BackColor);
        e.Graphics.FillRectangle(bg,e.Bounds);

        if(selected)
        {
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            using var shape=ChromeShape.Rounded(Rectangle.Inflate(e.Bounds,-2,-2),7);
            using var brush=new SolidBrush(dark?Color.FromArgb(52,67,84):Color.FromArgb(227,237,247));
            e.Graphics.FillPath(brush,shape);
        }

        string rawText = list.Items[e.Index]?.ToString() ?? "";
        var iconBounds = new Rectangle(e.Bounds.Left + 8, e.Bounds.Top, 22, e.Bounds.Height);
        var textBounds = new Rectangle(e.Bounds.Left + 32, e.Bounds.Top, Math.Max(10, e.Bounds.Width - 40), e.Bounds.Height);

        // Draw web icon indicator
        TextRenderer.DrawText(e.Graphics, "🌐", list.Font, iconBounds, dark ? Color.FromArgb(140, 165, 195) : Color.FromArgb(100, 125, 155),
            TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);

        Color textColor = dark ? Color.FromArgb(229, 236, 244) : Color.FromArgb(37, 51, 67);
        TextRenderer.DrawText(e.Graphics, rawText, list.Font, textBounds, textColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    void Hover(object? sender,MouseEventArgs e)
    {
        int i=list.IndexFromPoint(e.Location);
        if(i>=0 && i<list.Items.Count) list.SelectedIndex=i;
    }

    void Choose(object? sender,MouseEventArgs e)
    {
        if(e.Button==MouseButtons.Left && list.IndexFromPoint(e.Location)>=0) Accept();
    }

    void Accept()
    {
        string? value = list.SelectedItem?.ToString();
        if (string.IsNullOrWhiteSpace(value))
        {
            value = text.Text.Trim();
        }
        if (string.IsNullOrWhiteSpace(value)) return;

        setting = true;
        try
        {
            text.Text = value;
            text.SelectionStart = text.Text.Length;
            text.SelectionLength = 0;
        }
        finally { setting = false; }

        Hide();
        navigate(ResourcePolicy.Address(value));
    }

    public bool HandleKey(Keys key)
    {
        if (text.Focused && key is Keys.Right or Keys.Tab && text.SelectionLength > 0)
        {
            text.SelectionStart = text.Text.Length;
            text.SelectionLength = 0;
            return true;
        }

        if(!popup.Visible || !text.Focused) return false;

        if(key==Keys.Escape)
        {
            Hide();
            return true;
        }
        if(key is Keys.Down or Keys.Up)
        {
            if (list.Items.Count == 0) return false;
            int i=list.SelectedIndex;
            list.SelectedIndex=i<0?(key==Keys.Down?0:list.Items.Count-1):(i+(key==Keys.Down?1:-1)+list.Items.Count)%list.Items.Count;
            if (list.SelectedItem is string itemVal)
            {
                setting = true;
                try
                {
                    text.Text = itemVal;
                    text.SelectionStart = itemVal.Length;
                    text.SelectionLength = 0;
                }
                finally { setting = false; }
            }
            return true;
        }
        if(key==Keys.Enter)
        {
            Accept();
            return true;
        }
        return false;
    }

    public bool PreFilterMessage(ref Message m)
    {
        if(m.Msg is 0x201 or 0x204 or 0x207 or 0xA1)
        {
            var p=Cursor.Position;
            if(!popup.Bounds.Contains(p) && !text.RectangleToScreen(text.ClientRectangle).Contains(p))
                Hide();
        }
        return false;
    }

    public void Hide()
    {
        if(disposed)return;
        popup.Hide();
        if(filtered){Application.RemoveMessageFilter(this);filtered=false;}
    }

    public void Dispose()
    {
        if(disposed)return;
        Hide();
        disposed=true;
        text.TextChanged-=Changed;
        text.KeyDown-=TextKeyDown;
        text.LostFocus-=LostFocus;
        owner.Deactivate-=OwnerChanged;
        owner.LocationChanged-=OwnerChanged;
        owner.SizeChanged-=OwnerChanged;
        popup.Dispose();
    }
}
