using System.Diagnostics;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Volt;

static class Program
{
    [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet=System.Runtime.InteropServices.CharSet.Unicode)]
    static extern int SetCurrentProcessExplicitAppUserModelID(string appId);
    [STAThread]
    static void Main()
    {
        // Stable identity keeps the running window grouped under VOLT in the taskbar.
        SetCurrentProcessExplicitAppUserModelID("VOLT.Browser");
        ApplicationConfiguration.Initialize();
        Application.Run(new BrowserWindow());
    }
}

sealed class BrowserView : WebView2
{
    bool? lastVisible;
    public void SetPageVisible(bool value, bool force = false)
    {
        if (!force && lastVisible == value) return;
        Visible = value;
        // Um TabPage já oculto pode impedir um novo VisibleChanged.
        // Repropagar a visibilidade também nessa situação para o controle nativo.
        OnVisibleChanged(EventArgs.Empty);
        lastVisible = value;
    }
}

sealed class BrowserTab
{
    public TabPage Page { get; } = new("Nova guia");
    public BrowserView? View { get; set; }
    public string? Url { get; set; }
    public string Title { get; set; } = "Nova guia";
    public Image? Favicon { get; set; }
    public int IconRevision { get; set; }
    public bool Unloaded { get; set; }
    public string SleepResult { get; set; } = "Aguardando";
    public DateTime LoadStarted { get; set; }
    public bool DocumentReady { get; set; }
    public DateTime HiddenSince { get; set; } = DateTime.UtcNow;
    public bool Busy { get; set; }
    public bool Ready => View?.CoreWebView2 is not null;
    public bool Loading { get; set; }
    public bool KeepAwake { get; set; }
    public int Downloads { get; set; }
    public List<CoreWebView2Frame> Frames { get; } = new();
    public string? MediaScriptId { get; set; }
    public bool MediaInstalled { get; set; }
}

sealed partial class BrowserWindow : Form
{
    readonly HoverTabs tabs = new() { Dock = DockStyle.Fill, ShowToolTips = true };
    readonly TabPage plusTab = new("+") { ToolTipText = "Abrir nova aba ao lado da selecionada" };
    readonly TextBox address = new() { BorderStyle = BorderStyle.None, Dock = DockStyle.Fill, Font = new("Segoe UI", 12) };
    readonly Label status = new() { Dock = DockStyle.Bottom, Height = 26, TextAlign = ContentAlignment.MiddleLeft, Padding = new(12, 0, 0, 0) };
    readonly Dictionary<TabPage, BrowserTab> items = new();
    readonly System.Windows.Forms.Timer timer = new() { Interval = 15000 };
    readonly Button back = MakeButton("‹"), forward = MakeButton("›");
    readonly Button bookmarkButton = MakeButton("Favoritar"), downloadsButton = MakeButton("Downloads");
    readonly ToolTip chromeTips = new() { InitialDelay=400, ReshowDelay=100, AutoPopDelay=5000 };
    readonly ToolStripMenuItem pin = new("Manter esta aba ativa") { CheckOnClick = true };
    CoreWebView2Environment? environment;
    Task<CoreWebView2Environment>? environmentTask;
    readonly string themeFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VOLT", "theme.txt");
    bool darkMode;
    bool blockTrackers = true;
    bool deepEconomy;
    bool changingTabs;
    bool movingTabs;
    bool pauseBackgroundMedia = true;
    long mediaRevision;
    int blocked;
    string memory = "";
    long measuredBytes;
    bool validMemorySample;
    bool automaticMemory = true;
    bool memoryPressure;
    DateTime lastMemoryRelease;
    DateTime measured;
    BrowserTab? previous;
    bool sweeping;
    bool closing;
    bool economy = true;
    BrowserTab? Current => tabs.SelectedTab is { } page && items.TryGetValue(page, out var tab) ? tab : null;

