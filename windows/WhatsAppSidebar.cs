using Microsoft.Web.WebView2.Core;

namespace Volt;

sealed partial class BrowserWindow
{
    readonly Panel whatsAppPanel=new(){Dock=DockStyle.None,Width=720,Visible=false,Tag="chrome"};
    readonly Panel whatsAppBody=new(){Dock=DockStyle.Fill,Tag="surface"};
    readonly Panel whatsAppSplitter=new(){Dock=DockStyle.None,Width=5,Visible=false,Cursor=Cursors.SizeWE,Tag="chrome"};
    readonly CheckBox whatsAppKeepActive=new(){Text="Manter ativo",Checked=true,Dock=DockStyle.Right,Width=125,AccessibleName="Manter WhatsApp ativo em segundo plano"};
    readonly Label whatsAppStatus=new(){Dock=DockStyle.Fill,Text="Abra o WhatsApp para conectar sua conta.",TextAlign=ContentAlignment.MiddleCenter,Tag="surface"};
    Point? whatsAppDragStart;
    int whatsAppDragWidth;
    OutsidePanelClick? outsideWhatsApp;
    Panel? browserContent;
    BrowserView? whatsAppView;
    bool whatsAppBusy;
    int whatsAppDownloads;
    DateTime whatsAppHiddenSince;
    PictureBox? sidebarLogo;

