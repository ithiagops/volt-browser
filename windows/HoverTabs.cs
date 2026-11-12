using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace Volt;

// Só gerencia as páginas; o cabeçalho é um controle leve, sem tema nativo por baixo.
sealed class HoverTabs : TabControl
{
    public HoverTabs()
    {
        SizeMode = TabSizeMode.Fixed;
        ItemSize = new Size(1, 1);
        Padding = Point.Empty;
        Appearance = TabAppearance.FlatButtons;
    }
    public event Action<TabPage>? CloseRequested;
    public event Action<TabPage, int>? ReorderRequested;
    public event Action<TabPage>? OpeningRequested;
    public void RequestClose(TabPage page) => CloseRequested?.Invoke(page);
    public void RequestMove(TabPage page, int destination)
    {
        int source = TabPages.IndexOf(page);
        ReorderRequested?.Invoke(page, destination > source ? destination + 1 : destination);
    }
    public void MarkOpening(TabPage page) => OpeningRequested?.Invoke(page);
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x1328 && !DesignMode) { m.Result = IntPtr.Zero; return; }
        base.WndProc(ref m);
    }
}

sealed class BrowserTabsHeader : Control
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal bool Dark { get; set; }
    sealed class Visual { public float X, Width, Opacity = 1; }
    readonly HoverTabs model;
    readonly TabPage plusPage;
    readonly Dictionary<TabPage, Visual> visuals = new();
    readonly Dictionary<TabPage, RectangleF> targets = new();
    readonly Dictionary<TabPage, RectangleF> hits = new();
    readonly HashSet<TabPage> opening = new();
    readonly TabRangeSelection<TabPage> selection = new();
    readonly ContextMenuStrip groupMenu = new();
    bool rangeClick;
    TabPage[] dragGroup=[];
    public event Action<IReadOnlyList<TabPage>,int>? MoveGroupRequested;
    public event Action<IReadOnlyList<TabPage>>? CloseGroupRequested;
    readonly System.Windows.Forms.Timer animation = new() { Interval = 16 };
    readonly ToolTip tooltip = new() { InitialDelay = 700 };
    TabPage? hover, candidate, pressedClose;
    Point pointer = new(-1, -1), origin;
    bool dragging, reveal = true, hotClose;
    int start, destination;
    float grabOffset;
    RectangleF plus, left, right, tabViewport;
    long lastFrame, lastScroll;
    public event Action? NewRequested;
    public event Action<bool>? EmptyDragRequested;
    float DpiFactor => DeviceDpi / 96f;
    float Px(float value) => value * DpiFactor;
    List<TabPage> Pages => model.TabPages.Cast<TabPage>().Where(p => p != plusPage).ToList();

    public BrowserTabsHeader(HoverTabs model, TabPage plusPage)
    {
        this.model = model; this.plusPage = plusPage;
        groupMenu.Items.Add("Fechar abas selecionadas",null,(_,_)=>
        {
            var snapshot=Pages.Where(p=>selection.Items.Contains(p)).ToArray();
            if(snapshot.Length>0)CloseGroupRequested?.Invoke(snapshot);
        });
        Dock = DockStyle.Top; Height = 36; Font = new Font("Segoe UI", 9.5f);
        BackColor = Color.FromArgb(232, 239, 245); ForeColor = Color.FromArgb(41, 52, 63);
        AccessibleName = "Abas do VOLT — arraste para reordenar";
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        animation.Tick += Frame;
        model.ControlAdded += Added; model.ControlRemoved += Removed;
        model.SelectedIndexChanged += Selected; model.OpeningRequested += Opened;
        foreach (TabPage p in model.TabPages) p.TextChanged += TitleChanged;
    }
    void Added(object? s, ControlEventArgs e) { if (e.Control is TabPage p) p.TextChanged += TitleChanged; StartAnimation(); Invalidate(); }
    void Removed(object? s, ControlEventArgs e) { if (e.Control is TabPage p) p.TextChanged -= TitleChanged; StartAnimation(); Invalidate(); }
    void TitleChanged(object? s, EventArgs e) => Invalidate();
    void Selected(object? s, EventArgs e) {
        if(!rangeClick && model.SelectedTab is { } selected && selected!=plusPage)
            selection.Click(Pages,selected,false);
        reveal = true; StartAnimation(); Invalidate(); }
    void Opened(TabPage p) { opening.Add(p); visuals.Remove(p); RefreshLayout(); }
    void RefreshLayout() { if (IsDisposed) return; Arrange(); StartAnimation(); Invalidate(); }
    void StartAnimation()
    {
        if (animation.Enabled) return;
        lastFrame = Stopwatch.GetTimestamp(); animation.Start();
    }
    void Arrange()
    {
        var pages = Pages;
        if(dragging && dragGroup.Length>1)pages=TabBlockMove.Move(pages,dragGroup,destination);
        else if (dragging && candidate is not null && pages.Remove(candidate)) pages.Insert(Math.Clamp(destination, 0, pages.Count), candidate);
        var geometry = TabLayout.Calculate(pages.Count, Width / DpiFactor, start, pages.IndexOf(model.SelectedTab!), reveal && !dragging);
        start = geometry.Start; reveal = false;
        targets.Clear();
        float x = Px(8), y = Px(6), h = Math.Max(Px(24), Height - Px(12)), w = Px(geometry.Width);
        foreach (var page in pages.Skip(start).Take(geometry.Visible))
        {
            var bounds = new RectangleF(x, y, w, h); targets[page] = bounds;
            if (!visuals.ContainsKey(page)) visuals[page] = new Visual { X = x + (opening.Contains(page) ? Px(12) : 0), Width = w, Opacity = opening.Contains(page) ? 0 : 1 };
            x += w + Px(4);
        }
        tabViewport = new RectangleF(Px(4), 0, Math.Max(0, x - Px(4)), Height);
        plus = new RectangleF(x + Px(4), (Height - Px(22)) / 2, Px(22), Px(22));
        left = geometry.Overflow ? new(plus.Right + Px(8), plus.Y, Px(22), Px(22)) : RectangleF.Empty;
        right = geometry.Overflow ? new(left.Right + Px(4), plus.Y, Px(22), Px(22)) : RectangleF.Empty;
        foreach (var page in visuals.Keys.Where(p => !pages.Contains(p)).ToArray()) { visuals.Remove(page); opening.Remove(page); }
        // Não manter retângulos e estados de centenas de abas fora da área visível.
        foreach (var page in visuals.Keys.Where(p => !targets.ContainsKey(p) && p != candidate).ToArray()) visuals.Remove(page);
    }
    void Frame(object? sender, EventArgs e)
    {
        if (IsDisposed) return;
        if (dragging) AutoScrollDrag();
        Arrange();
        float dt = (float)Math.Clamp(Stopwatch.GetElapsedTime(lastFrame).TotalMilliseconds, 1, 50); lastFrame = Stopwatch.GetTimestamp();
        float amount = SystemInformation.IsMenuAnimationEnabled ? 1 - MathF.Exp(-dt / 40) : 1;
        bool pending = false;
        foreach (var (page, target) in targets)
        {
            if (dragging && (page == candidate || dragGroup.Contains(page))) continue;
            var v = visuals[page];
            v.X += (target.X - v.X) * amount; v.Width += (target.Width - v.Width) * amount; v.Opacity += (1 - v.Opacity) * amount;
            if (Math.Abs(target.X - v.X) < .3f && Math.Abs(target.Width - v.Width) < .3f && v.Opacity > .99f)
            { v.X = target.X; v.Width = target.Width; v.Opacity = 1; opening.Remove(page); }
            else pending = true;
        }
        Invalidate();
        if (!pending && !dragging) animation.Stop();
    }
    static GraphicsPath Shape(RectangleF b, float radius)
    {
        var p = new GraphicsPath(); float d = Math.Min(radius * 2, Math.Min(b.Width, b.Height));
        p.AddArc(b.X, b.Y, d, d, 180, 90); p.AddArc(b.Right - d, b.Y, d, d, 270, 90);
        p.AddArc(b.Right - d, b.Bottom - d, d, d, 0, 90); p.AddArc(b.X, b.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p;
    }
    static Color Blend(Color a, Color b, float weight) => Color.FromArgb((int)(a.R + (b.R - a.R) * weight), (int)(a.G + (b.G - a.G) * weight), (int)(a.B + (b.B - a.B) * weight));
    RectangleF CloseBox(RectangleF b) => new(b.Right - Px(25), b.Y + (b.Height - Px(18)) / 2, Px(18), Px(18));
    RectangleF Ghost()
    {
        float width = candidate is not null && visuals.TryGetValue(candidate, out var v) ? v.Width : Px(150);
        return new(Math.Clamp(pointer.X - grabOffset, tabViewport.Left, Math.Max(tabViewport.Left, tabViewport.Right - width)), Px(6), width, Height - Px(12));
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); Arrange(); var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; hits.Clear();
        foreach (var (page, target) in targets.OrderBy(item => item.Key == model.SelectedTab))
        {
            if (dragging && (page == candidate || dragGroup.Contains(page))) continue;
            var v = visuals[page]; var b = new RectangleF(v.X, target.Y, v.Width, target.Height);
            // A janela mantém abas completas, sem desenhar uma metade na borda.
            if (b.Left < 0 || b.Right > tabViewport.Right + .5f) continue;
            hits[page] = b; DrawTab(g, page, b, v.Opacity, false);
        }
        if (dragging && candidate is not null)
        {
            var b=Ghost();
            if(dragGroup.Length>1)
            {
                float pitch=b.Width+Px(4);
                float first=Math.Clamp(b.X-Array.IndexOf(dragGroup,candidate)*pitch,tabViewport.Left,
                    Math.Max(tabViewport.Left,tabViewport.Right-Math.Min(tabViewport.Width,dragGroup.Length*pitch-Px(4))));
                foreach(var member in dragGroup)
                {
                    var at=new RectangleF(first,b.Y,b.Width,b.Height);
                    if(at.Right<=tabViewport.Right+.5f){DrawTab(g,member,at,1,true);hits[member]=at;}
                    first+=pitch;
                }
            }
            else{DrawTab(g,candidate,b,1,true);hits[candidate]=b;}
        }
        Circle(g, plus, "+", true);
        if (!left.IsEmpty) { Circle(g, left, "‹", false); Circle(g, right, "›", false); }
    }
    void DrawTab(Graphics g, TabPage page, RectangleF b, float opacity, bool ghost)
    {
        bool selected = model.SelectedTab == page;
        bool grouped = selection.Items.Contains(page) && selection.Items.Count>1;
        var color = selected ? (Dark ? Color.FromArgb(51, 65, 82) : Color.White) : page == hover || ghost ? (Dark ? Color.FromArgb(42, 54, 69) : Color.FromArgb(190, 207, 220)) : BackColor;
        if(grouped) color=Dark?Color.FromArgb(65,89,121):Color.FromArgb(187,214,244);
        if (selected || grouped || page == hover || ghost)
        {
            var shadow = b; shadow.Offset(0, Px(2));
            if (selected || ghost) { using var path = Shape(shadow, Px(6)); using var shade = new SolidBrush(Color.FromArgb(ghost ? 40 : 16, 40, 57, 75)); g.FillPath(shade, path); }
            using var body = Shape(b, Px(6)); using var fill = new SolidBrush(Blend(BackColor, color, opacity)); g.FillPath(fill, body);
            if(grouped){using var outline=new Pen(Dark?Color.FromArgb(139,182,230):Color.FromArgb(76,128,185),Px(1));g.DrawPath(outline,body);}
        }
        else
        { using var divider = new Pen(Dark ? Color.FromArgb(64, 78, 96) : Color.FromArgb(182, 197, 209)); g.DrawLine(divider, b.Right, b.Top + Px(8), b.Right, b.Bottom - Px(8)); }
        var text = new Rectangle((int)(b.X + Px(10)), (int)b.Y, Math.Max(0, (int)(b.Width - Px(38))), (int)b.Height);
        TextRenderer.DrawText(g, page.Text, Font, text, Blend(BackColor, ForeColor, opacity), TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        if (page == hover || ghost)
        {
            var box = CloseBox(b);
            if (box.Contains(pointer) && !ghost) { using var fill = new SolidBrush(Dark ? Color.FromArgb(79, 97, 120) : Color.FromArgb(174, 193, 208)); g.FillEllipse(fill, box); }
            using var pen = new Pen(Blend(BackColor, ForeColor, opacity), Math.Max(1, Px(1.1f)));
            float inset = Px(6);
            g.DrawLine(pen, box.Left + inset, box.Top + inset, box.Right - inset, box.Bottom - inset);
            g.DrawLine(pen, box.Left + inset, box.Bottom - inset, box.Right - inset, box.Top + inset);
        }
    }
    void Circle(Graphics g, RectangleF b, string text, bool isPlus)
    {
        if (isPlus || b.Contains(pointer)) { using var fill = new SolidBrush(Dark ? (b.Contains(pointer) ? Color.FromArgb(65, 82, 104) : Color.FromArgb(44, 57, 74)) : (b.Contains(pointer) ? Color.FromArgb(238, 244, 249) : Color.White)); g.FillEllipse(fill, b); }
        if (isPlus)
        {
            using var pen = new Pen(ForeColor, Math.Max(1, Px(1.3f))); float cx = b.X + b.Width / 2, cy = b.Y + b.Height / 2, arm = Px(4);
            g.DrawLine(pen, cx - arm, cy, cx + arm, cy); g.DrawLine(pen, cx, cy - arm, cx, cy + arm);
        }
        else TextRenderer.DrawText(g, text, Font, Rectangle.Round(b), ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
    TabPage? Hit(Point p)
    {
        if(!ClientRectangle.Contains(p))return null;
        // Match paint order during animations; close targets keep their original bounds.
        return hits.Reverse().FirstOrDefault(item=>TabLayout.HitBounds(item.Value,Height,Px(2)).Contains(p)).Key;
    }
    internal bool HasTabAtScreenPoint(Point screen) => Hit(PointToClient(screen)) is not null;
    bool AtClose(TabPage page, Point p) => hits.TryGetValue(page, out var b) && CloseBox(b).Contains(p);
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e); var oldPointer = pointer; pointer = e.Location;
        if (candidate is not null && (e.Button & MouseButtons.Left) != 0)
        {
            if (!dragging && (Math.Abs(e.X - origin.X) > SystemInformation.DragSize.Width / 2 || Math.Abs(e.Y - origin.Y) > SystemInformation.DragSize.Height / 2))
            {
                dragging=true;Capture=true;
                dragGroup=Pages.Where(p=>selection.Items.Contains(p)).ToArray();
                if(dragGroup.Length==0)dragGroup=[candidate];
                destination=Pages.IndexOf(dragGroup.Length>1?dragGroup[0]:candidate);tooltip.Hide(this);
            }
            if (dragging) { UpdateDestination(); StartAnimation(); Invalidate(); return; }
        }
        var next = Hit(pointer); bool close = next is not null && AtClose(next, pointer);
        bool previousButton = plus.Contains(oldPointer) || left.Contains(oldPointer) || right.Contains(oldPointer);
        bool nextButton = plus.Contains(pointer) || left.Contains(pointer) || right.Contains(pointer);
        if (next != hover || close != hotClose || nextButton != previousButton)
        { hover = next; hotClose = close; tooltip.SetToolTip(this, next?.ToolTipText ?? (plus.Contains(pointer) ? "Nova aba" : "")); Invalidate(); }
        Cursor = close || nextButton ? Cursors.Hand : Cursors.Default;
    }
    void UpdateDestination()
    {
        var pages = Pages; if (candidate is null || pages.Count == 0) return;
        var geometry = TabLayout.Calculate(pages.Count, Width / DpiFactor, start, pages.IndexOf(model.SelectedTab!), false);
        if(dragGroup.Length>1)
        {
            destination=Math.Clamp(start+(int)Math.Floor((pointer.X-Px(8))/Px(geometry.Width+4)+.5f),start,Math.Min(pages.Count,start+geometry.Visible));
            Arrange();return;
        }
        float center = pointer.X - grabOffset + Px(geometry.Width) / 2;
        destination = Math.Clamp(start + (int)Math.Floor((center - Px(8)) / Px(geometry.Width + 4)), start, Math.Min(pages.Count - 1, start + geometry.Visible - 1));
        Arrange();
    }
    void AutoScrollDrag()
    {
        if (Stopwatch.GetElapsedTime(lastScroll).TotalMilliseconds < 300) return;
        var pages = Pages; var geometry = TabLayout.Calculate(pages.Count, Width / DpiFactor, start, -1, false);
        if (pointer.X < Px(16) && start > 0) start--;
        else if (pointer.X > tabViewport.Right - Px(16) && start + geometry.Visible < pages.Count) start++;
        else return;
        lastScroll = Stopwatch.GetTimestamp(); UpdateDestination();
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e); pointer = e.Location; origin = e.Location; var page = Hit(pointer);
        selection.Prune(Pages);
        if(e.Button==MouseButtons.Right && page is not null)
        {
            if(!selection.Items.Contains(page))
            {
                selection.Click(Pages,page,false);
                rangeClick=true;try{model.SelectedTab=page;}finally{rangeClick=false;}
            }
            groupMenu.Items[0].Text=selection.Items.Count>1?$"Fechar {selection.Items.Count} abas selecionadas":"Fechar aba";
            groupMenu.BackColor=Dark?BrowserTheme.Surface:SystemColors.Control;
            groupMenu.ForeColor=Dark?BrowserTheme.Text:SystemColors.ControlText;
            groupMenu.Renderer=Dark?new ToolStripProfessionalRenderer(new DarkMenuColors()):new ToolStripProfessionalRenderer();
            Invalidate();groupMenu.Show(this,e.Location);return;
        }
        if (e.Button == MouseButtons.Middle && page is not null) { model.RequestClose(page); return; }
        if (e.Button != MouseButtons.Left) return;
        if (page is not null && AtClose(page, pointer)) { pressedClose = page; Capture = true; return; }
        if (plus.Contains(pointer)) { NewRequested?.Invoke(); return; }
        if (left.Contains(pointer) || right.Contains(pointer)) { start += left.Contains(pointer) ? -1 : 1; reveal = false; RefreshLayout(); return; }
        if (page is null) { EmptyDragRequested?.Invoke(e.Clicks > 1); return; }
        if (page is not null)
        {
            bool extend=(ModifierKeys&Keys.Shift)!=0;
            if(extend && selection.Items.Count==0 && model.SelectedTab is { } active && active!=plusPage)selection.Click(Pages,active,false);
            if(extend || !selection.Items.Contains(page) || selection.Items.Count<=1)
                selection.Click(Pages,page,extend);
            rangeClick=true;try{model.SelectedTab=page;}finally{rangeClick=false;}
            if(!extend){candidate=page;grabOffset=pointer.X-hits[page].Left;}
            Invalidate();Focus();
        }
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e); if (e.Button != MouseButtons.Left) return;
        var close=pressedClose;var page=candidate;bool moved=dragging;int target=destination;var group=dragGroup;
        if(moved && page is not null && visuals.TryGetValue(page,out var v))v.X=Ghost().X;
        pressedClose=null;candidate=null;dragging=false;dragGroup=[];Capture=false;
        if(close is not null && AtClose(close,e.Location))model.RequestClose(close);
        else if(moved && page is not null && ClientRectangle.Contains(e.Location))
        {
            rangeClick=true;
            try {if(group.Length>1)MoveGroupRequested?.Invoke(group,target);else model.RequestMove(page,target);}
            finally{rangeClick=false;}
            if(model.SelectedTab is { } active && active!=plusPage)
                selection.Click(Pages,active,false);
        }
        else if(!moved && page is not null)selection.Click(Pages,page,false);
        RefreshLayout();
    }
    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        if (!Capture && (dragging || pressedClose is not null)) { dragging = false; dragGroup=[]; candidate = null; pressedClose = null; RefreshLayout(); }
        base.OnMouseCaptureChanged(e);
    }
    protected override void OnMouseLeave(EventArgs e)
    { base.OnMouseLeave(e); if (!dragging) { hover = null; hotClose = false; pointer = new(-1, -1); Invalidate(); } }
    protected override void OnMouseWheel(MouseEventArgs e)
    { base.OnMouseWheel(e); start -= Math.Sign(e.Delta); reveal = false; RefreshLayout(); }
    protected override void OnResize(EventArgs e) { base.OnResize(e); if (model is not null) { reveal = true; RefreshLayout(); } }
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); Height = (int)Px(36); RefreshLayout(); }
    protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); Height = (int)Px(36); visuals.Clear(); reveal = true; RefreshLayout(); }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e); var pages = Pages; int current = pages.IndexOf(model.SelectedTab!);
        if (pages.Count == 0) return;
        if (e.KeyCode is Keys.Left or Keys.Right) { model.SelectedTab = pages[(current + (e.KeyCode == Keys.Left ? -1 : 1) + pages.Count) % pages.Count]; e.Handled = true; }
        if (e.KeyCode == Keys.Escape && dragging) { candidate = null; dragging = false; dragGroup=[]; Capture = false; RefreshLayout(); e.Handled = true; }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            groupMenu.Dispose(); animation.Dispose(); tooltip.Dispose(); model.ControlAdded -= Added; model.ControlRemoved -= Removed;
            model.SelectedIndexChanged -= Selected; model.OpeningRequested -= Opened;
            foreach (TabPage p in model.TabPages) p.TextChanged -= TitleChanged;
        }
        base.Dispose(disposing);
    }
}