    public BrowserWindow()
    {
        LoadBrowsingPreferences();
        Text = "VOLT — Navegue sem peso";
        using(var iconStream=typeof(BrowserWindow).Assembly.GetManifestResourceStream("Volt.AppIcon")!)
        using(var embeddedIcon=new Icon(iconStream)) Icon=(Icon)embeddedIcon.Clone();
        ShowIcon=true;
        Size = new(1240, 820);
        FormBorderStyle = FormBorderStyle.None;
        Padding = new(2);
        MinimumSize = new(560, 360);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(240, 243, 248);
        Font = new("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        var toolbar = new TableLayoutPanel
        {
            Dock=DockStyle.Top, Height=42, ColumnCount=9, RowCount=1,
            Padding=new(6,4,6,4), AutoSize=false, GrowStyle=TableLayoutPanelGrowStyle.FixedSize, Tag="chrome"
        };
        toolbar.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        foreach(int width in new[]{30,30,32,32}) toolbar.ColumnStyles.Add(new(SizeType.Absolute,width));
        toolbar.ColumnStyles.Add(new(SizeType.Percent,100));
        foreach(int width in new[]{34,34,40,36}) toolbar.ColumnStyles.Add(new(SizeType.Absolute,width));
        var reload=MakeButton("↻");var home=MakeButton("Início");
        var menu=MakeButton("Menu");var favorite=bookmarkButton;var downloads=downloadsButton;
        favorite.Click+=(_,_)=>AddBookmark();
        downloads.Click+=(_,_)=>{if(Current?.Ready==true)Current.View!.CoreWebView2.OpenDefaultDownloadDialog();};
        toolbar.Controls.Add(back,0,0);toolbar.Controls.Add(forward,1,0);toolbar.Controls.Add(reload,2,0);toolbar.Controls.Add(home,3,0);
        var addressPanel=new RoundedSurface {Dock=DockStyle.Fill,Padding=new(12,0,12,0),BackColor=Color.White,Tag="surface",Margin=new(4,1,6,1)};
        address.Font=new("Segoe UI",10);var addressHost=new CenteredTextHost(address) {Dock=DockStyle.Fill,Tag="surface",BackColor=Color.White};
        addressPanel.Controls.Add(addressHost);toolbar.Controls.Add(addressPanel,4,0);
        var themeToggle=MakeButton("Tema");
        themeToggle.AccessibleDescription="Alternar entre os temas claro e escuro com um clique";
        toolbar.Controls.Add(favorite,5,0);toolbar.Controls.Add(downloads,6,0);toolbar.Controls.Add(themeToggle,7,0);toolbar.Controls.Add(menu,8,0);
        tabs.TabPages.Add(plusTab);
        var tabHeader=new BrowserTabsHeader(tabs,plusTab) {Dock=DockStyle.Fill};
        browserTabHeader=tabHeader;
        tabHeader.NewRequested+=async()=>await AddTab();
        tabHeader.CloseGroupRequested+=CloseTabGroup;
        tabHeader.MoveGroupRequested+=MoveTabGroup;
        tabHeader.EmptyDragRequested+=doubleClick=>{if(doubleClick)ToggleMaximize();else DragWindow();};
        var titleBar=new Panel {Dock=DockStyle.Top,Height=36,Tag="chrome"};
        var caption=new TableLayoutPanel {Dock=DockStyle.Right,Width=126,ColumnCount=3,RowCount=1,Tag="chrome",Margin=Padding.Empty};
        caption.RowStyles.Add(new(SizeType.Percent,100));
        foreach(int i in new[]{0,1,2}) caption.ColumnStyles.Add(new(SizeType.Percent,33.333f));
        var minimize=MakeButton("Minimizar");var maximize=MakeButton("Maximizar");var exit=MakeButton("Fechar janela");
        minimize.Click+=(_,_)=>WindowState=FormWindowState.Minimized;maximize.Click+=(_,_)=>ToggleMaximize();exit.Click+=(_,_)=>Close();
        Resize+=(_,_)=>
        {
            ((ChromeButton)maximize).Kind=WindowState==FormWindowState.Maximized?"restore":"maximize";
            int top=WindowState==FormWindowState.Maximized?0:2;
            if(Padding.Top!=top)Padding=new(2,top,2,2);
            maximize.Invalidate();
        };
        caption.Controls.Add(minimize,0,0);caption.Controls.Add(maximize,1,0);caption.Controls.Add(exit,2,0);
        titleBar.Controls.Add(tabHeader);titleBar.Controls.Add(caption);
        var content=new BrowserContentPanel {Dock=DockStyle.Fill,Tag="surface"};content.Controls.Add(tabs);
        Controls.Add(content);Controls.Add(bookmarkBar);Controls.Add(toolbar);Controls.Add(titleBar);Controls.Add(status);
        foreach(var button in toolbar.Controls.OfType<Button>().Concat(caption.Controls.OfType<Button>()))
            chromeTips.SetToolTip(button,button.AccessibleName);
        status.Height=22;status.Font=new("Segoe UI",8);status.Padding=new(10,0,0,0);status.Tag="chrome";
        InitializeSidebar(content);
        tabs.CloseRequested += page => { if (items.TryGetValue(page, out var tab)) CloseTab(tab); };
        tabs.ReorderRequested += MoveTab;
        tabs.Selecting += (_, e) =>
        {
            if (e.TabPage != plusTab) return;
            e.Cancel = true;
            if (changingTabs) return;
            BeginInvoke(new Action(() => { if (!closing) _ = AddTab(); }));
        };
        back.Click += (_, _) => { if (Current?.View?.CanGoBack == true) Current.View!.GoBack(); };
        forward.Click += (_, _) => { if (Current?.View?.CanGoForward == true) Current.View!.GoForward(); };
        reload.Click += (_, _) => { if (Current?.Ready == true) Current.View!.Reload(); };
        home.Click += (_, _) => { if (Current is { } tab && (!tab.Ready || MessageBox.Show(this, "Voltar ao início? A página atual será fechada. Salve alterações e aguarde downloads antes de continuar.", "VOLT", MessageBoxButtons.YesNo) == DialogResult.Yes)) ShowHome(tab); };
        address.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Navigate(address.Text); } };
        tabs.SelectedIndexChanged += (_, _) => SelectTab();
        try { darkMode = !File.Exists(themeFile) || File.ReadAllText(themeFile).Trim() == "dark"; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        var popup = new ContextMenuStrip();
        var dark = new ToolStripMenuItem("Modo escuro") { Checked = darkMode, CheckOnClick = true };
        void UpdateThemeToggle()
        {
            ((ChromeButton)themeToggle).IsOn = darkMode;
            themeToggle.Invalidate();
            themeToggle.AccessibleName = darkMode ? "Ativar modo claro" : "Ativar modo escuro";
            chromeTips.SetToolTip(themeToggle,themeToggle.AccessibleName);
        }
        themeToggle.Click += (_, _) => dark.Checked = !dark.Checked;
        UpdateThemeToggle();
        dark.CheckedChanged += (_, _) =>
        {
            darkMode = dark.Checked;
            UpdateThemeToggle();
            using(var redraw=new ThemeRedraw(this)) BrowserTheme.Apply(this, darkMode);
            addressSuggestions?.ApplyTheme(darkMode);
            popup.Renderer = darkMode ? new ToolStripProfessionalRenderer(new DarkMenuColors()) : new ToolStripProfessionalRenderer();
            popup.BackColor = darkMode ? BrowserTheme.Surface : SystemColors.Control;
            popup.ForeColor = darkMode ? BrowserTheme.Text : SystemColors.ControlText;
            // Shared profile: publish its color preference once, after native chrome is repainted.
            BeginInvoke(new Action(() =>
            {
                if(closing) return;
                var ready=items.Values.FirstOrDefault(t=>t.Ready);
                if(ready is not null) SetWebTheme(ready.View!);
                else if(whatsAppView?.CoreWebView2 is not null) SetWebTheme(whatsAppView);
            }));
            try { Directory.CreateDirectory(Path.GetDirectoryName(themeFile)!); File.WriteAllText(themeFile, darkMode ? "dark" : "light"); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            { MessageBox.Show(this, "O tema foi aplicado, mas não foi possível salvar a preferência.", "VOLT"); }
        };
        popup.Items.Add("Senhas — importar e gerenciar…",null,(_,_)=>ShowPasswords());
        popup.Items.Add(dark);
        BrowserTheme.Apply(this, darkMode);
        if (darkMode) { popup.Renderer = new ToolStripProfessionalRenderer(new DarkMenuColors()); popup.BackColor = BrowserTheme.Surface; popup.ForeColor = BrowserTheme.Text; }
        var saver = new ToolStripMenuItem("Suspender abas paradas") { Checked = true, CheckOnClick = true };
        saver.CheckedChanged += (_, _) => { economy = saver.Checked; if (!economy) foreach (var tab in items.Values) if (tab.Ready) tab.View!.CoreWebView2.Resume(); UpdateChrome(); };
        pin.CheckedChanged += (_, _) => { if (Current is { } tab) tab.KeepAwake = pin.Checked; };
        popup.Items.Add(saver); popup.Items.Add(pin);
        var mediaPolicy = new ToolStripMenuItem("Pausar mídia ao sair da aba") { Checked = true, CheckOnClick = true };
        mediaPolicy.CheckedChanged += async (_, _) =>
        {
            pauseBackgroundMedia = mediaPolicy.Checked;
            mediaPolicy.Enabled = false;
            try
            {
                foreach (var tab in items.Values.ToArray())
                    if (tab.Ready && tab.MediaInstalled && tab.View is { } web) await InstallMediaGuard(tab, web);
            }
            finally { if (!closing) mediaPolicy.Enabled = true; }
        };
        popup.Items.Add(mediaPolicy);
        popup.Items.Add("Suspender abas inativas agora", null, async (_, _) => await SuspendTabs(true));
        var blockers = new ToolStripMenuItem("Bloquear rastreadores conhecidos") { Checked = true, CheckOnClick = true };
        blockers.CheckedChanged += (_, _) => { blockTrackers = blockers.Checked; UpdateChrome(); };
        popup.Items.Add(blockers);
        popup.Items.Add("Liberar abas em segundo plano...", null, (_, _) => UnloadBackgroundTabs());
        var deep = new ToolStripMenuItem("Economia forte: descartar após o prazo") { CheckOnClick = false };
        deep.Click += (_, _) =>
        {
            if (!deepEconomy && MessageBox.Show(this,
                "Ativar economia forte? Abas elegíveis serão descartadas depois do prazo configurado no menu, começando pelas menos recentes. A opção Nunca desativa o descarte por tempo. Elas recarregam ao voltar. Áudio, downloads e abas marcadas como ativas podem manter o total acima de 2. Histórico da aba, formulários, rolagem e posição de vídeo podem ser perdidos. Marque Manter esta aba ativa para editores, uploads, chamadas e mensagens importantes. A detecção dessas atividades não é completa. Áudio, downloads e carregamentos têm proteção.",
                "Economia forte", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes) return;
            deepEconomy = !deepEconomy; deep.Checked = deepEconomy; UpdateChrome();
        };
        popup.Items.Add(deep);
        var automatic = new ToolStripMenuItem("Liberar abas automaticamente acima de 1,2 GB") { Checked = true, CheckOnClick = true };
        automatic.CheckedChanged += (_, _) => { automaticMemory = automatic.Checked; memoryPressure = false; };
        popup.Items.Add(automatic);
        popup.Items.Add("Diagnóstico das abas", null, (_, _) => ShowTabDiagnostics());
        popup.Items.Add("Memória e otimizações", null, (_, _) => MessageBox.Show(this,
            "VOLT 0.6.5 • versão experimental\n\nInício nativo: novas abas não carregam WebView2. O prazo de hibernação é configurável no menu. Economia forte descarta abas elegíveis após esse prazo. As primeiras N abas podem ser protegidas pela posição. A limpeza por pressão de RAM é independente do prazo. Áudio, downloads, carregamento e abas marcadas como ativas são preservados.\n\nLiberar abas em segundo plano descarta a página e exige recarregar ao voltar. Use só depois de salvar seu trabalho e terminar uploads/chamadas. Também pode ser automatizado pelo modo opcional de economia forte.\n\nRAM estimada: soma do Working Set do VOLT e dos processos do mesmo perfil WebView2. Pode contar memória compartilhada mais de uma vez; difere do Gerenciador de Tarefas e não inclui crashpad. Se outra janela VOLT compartilhar o perfil, o motor compartilhado também entra na soma.\n\nO bloqueio usa uma pequena lista local de domínios. Se um site falhar, desligue a opção e recarregue. Não é um bloqueador completo de anúncios.\n\nGanho ainda não medido no Windows.", "VOLT"));
        menu.Click += (_, _) => { pin.Checked = Current?.KeepAwake == true; popup.Show(menu, new Point(0, menu.Height)); };
        InitializeBookmarks();
        InitializeBrowsingOptions(popup);
        InitializeWindowPlacement();
        timer.Tick += async (_, _) => { await SuspendTabs(false); await SweepWhatsApp(); UpdateChrome(); ReleaseMemoryPressure(); };
        Shown += async (_, _) => { await AddTab(); timer.Start(); };
        FormClosing += (_, _) => { closing = true; addressSuggestions?.Dispose(); timer.Stop(); outsideWhatsApp?.Dispose(); DisposeWhatsApp(); sidebarLogo?.Image?.Dispose(); foreach (var tab in items.Values) { DisposeView(tab); ClearFavicon(tab); } timer.Dispose(); popup.Dispose(); chromeTips.Dispose(); };
    }

    void SetWebTheme(BrowserView view)
    {
        view.DefaultBackgroundColor = darkMode ? BrowserTheme.Background : Color.White;
        view.CoreWebView2.Profile.PreferredColorScheme = darkMode
            ? CoreWebView2PreferredColorScheme.Dark : CoreWebView2PreferredColorScheme.Light;
    }

    static Button MakeButton(string label) => new ChromeButton
    {
        Text=label, Dock=DockStyle.Fill, AutoSize=false, BackColor=Color.FromArgb(239,244,248),
        ForeColor=Color.FromArgb(57,72,89), Font=new("Segoe UI",10), Margin=new(1), Padding=Padding.Empty,
        Tag="chrome", AccessibleName=label,
        Kind=label switch { "‹"=>"back", "›"=>"forward", "↻"=>"reload", "Início"=>"home", "Menu"=>"menu", "Favoritar"=>"favorite", "Downloads"=>"download", "Tema"=>"theme", "Minimizar"=>"minimize", "Maximizar"=>"maximize", "Fechar janela"=>"close", _=>"" }
    };

    async Task AddTab(string? url = null)
    {
        if (closing) return;
        BrowserTab tab;
        using(var redraw=new ThemeRedraw(this))
        {
            tab=new BrowserTab();tab.Page.Tag=tab;tab.Page.Bounds=tabs.DisplayRectangle;
            tab.Page.BackColor=darkMode?BrowserTheme.Background:Color.FromArgb(240,243,248);
            items.Add(tab.Page,tab);
            // Build and theme the page before selecting it; the old frame covers native handle creation.
            ShowHome(tab);
            int insert=ResourcePolicy.NewTabIndex(tabs.SelectedIndex,items.Count-1);
            changingTabs=true;
            try{tabs.TabPages.Insert(insert,tab.Page);tabs.SelectedTab=tab.Page;}
            finally{changingTabs=false;}
            tabs.MarkOpening(tab.Page);UpdateChrome();
        }
        if (url is null)
        {
            // Finish the originating mouse/keyboard event before assigning focus.
            BeginInvoke(new Action(() =>
            {
                if (closing || Current != tab || tab.Url is not null) return;
                var home = tab.Page.Controls.OfType<HomePage>().FirstOrDefault();
                home?.FocusSearch();
            }));
        }
        if (url is not null) await NavigateTab(tab, url);
        ResumePriorityTabs();UpdateChrome();
    }

    void DisposeView(BrowserTab tab)
    {
        var view = tab.View;
        tab.View = null; tab.Loading = false; tab.Downloads = 0;
        tab.Frames.Clear(); tab.MediaScriptId = null; tab.MediaInstalled = false;
        if (view is not null) { tab.Page.Controls.Remove(view); view.Dispose(); }
    }

    static void ClearPage(BrowserTab tab)
    {
        foreach (Control control in tab.Page.Controls.Cast<Control>().ToArray()) control.Dispose();
        tab.Page.Controls.Clear();
    }

    void ShowHome(BrowserTab tab)
    {
        // Repeated Home clicks keep the existing native controls and their layout.
        if (tab.View is null && tab.Page.Controls.OfType<HomePage>().Any()) return;
        var homePage = new HomePage(input => { _ = NavigateTab(tab, ResourcePolicy.Address(input)); })
        {
            Bounds = tab.Page.ClientRectangle,
            Visible = false
        };
        tab.Page.SuspendLayout();
        try
        {
            DisposeView(tab); ClearPage(tab);
            tab.Page.Controls.Add(homePage);
            tab.Page.ResumeLayout(true);
            BrowserTheme.Apply(tab.Page, darkMode);
            homePage.PerformLayout();
            homePage.Visible = true;
        }
        finally { tab.Page.ResumeLayout(true); }
        ClearFavicon(tab); tab.Url = null; tab.Unloaded = false; tab.SleepResult = "Início nativo (sem motor)"; tab.Title = "Nova guia"; tab.Page.Text = tab.Title;
        UpdateChrome();
    }

    async Task NavigateTab(BrowserTab tab, string url)
    {
        if (closing || !items.ContainsKey(tab.Page) || !ResourcePolicy.IsHttp(url)) return;
        tab.Url = url; tab.Unloaded = false;
        if (tab.Ready) { tab.View!.CoreWebView2.Resume(); tab.View.CoreWebView2.Navigate(url); return; }
        // Uma navegação nova durante a criação atualiza o destino sem criar outro motor.
        if (tab.View is not null) return;
        ClearPage(tab);
        var view = new BrowserView { Dock = DockStyle.Fill, Visible = tab == Current, DefaultBackgroundColor = darkMode ? BrowserTheme.Background : Color.White };
        tab.View = view; tab.Page.Controls.Add(view); tab.Loading = true;
        try
        {
            var profile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VOLT", "Profile");
            environmentTask ??= CoreWebView2Environment.CreateAsync(null, profile);
            var env = await environmentTask;
            if (closing || tab.View != view) return;
            environment = env;
            await view.EnsureCoreWebView2Async(env);
            if (tab.View == view && !closing) SetWebTheme(view);
            if (closing || tab.View != view) return;
            view.SetPageVisible(tab == Current, true);
            var core = view.CoreWebView2;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.IsStatusBarEnabled = true;
            // Só intercepta domínios da lista, sem trazer todo o tráfego para a UI.
            foreach (var domain in ResourcePolicy.TrackerDomains)
                foreach (var scheme in new[] { "https", "http" })
                    foreach (var host in new[] { domain, "*." + domain })
                        core.AddWebResourceRequestedFilter(scheme + "://" + host + "/*", CoreWebView2WebResourceContext.All, CoreWebView2WebResourceRequestSourceKinds.All);
            core.WebResourceRequested += (_, e) =>
            {
                if (!blockTrackers || e.ResourceContext == CoreWebView2WebResourceContext.Document || !ResourcePolicy.IsTracker(e.Request.Uri)) return;
                e.Response = env.CreateWebResourceResponse(null, 204, "No Content", "Cache-Control: no-store");
                blocked++;
            };
            core.NavigationStarting += (_, e) =>
            {
                if (!ResourcePolicy.IsHttp(e.Uri) && e.Uri != "about:blank") { e.Cancel = true; tab.Loading = false; return; }
                ClearFavicon(tab);
                tab.Loading = true; tab.DocumentReady = false; tab.LoadStarted = DateTime.UtcNow;
            };
            core.FrameCreated += (_, e) =>
            {
                var frame = e.Frame; tab.Frames.Add(frame);
                frame.Destroyed += (_, _) => tab.Frames.Remove(frame);
                frame.DOMContentLoaded += (_, _) => { _ = ApplyMediaGuard(tab); };
            };
            core.IsDocumentPlayingAudioChanged += (_, _) =>
            {
                if (pauseBackgroundMedia && tab != Current) _ = ApplyMediaGuard(tab);
            };
            core.DOMContentLoaded += (_, _) => { tab.DocumentReady = true; _ = ApplyMediaGuard(tab); };
            core.NavigationCompleted += (_, e) =>
            {
                tab.Loading = false; _ = ApplyMediaGuard(tab);
                if(!closing && tab.View==view && e.IsSuccess) RememberAddress(core.Source);
                if (Current == tab && !e.IsSuccess) status.Text = "Não foi possível carregar a página: " + e.WebErrorStatus;
            };
            core.FaviconChanged += async (_, _) => await UpdateFavicon(tab,view);
            core.SourceChanged += (_, _) => { if (ResourcePolicy.IsHttp(core.Source)) tab.Url = core.Source; if (Current == tab) UpdateChrome(); };
            core.HistoryChanged += (_, _) => { if (Current == tab) UpdateChrome(); };
            core.DocumentTitleChanged += (_, _) =>
            {
                tab.Title = core.DocumentTitle;
                tab.Page.Text = tab.Title.Length > 24 ? tab.Title[..24] + "…" : tab.Title;
                tab.Page.ToolTipText = tab.Title;
            };
            core.NewWindowRequested += async (_, e) => { e.Handled = true; if (ResourcePolicy.IsHttp(e.Uri)) await AddTab(e.Uri); };
            core.DownloadStarting += (_, e) =>
            {
                tab.Loading = false; tab.Downloads++; core.Resume();
                var operation = e.DownloadOperation; bool finished = false;
                operation.StateChanged += (_, _) =>
                {
                    if (tab.View == view && !finished && operation.State != CoreWebView2DownloadState.InProgress)
                    { finished = true; tab.Downloads = Math.Max(0, tab.Downloads - 1); }
                };
            };
            core.ProcessFailed += (_, _) => { if (!closing) status.Text = "Uma página parou de responder. Recarregue a aba."; };
            await InstallMediaGuard(tab, view);
            if (closing || tab.View != view) return;
            core.Navigate(tab.Url!);
            UpdateChrome();
        }
        catch (Exception ex)
        {
            if (environmentTask?.IsFaulted == true) environmentTask = null;
            if (!closing && tab.View == view && items.ContainsKey(tab.Page))
            {
                DisposeView(tab); tab.Unloaded = true; tab.Page.Text = "Reabrir aba";
                var retry = new Button { Text = "Tentar abrir novamente", Dock = DockStyle.Top, Height = 50 };
                retry.Click += async (_, _) => { if (tab.Url is { } target) await NavigateTab(tab, target); };
                tab.Page.Controls.Add(retry);
                MessageBox.Show(this, "Não foi possível iniciar a página. Verifique o WebView2 Runtime.\n\n" + ex.Message, "VOLT");
            }
        }
    }

    async void Navigate(string input)
    {
        if (Current is not { } tab || string.IsNullOrWhiteSpace(input)) return;
        await NavigateTab(tab, ResourcePolicy.Address(input));
        if (!closing && Current == tab && tab.Ready) tab.View!.Focus();
    }

    void SelectTab()
    {
        if (closing || movingTabs) return;
        var oldTab = previous;
        if (oldTab is not null && oldTab != Current) oldTab.HiddenSince = DateTime.UtcNow;
        previous = Current;
        // Não depender apenas da visibilidade herdada do TabPage:
        // o controlador WebView2 precisa ficar explicitamente oculto.
        foreach (var other in items.Values)
            if (other.View is { } web && other != Current) web.SetPageVisible(false);
        if (Current is { } tab)
        {
            if (tab.View is { } web) web.SetPageVisible(true);
            if (tab.Ready) { tab.View!.CoreWebView2.Resume(); tab.SleepResult = "Em uso"; }
            else if (tab.Unloaded && tab.Url is { } url) _ = NavigateTab(tab, url);
        }
        if (oldTab is not null && oldTab != Current && oldTab.Ready) _ = ApplyMediaGuard(oldTab);
        if (Current?.Ready == true) _ = ApplyMediaGuard(Current);
        UpdateChrome();
    }

    async Task InstallMediaGuard(BrowserTab tab, BrowserView view)
    {
        try
        {
            bool enabled;
            do
            {
                enabled = pauseBackgroundMedia;
                if (tab.MediaScriptId is { } old) view.CoreWebView2.RemoveScriptToExecuteOnDocumentCreated(old);
                string id = await view.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(MediaGuard.Script(enabled, tab != Current, 0));
                if (closing || tab.View != view) return;
                tab.MediaScriptId = id;
            } while (enabled != pauseBackgroundMedia);
            tab.MediaInstalled = true;
            await ApplyMediaGuard(tab);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Runtime.InteropServices.COMException)
        { if (!closing) tab.SleepResult = "Não foi possível configurar a pausa de mídia"; }
    }

    async Task ApplyMediaGuard(BrowserTab tab)
    {
        if (closing || !tab.Ready || tab.View is not { } view) return;
        string script = MediaGuard.Update(pauseBackgroundMedia, tab != Current, ++mediaRevision);
        // Uma página suspensa já não reproduz. Não acordá-la só para atualizar a política.
        try
        {
            if (view.CoreWebView2.IsSuspended) return;
            await view.CoreWebView2.ExecuteScriptAsync(script);
            if (closing || tab.View != view) return;
            foreach (var frame in tab.Frames.ToArray())
            {
                if (closing || tab.View != view) return;
                try { await frame.ExecuteScriptAsync(script); }
                catch (Exception ex) when (ex is InvalidOperationException or System.Runtime.InteropServices.COMException) { }
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Runtime.InteropServices.COMException) { }
    }

    void MoveTab(TabPage page, int slot)
    {
        // Single and group drags share the same transaction: never detach the active page.
        MoveTabGroup(new[]{page},slot);
    }

    void MoveTabGroup(IReadOnlyList<TabPage> pages,int slot)
    {
        if(closing)return;
        var before=tabs.TabPages.Cast<TabPage>().Where(items.ContainsKey).ToList();
        var group=before.Where(pages.Contains).ToArray();
        if(group.Length==0)return;
        var after=TabBlockMove.Move(before,group,slot);
        if(before.SequenceEqual(after))return;
        var selected=tabs.SelectedTab;
        using(var redraw=new ThemeRedraw(this))
        {
            changingTabs=true;movingTabs=true;tabs.SuspendLayout();
            try
            {
                if(selected is not null && group.Contains(selected))
                {
                    // Keep the visible page attached; move its siblings around it.
                    foreach(var page in before.Where(p=>p!=selected))tabs.TabPages.Remove(page);
                    for(int i=0;i<after.Count;i++)if(after[i]!=selected)tabs.TabPages.Insert(i,after[i]);
                }
                else
                {
                    foreach(var page in group)tabs.TabPages.Remove(page);
                    int insert=after.IndexOf(group[0]);
                    foreach(var page in group)tabs.TabPages.Insert(insert++,page);
                }
                if(selected is not null && items.ContainsKey(selected))tabs.SelectedTab=selected;
            }
            finally{movingTabs=false;changingTabs=false;tabs.ResumeLayout(true);}
            SelectTab();ResumePriorityTabs();
        }
    }

    bool StillLoading(BrowserTab tab) => tab.Loading && (!tab.DocumentReady || DateTime.UtcNow - tab.LoadStarted < TimeSpan.FromSeconds(30));
    string Protection(BrowserTab tab) => ResourcePolicy.SleepProtection(tab == Current, tab.Ready, StillLoading(tab), (tab.KeepAwake || HasPositionPriority(tab)), tab.Downloads, tab.Ready && tab.View!.CoreWebView2.IsDocumentPlayingAudio);
    bool CanSleep(BrowserTab tab) => Protection(tab).Length == 0;

    void UnloadTab(BrowserTab tab)
    {
        DisposeView(tab); ClearPage(tab); tab.Unloaded = true; tab.SleepResult = "Liberada; recarrega ao selecionar";
        measured = DateTime.MinValue;
    }

    void ShowTabDiagnostics()
    {
        var details = new List<string>();
        foreach (TabPage page in tabs.TabPages)
        {
            if (!items.TryGetValue(page, out var tab)) continue;
            string reason = Protection(tab);
            string state = tab.Unloaded ? "Liberada" : tab.Ready && tab.View!.CoreWebView2.IsSuspended ? "Suspensa" : reason.Length > 0 ? reason : tab.SleepResult;
            details.Add(tab.Title + "\n" + state + $" • {Math.Max(0, (int)(DateTime.UtcNow - tab.HiddenSince).TotalSeconds)} s desde a última saída da aba");
        }
        using var dialog = new Form { Text = "Diagnóstico das abas — VOLT 0.6.5", Size = new(720, 500), StartPosition = FormStartPosition.CenterParent };
        dialog.Controls.Add(new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, Text = string.Join("\r\n\r\n", details), Font = new("Segoe UI", 11) });
        dialog.ShowDialog(this);
    }

    void UnloadBackgroundTabs()
    {
        if (sweeping) return;
        if (MessageBox.Show(this, "Descarregar páginas em segundo plano? Elas serão recarregadas ao voltar. Histórico desta aba, posição de vídeos, formulários e dados não salvos podem ser perdidos. Termine uploads e chamadas antes. Abas com áudio, downloads ou marcadas como ativas serão preservadas.", "Liberar memória", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes) return;
        ReleaseEligibleBackgroundTabs(false);
        measured = DateTime.MinValue; UpdateChrome();
    }

    int ReleaseEligibleBackgroundTabs(bool automatic)
    {
        int released = 0;
        foreach (var tab in items.Values.OrderBy(t => t.HiddenSince).ToArray())
        {
            if (!CanSleep(tab) || tab.Busy || tab.Url is null) continue;
            UnloadTab(tab);
            if (automatic) tab.SleepResult = "Liberada automaticamente por pressão de memória";
            released++;
        }
        return released;
    }

    void CloseTabGroup(IReadOnlyList<TabPage> pages)
    {
        var targets=pages.Distinct().Where(items.ContainsKey).Select(p=>items[p]).ToArray();
        if(targets.Length==0)return;
        if(targets.Length==1){CloseTab(targets[0]);return;}
        ClosePages(targets);
    }

    void CloseTab(BrowserTab? target = null)
    {
        var tab = target ?? Current;
        if (tab is null) return;
        ClosePages(new[]{tab});
    }

    void ClosePages(IReadOnlyList<BrowserTab> targets)
    {
        if(closing)return;
        var order=tabs.TabPages.Cast<TabPage>().Where(items.ContainsKey).ToList();
        var next=TabCloseSelection.Next(order,targets.Select(t=>t.Page),tabs.SelectedTab);
        if(next is null){Close();return;}
        using(var redraw=new ThemeRedraw(this))
        {
            changingTabs=true;movingTabs=true;tabs.SuspendLayout();
            try
            {
                // Select the final survivor first. Removing other pages cannot activate intermediates.
                tabs.SelectedTab=next;
                foreach(var target in targets)
                {
                    if(!items.Remove(target.Page))continue;
                    tabs.TabPages.Remove(target.Page);DisposeView(target);ClearFavicon(target);target.Page.Dispose();
                }
            }
            finally{movingTabs=false;changingTabs=false;tabs.ResumeLayout(true);}
            SelectTab();ResumePriorityTabs();
        }
    }

    async Task SuspendTabs(bool now)
    {
        if (sweeping || closing || (!economy && !now && !deepEconomy)) return;
        ResumePriorityTabs();
        sweeping = true;
        try
        {
            foreach (var tab in items.Values.OrderBy(t => t.HiddenSince).ToArray())
            {
                if (closing) break;
                if (!items.ContainsKey(tab.Page) || tab.Busy) continue;
                string protection = Protection(tab);
                if (protection.Length > 0) { tab.SleepResult = protection; continue; }
                var inactive = DateTime.UtcNow - tab.HiddenSince;
                // Liberação forte é opt-in; as proteções são verificadas novamente em cada ciclo.
                if (ResourcePolicy.SleepDue(sleepMinutes,inactive) && deepEconomy && CanSleep(tab)) { UnloadTab(tab); continue; }
                if ((!economy && !now) || (!now && !ResourcePolicy.SleepDue(sleepMinutes,inactive))) continue;
                var view = tab.View!;
                var core = view.CoreWebView2;
                if (core.IsSuspended) { tab.SleepResult = "Suspensa"; continue; }
                tab.Busy = true;
                try
                {
                    view.SetPageVisible(false);
                    // Permitir que o WinForms propague a mudança ao controlador nativo.
                    await Task.Yield();
                    if (closing || tab.View != view || !items.ContainsKey(tab.Page)) continue;
                    if (!CanSleep(tab)) { view.SetPageVisible(tab == Current); continue; }
                    bool accepted = await core.TrySuspendAsync();
                    if (closing || tab.View != view || !items.ContainsKey(tab.Page)) continue;
                    if (!CanSleep(tab) || (!economy && !now))
                    {
                        core.Resume(); view.SetPageVisible(tab == Current); tab.SleepResult = "Retomada por atividade";
                    }
                    else tab.SleepResult = accepted ? "Suspensa" : "Motor recusou suspensão; tente economia forte";
                }
                catch (Exception ex) when (ex is InvalidOperationException or System.Runtime.InteropServices.COMException)
                {
                    tab.SleepResult = $"Suspensão falhou (0x{ex.HResult:X8})";
                }
                finally { tab.Busy = false; }
            }
            if (!closing) UpdateChrome();
        }
        finally { sweeping = false; }
    }

    void MeasureMemory()
    {
        if (DateTime.UtcNow - measured < TimeSpan.FromSeconds(15)) return;
        measured = DateTime.UtcNow;
        validMemorySample = false;
        try
        {
            var ids = new HashSet<int> { Environment.ProcessId };
            if (environment is not null) foreach (var info in environment.GetProcessInfos()) ids.Add(info.ProcessId);
            long total = 0; bool partial = false;
            foreach (int id in ids)
            {
                try { using var process = Process.GetProcessById(id); total += process.WorkingSet64; }
                catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception) { partial = true; }
            }
            measuredBytes = total; validMemorySample = true;
            memory = $" • RAM* ~{total / 1048576:N0} MB" + (partial ? " (parcial)" : "");
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Runtime.InteropServices.COMException) { memory = " • RAM indisponível"; }
    }

    void ReleaseMemoryPressure()
    {
        if (closing || sweeping || !automaticMemory || !validMemorySample) return;
        memoryPressure = ResourcePolicy.MemoryPressure(memoryPressure, measuredBytes);
        if (!memoryPressure || DateTime.UtcNow - lastMemoryRelease < TimeSpan.FromSeconds(30)) return;
        // Same eligibility and operation as the manual release command.
        lastMemoryRelease = DateTime.UtcNow;
        int released = ReleaseEligibleBackgroundTabs(true)+(ReleaseHiddenWhatsApp()?1:0);
        validMemorySample = false;
        if (released == 0) return;
        UpdateChrome();
    }

    void UpdateChrome()
    {
        if (closing) return;
        var tab = Current;
        // O timer não deve sobrescrever o endereço enquanto o usuário digita.
        if (!address.Focused) address.Text = tab?.Url ?? "";
        address.PlaceholderText = "Busque o que quiser sem peso, ou digite a sua URL";
        bookmarkButton.Enabled = tab?.Url is { } bookmarkUrl && ResourcePolicy.IsHttp(bookmarkUrl);
        downloadsButton.Enabled = tab?.Ready == true;
        back.Enabled = tab?.Ready == true && tab.View!.CanGoBack;
        forward.Enabled = tab?.Ready == true && tab.View!.CanGoForward;
        int sleeping = items.Values.Count(t => t.Ready && t.View!.CoreWebView2.IsSuspended);
        int unloaded = items.Values.Count(t => t.Unloaded);
        foreach (var item in items.Values)
        {
            string prefix = "";
            item.Page.Text = prefix + (item.Title.Length > 24 ? item.Title[..24] + "…" : item.Title);
            item.Page.ToolTipText = item.Title + "\n" + (item.Unloaded ? "Liberada; recarrega ao voltar" : item.SleepResult);
        }
        MeasureMemory();
        status.Text = $"VOLT 0.6.5 • {items.Count} aba(s) • {sleeping} suspensa(s) • {unloaded} liberada(s) • {blocked} bloqueio(s)" + (deepEconomy ? " • Economia forte" : "") + memory;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if(addressSuggestions?.HandleKey(keyData)==true)return true;
        if (keyData == (Keys.Control | Keys.L)) { address.Focus(); address.SelectAll(); return true; }
        if (keyData == (Keys.Control | Keys.T)) { _ = AddTab(); return true; }
        if (keyData == (Keys.Control | Keys.W)) { CloseTab(); return true; }
        if (keyData == Keys.F5 && Current?.Ready == true) { Current.View!.Reload(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }
}