    void InitializeSidebar(Panel content)
    {
        browserContent=content;
        var sidebar=new TableLayoutPanel {Dock=DockStyle.Left,Width=48,ColumnCount=1,RowCount=6,Padding=new(4),Tag="chrome"};
        sidebar.ColumnStyles.Add(new(SizeType.Percent,100));
        foreach(int height in new[]{44,44,44})sidebar.RowStyles.Add(new(SizeType.Absolute,height));
        sidebar.RowStyles.Add(new(SizeType.Percent,50));sidebar.RowStyles.Add(new(SizeType.Absolute,46));sidebar.RowStyles.Add(new(SizeType.Percent,50));
        sidebarLogo=new PictureBox {Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,Image=Icon?.ToBitmap(),Margin=new(8),Tag="chrome",AccessibleName="VOLT"};
        var home=MakeButton("Início");var favorites=MakeButton("Favoritar");
        favorites.AccessibleName="Abrir favoritos";
        home.Click+=(_,_)=>{if(Current is {} tab && (!tab.Ready || MessageBox.Show(this,"Voltar ao início e fechar a página atual? Salve seu trabalho antes.","VOLT",MessageBoxButtons.YesNo)==DialogResult.Yes))ShowHome(tab);};
        favorites.Click+=(_,_)=>ShowSidebarFavorites(favorites);
        var whatsapp=new ChromeButton {Kind="whatsapp",Dock=DockStyle.Fill,Margin=new(1),BackColor=Color.FromArgb(239,244,248),ForeColor=Color.White,Tag="chrome",AccessibleName="Abrir ou ocultar WhatsApp"};
        outsideWhatsApp=new OutsidePanelClick(this,point=>
            whatsAppPanel.Visible && RectangleToScreen(ClientRectangle).Contains(point)
            && !whatsAppPanel.RectangleToScreen(whatsAppPanel.ClientRectangle).Contains(point)
            && !whatsAppSplitter.RectangleToScreen(whatsAppSplitter.ClientRectangle).Contains(point)
            && !whatsapp.RectangleToScreen(whatsapp.ClientRectangle).Contains(point),HideWhatsApp);
        whatsapp.Click+=async(_,_)=>await ToggleWhatsApp();
        chromeTips.SetToolTip(whatsapp,"WhatsApp");chromeTips.SetToolTip(home,"Início");chromeTips.SetToolTip(favorites,"Favoritos");
        sidebar.Controls.Add(sidebarLogo,0,0);sidebar.Controls.Add(home,0,1);sidebar.Controls.Add(favorites,0,2);sidebar.Controls.Add(whatsapp,0,4);
        whatsAppKeepActive.CheckedChanged+=(_,_)=>
        {
            try{if(whatsAppKeepActive.Checked && whatsAppView?.CoreWebView2 is {} core && core.IsSuspended)core.Resume();}
            catch(Exception ex) when(ex is InvalidOperationException or System.Runtime.InteropServices.COMException){}
        };
        var header=new Panel {Dock=DockStyle.Top,Height=40,Padding=new(8,2,2,2),Tag="chrome"};
        var title=new Label {Text="WhatsApp",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Tag="chrome"};
        var close=MakeButton("Fechar janela");close.Dock=DockStyle.Right;close.Width=34;close.AccessibleName="Ocultar painel do WhatsApp";
        close.Click+=(_,_)=>HideWhatsApp();chromeTips.SetToolTip(close,close.AccessibleName);
        var reload=MakeButton("↻");reload.Dock=DockStyle.Right;reload.Width=34;reload.AccessibleName="Recarregar WhatsApp";
        reload.Click+=async(_,_)=>{if(whatsAppView?.CoreWebView2 is {} core)core.Reload();else await EnsureWhatsApp();};chromeTips.SetToolTip(reload,"Recarregar WhatsApp");
        header.Controls.Add(title);header.Controls.Add(whatsAppKeepActive);header.Controls.Add(reload);header.Controls.Add(close);
        whatsAppBody.Controls.Add(whatsAppStatus);whatsAppPanel.Controls.Add(whatsAppBody);whatsAppPanel.Controls.Add(header);
        content.Controls.Add(whatsAppSplitter);content.Controls.Add(whatsAppPanel);Controls.Add(sidebar);
        whatsAppSplitter.MouseDown+=(_,e)=>
        {
            if(e.Button!=MouseButtons.Left)return;
            whatsAppDragStart=Cursor.Position;whatsAppDragWidth=whatsAppPanel.Width;whatsAppSplitter.Capture=true;
        };
        whatsAppSplitter.MouseMove+=(_,_)=>
        {
            if(whatsAppDragStart is not {} start)return;
            whatsAppPanel.Width=SidebarLayout.Width(content.ClientSize.Width,whatsAppDragWidth+Cursor.Position.X-start.X);
            LayoutWhatsAppOverlay();
        };
        whatsAppSplitter.MouseUp+=(_,_)=>{whatsAppDragStart=null;whatsAppSplitter.Capture=false;};
        whatsAppSplitter.MouseCaptureChanged+=(_,_)=>{if(!whatsAppSplitter.Capture)whatsAppDragStart=null;};
        content.Resize+=(_,_)=>{if(whatsAppPanel.Visible)LayoutWhatsAppOverlay();};
    }
    void ShowSidebarFavorites(Control source)
    {
        var menu=new ContextMenuStrip {BackColor=darkMode?BrowserTheme.Surface:SystemColors.Control,ForeColor=darkMode?BrowserTheme.Text:SystemColors.ControlText};
        if(darkMode)menu.Renderer=new ToolStripProfessionalRenderer(new DarkMenuColors());
        foreach(var link in bookmarks.Where(b=>ResourcePolicy.IsHttp(b.Url)))
            menu.Items.Add(link.Title,null,(_,_)=>{if(Current is {} tab)_=NavigateTab(tab,link.Url);});
        if(menu.Items.Count==0)menu.Items.Add("Nenhum favorito salvo").Enabled=false;
        menu.Closed+=(_,_)=>{if(!IsDisposed && IsHandleCreated)BeginInvoke(new Action(menu.Dispose));else menu.Dispose();};menu.Show(source,new Point(source.Width,0));
    }
    async Task ToggleWhatsApp()
    {
        if(whatsAppPanel.Visible){HideWhatsApp();return;}
        SetWhatsAppLayout(true);
        if(whatsAppView?.CoreWebView2 is {} core){core.Resume();whatsAppView.SetPageVisible(true,true);}
        else await EnsureWhatsApp();
    }
    void HideWhatsApp()
    {
        SetWhatsAppLayout(false);
        whatsAppHiddenSince=DateTime.UtcNow;
    }
    void SetWhatsAppLayout(bool visible)
    {
        if(browserContent is null || closing)return;
        // Exclude controls that will be hidden: WM_SETREDRAW(TRUE) may restore WS_VISIBLE.
        // Keep the transition synchronous; never hold a screen cover across WebView initialization.
        using var redraw=new ThemeRedraw(this,c=>c==whatsAppPanel || c==whatsAppSplitter);
        browserContent.SuspendLayout();
        try
        {
            if(!visible)whatsAppView?.SetPageVisible(false,true);
            if(visible)
            {
                int preferred=whatsAppView is null?(int)(760*DeviceDpi/96f):whatsAppPanel.Width;
                whatsAppPanel.Width=SidebarLayout.Width(browserContent.ClientSize.Width,preferred);
                LayoutWhatsAppOverlay();
                whatsAppPanel.BringToFront();whatsAppSplitter.BringToFront();
            }
            whatsAppPanel.Visible=visible;
            whatsAppSplitter.Visible=visible;
        }
        finally { browserContent.ResumeLayout(true); }
        RefreshBrowserContent();
        if(visible)outsideWhatsApp?.Start();else outsideWhatsApp?.Stop();
    }
    void LayoutWhatsAppOverlay()
    {
        if(browserContent is null)return;
        int width=SidebarLayout.Width(browserContent.ClientSize.Width,whatsAppPanel.Width);
        whatsAppPanel.SetBounds(0,0,width,browserContent.ClientSize.Height);
        whatsAppSplitter.SetBounds(width,0,5,browserContent.ClientSize.Height);
    }
    void RefreshBrowserContent()
    {
        if(browserContent is null || closing)return;
        browserContent.PerformLayout();
        tabs.PerformLayout();
        if(Current is {} tab)
        {
            tab.Page.PerformLayout();
            foreach(Control child in tab.Page.Controls)child.PerformLayout();
        }
        // Home uses a viewport-relative gradient and polygon: repaint the complete final bounds.
        browserContent.Invalidate(true);
        browserContent.Update();
    }
    async Task EnsureWhatsApp()
    {
        if(closing || whatsAppBusy || whatsAppView is not null)return;
        whatsAppBusy=true;whatsAppStatus.Text="Carregando WhatsApp…";whatsAppStatus.Visible=true;
        var view=new BrowserView {Dock=DockStyle.Fill,Visible=false,BackColor=darkMode?BrowserTheme.Background:Color.White,DefaultBackgroundColor=darkMode?BrowserTheme.Background:Color.White};
        whatsAppView=view;whatsAppBody.Controls.Add(view);view.BringToFront();
        try
        {
            string profile=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"VOLT","Profile");
            environmentTask??=CoreWebView2Environment.CreateAsync(null,profile);
            var env=await environmentTask;
            if(closing || whatsAppView!=view)return;
            environment=env;await view.EnsureCoreWebView2Async(env);
            if(closing || whatsAppView!=view)return;
            SetWebTheme(view);view.SetPageVisible(whatsAppPanel.Visible,true);
            var core=view.CoreWebView2;core.Settings.AreDevToolsEnabled=false;
            core.NewWindowRequested+=(_,e)=>{e.Handled=true;if(e.IsUserInitiated && ResourcePolicy.IsHttp(e.Uri))_ = AddTab(e.Uri);};
            core.NavigationStarting+=(_,e)=>
            {
                if(e.IsUserInitiated && Uri.TryCreate(e.Uri,UriKind.Absolute,out var uri) && ResourcePolicy.IsHttp(e.Uri) && uri.Host!="web.whatsapp.com")
                {e.Cancel=true;_ = AddTab(e.Uri);}
            };
            core.DownloadStarting+=(_,e)=>
            {
                whatsAppDownloads++;bool finished=false;
                e.DownloadOperation.StateChanged+=(_,_)=>
                {
                    if(finished || e.DownloadOperation.State==CoreWebView2DownloadState.InProgress)return;
                    finished=true;if(whatsAppView==view)whatsAppDownloads=Math.Max(0,whatsAppDownloads-1);
                };
            };
            core.Navigate("https://web.whatsapp.com/");whatsAppStatus.Visible=false;
        }
        catch(Exception ex) when(ex is InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            if(!closing && whatsAppView==view)
            {DisposeWhatsApp();whatsAppStatus.Text="Não foi possível abrir o WhatsApp. Use o botão de recarregar para tentar novamente.";}
        }
        finally{whatsAppBusy=false;}
    }
    bool CanReleaseWhatsApp()
    {
        if(whatsAppPanel.Visible || whatsAppKeepActive.Checked || whatsAppBusy || whatsAppDownloads>0 || whatsAppView?.CoreWebView2 is not {} core)return false;
        try{return !core.IsDocumentPlayingAudio;}catch(Exception ex) when(ex is InvalidOperationException or System.Runtime.InteropServices.COMException){return false;}
    }
    bool ReleaseHiddenWhatsApp()
    {
        if(!CanReleaseWhatsApp())return false;
        DisposeWhatsApp();return true;
    }
    async Task SweepWhatsApp()
    {
        if(closing || !economy || DateTime.UtcNow-whatsAppHiddenSince<TimeSpan.FromSeconds(45) || !CanReleaseWhatsApp())return;
        var view=whatsAppView!;
        whatsAppBusy=true;
        try
        {
            var core=view.CoreWebView2;if(core.IsSuspended)return;
            view.SetPageVisible(false,true);await core.TrySuspendAsync();
            if(whatsAppView==view && (whatsAppPanel.Visible || whatsAppKeepActive.Checked))core.Resume();
        }
        catch(Exception ex) when(ex is InvalidOperationException or System.Runtime.InteropServices.COMException){}
        finally{whatsAppBusy=false;}
    }
    void DisposeWhatsApp()
    {
        var view=whatsAppView;whatsAppView=null;whatsAppDownloads=0;
        if(view is not null){whatsAppBody.Controls.Remove(view);view.Dispose();}
        whatsAppStatus.Visible=true;
    }
}
