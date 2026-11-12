#!/usr/bin/env python3
"""VOLT Linux 0.6.0 — native Qt port based on Windows release 0.6.0."""
import sys, os, json, time
from pathlib import Path
from urllib.parse import urlparse
from PySide6.QtCore import Qt,QUrl,QTimer,QPoint,Signal,QStandardPaths,QSize,QByteArray,QEvent
from PySide6.QtGui import QColor,QPainter,QLinearGradient,QPolygonF,QIcon,QFont,QAction,QShortcut,QKeySequence,QPalette,QPixmap
from PySide6.QtWidgets import (QApplication,QMainWindow,QWidget,QVBoxLayout,QHBoxLayout,QLineEdit,QPushButton,QLabel,
 QStackedWidget,QSplitter,QMenu,QCheckBox,QFileDialog,QMessageBox,QSizePolicy)
from PySide6.QtWebEngineCore import QWebEnginePage,QWebEngineProfile,QWebEngineScript,QWebEngineUrlRequestInterceptor,QWebEngineSettings
from PySide6.QtWebEngineWidgets import QWebEngineView
from PySide6.QtSvg import QSvgRenderer
from policy import address,is_tracker,move_block,survivor,eligible,pressure,process_memory
BASE=Path(getattr(sys,'_MEIPASS',Path(__file__).resolve().parent))
ASSETS=BASE/'assets'
VERSION='0.6.3 Linux'
ACTIVE=QWebEnginePage.LifecycleState.Active
FROZEN=QWebEnginePage.LifecycleState.Frozen
DISCARDED=QWebEnginePage.LifecycleState.Discarded

ICON_PATHS={
 '‹':'<path d="m14 5-7 7 7 7"/>', '›':'<path d="m9 5 7 7-7 7"/>',
 '⟳':'<path d="M19 8a8 8 0 1 0 1 7M19 3v5h-5"/>',
 '⌂':'<path d="m3 11 9-8 9 8M6 9v12h12V9M10 21v-7h4v7"/>',
 '♡':'<path d="M12 21 3 12C-2 4 8 0 12 7c4-7 14-3 9 5Z"/>',
 '↓':'<path d="M12 3v13m-5-5 5 5 5-5M4 17v4h16v-4"/>',
 '◐':'<path d="M15 3a9 9 0 1 0 6 14A9 9 0 0 1 15 3Z"/>',
 '☰':'<path d="M3 6h18M3 12h18M3 18h18"/>',
 '⌕':'<circle cx="10" cy="10" r="6"/><path d="m15 15 6 6"/>',
 '⌾':'<path d="M8 3H3v5m13-5h5v5M3 16v5h5m13-5v5h-5"/><circle cx="12" cy="12" r="4"/>',
 '×':'<path d="m6 6 12 12M6 18 18 6"/>',
 '◉':'<path d="M4 18 2 22l5-2a10 10 0 1 0-3-2Z"/><path d="M8 6c-4 4 6 14 10 10l-3-3-2 1-3-3 1-2Z"/>'
}
def button(text,tip,callback,parent=None):
    b=QPushButton(parent); b.setToolTip(tip);b.setAccessibleName(tip);b.setCursor(Qt.PointingHandCursor)
    b.setProperty('voltSymbol',text);b.setFixedSize(32,32);b.clicked.connect(callback)
    refresh_icon(b,True)
    return b
def refresh_icon(b,dark):
    symbol=b.property('voltSymbol')
    if symbol=='◉':
        # Qt SVG does not resolve percentage corner radii consistently.
        data=(ASSETS/'WhatsApp.svg').read_bytes().replace(b'rx="15%"',b'rx="76.8"')
        renderer=QSvgRenderer(QByteArray(data))
        pix=QPixmap(64,64);pix.fill(Qt.transparent);p=QPainter(pix);renderer.render(p);p.end()
        b.setIcon(QIcon(pix));b.setIconSize(QSize(28,28));return
    if symbol not in ICON_PATHS:b.setText(symbol or '');return
    color='#25d366' if symbol=='◉' else '#c6d3e0' if dark else '#526477'
    svg=f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><g fill="none" stroke="{color}" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">{ICON_PATHS[symbol]}</g></svg>'
    pix=QPixmap(40,40);pix.fill(Qt.transparent);p=QPainter(pix);QSvgRenderer(QByteArray(svg.encode())).render(p);p.end();b.setIcon(QIcon(pix));b.setIconSize(QSize(18,18))

class Filter(QWebEngineUrlRequestInterceptor):
    def interceptRequest(self,info):
        if is_tracker(info.requestUrl().toString()):info.block(True)

class Home(QWidget):
    def __init__(self,window):
        super().__init__();self.window=window
        outer=QVBoxLayout(self);outer.setContentsMargins(24,20,24,55);outer.addStretch()
        card=QWidget();self.card=card;card.setFixedWidth(520)
        layout=QVBoxLayout(card);layout.setContentsMargins(0,0,0,0);layout.setSpacing(12)
        title=QLabel('V O L T');title.setObjectName('brandTitle');title.setAlignment(Qt.AlignCenter);title.setFont(QFont('Sans Serif',34));layout.addWidget(title)
        sub=QLabel('Navegue sem peso.');sub.setAlignment(Qt.AlignCenter);layout.addWidget(sub);layout.addSpacing(8)
        shell=QWidget();shell.setObjectName('search');shell.setFixedHeight(54);row=QHBoxLayout(shell);row.setContentsMargins(10,0,10,0)
        row.addWidget(button('⌕','Pesquisar',self.submit));self.search=QLineEdit();self.search.setObjectName('homeInput');self.search.setPlaceholderText('Pesquise na web...')
        self.search.returnPressed.connect(self.submit);row.addWidget(self.search)
        row.addWidget(button('⌾','Pesquisar imagem no Google Lens',lambda:window.navigate('https://lens.google/')));layout.addWidget(shell)
        links=QHBoxLayout();links.setSpacing(8);links.setContentsMargins(45,0,45,0)
        for name,url in [('YouTube','https://www.youtube.com'),('Google','https://www.google.com'),('ChatGPT','https://chatgpt.com'),('Outlook','https://outlook.live.com')]:
            b=QPushButton(name);b.setObjectName('pill');b.setFixedHeight(36);b.setCursor(Qt.PointingHandCursor);b.clicked.connect(lambda _,u=url:window.navigate(u));links.addWidget(b)
        layout.addLayout(links);outer.addWidget(card,0,Qt.AlignHCenter);outer.addStretch()
        for b in self.findChildren(QPushButton):
            if b.property('voltSymbol'):refresh_icon(b,window.dark)
    def resizeEvent(self,event):
        self.card.setFixedWidth(min(520,max(280,self.width()-48)))
        super().resizeEvent(event)
    def submit(self):
        if self.search.text().strip():self.window.navigate(self.search.text())
    def paintEvent(self,event):
        p=QPainter(self);g=QLinearGradient(0,0,self.width(),self.height());dark=self.window.dark
        g.setColorAt(0,QColor('#1b2533' if dark else '#dfe9f3'));g.setColorAt(1,QColor('#24212d' if dark else '#ecd7d0'));p.fillRect(self.rect(),g)
        w,h=self.width(),self.height();p.setPen(Qt.NoPen);p.setBrush(QColor('#2d3447' if dark else '#c6cbd0'))
        from PySide6.QtCore import QPointF
        p.drawPolygon(QPolygonF([QPointF(0,h),QPointF(0,h*.75),QPointF(w*.2,h*.60),QPointF(w*.5,h*.8),QPointF(w*.8,h*.67),QPointF(w,h*.75),QPointF(w,h)]))

class Tabs(QWidget):
    """Stable IDs and a single paint surface: reordering never reparents page views."""
    def __init__(self,window):
        super().__init__();self.w=window;self.selected=set();self.anchor=None;self.hover=-1;self.start=None;self.dragging=False;self.slot=None;self.offset=0
        self.setFixedHeight(36);self.setMouseTracking(True);self.setMinimumWidth(100)
    def width_tab(self):return max(120,min(200,(self.width()-40)//max(1,len(self.w.order))))
    def rect_tab(self,i):
        from PySide6.QtCore import QRect
        return QRect(i*self.width_tab()-self.offset,4,self.width_tab()-4,28)
    def hit_rect_tab(self,i):
        from PySide6.QtCore import QRect
        visual=self.rect_tab(i)
        return QRect(visual.left(),0,self.width_tab(),self.height())
    def hit(self,p):
        if not self.rect().contains(p):return -1
        for i in range(len(self.w.order)):
            if self.hit_rect_tab(i).contains(p):return i
        return -1
    def plus_rect(self):
        from PySide6.QtCore import QRect
        return QRect(min(len(self.w.order)*self.width_tab()-self.offset,self.width()-32),6,24,24)
    def paintEvent(self,event):
        p=QPainter(self);p.setRenderHint(QPainter.Antialiasing);p.fillRect(self.rect(),QColor(self.w.chrome))
        for i,ident in enumerate(self.w.order):
            r=self.rect_tab(i)
            if r.right()<0 or r.left()>self.width():continue
            tab=self.w.tabs[ident];chosen=ident in self.selected;current=ident==self.w.active
            p.setPen(QColor('#8eb7df') if chosen and len(self.selected)>1 else Qt.NoPen)
            p.setBrush(QColor('#425c7a' if chosen and len(self.selected)>1 else ('#344354' if self.w.dark else '#ffffff') if current else self.w.chrome));p.drawRoundedRect(r,7,7)
            p.setPen(QColor(self.w.fg));p.setFont(QFont('Sans Serif',9))
            title=('♫ ' if tab.view and tab.view.page().recentlyAudible() else '')+tab.title
            p.drawText(r.adjusted(12,0,-26,0),Qt.AlignVCenter,p.fontMetrics().elidedText(title,Qt.ElideRight,max(10,r.width()-40)))
            if self.hover==i:
                x=r.right()-17;p.setPen(QColor(self.w.fg));p.drawLine(x-3,15,x+3,21);p.drawLine(x-3,21,x+3,15)
        p.setBrush(QColor('#344354' if self.w.dark else '#dce5ed'));p.setPen(Qt.NoPen);p.drawEllipse(self.plus_rect());p.setPen(QColor(self.w.fg));p.drawText(self.plus_rect(),Qt.AlignCenter,'+')
        if self.dragging and self.slot is not None:
            x=min(self.width()-3,max(2,self.slot*self.width_tab()-self.offset));p.setPen(QColor('#95c9ff'));p.drawLine(x,3,x,33)
    def mousePressEvent(self,e):
        if e.button()!=Qt.LeftButton:return
        pos=e.position().toPoint()
        if self.plus_rect().contains(pos):self.w.new_tab();return
        i=self.hit(pos)
        if i<0:return
        ident=self.w.order[i];r=self.rect_tab(i)
        if r.contains(pos) and pos.x()>r.right()-26:self.w.close_tabs({ident});return
        shift=bool(e.modifiers()&Qt.ShiftModifier);ctrl=bool(e.modifiers()&Qt.ControlModifier)
        if shift and self.anchor in self.w.order:
            a=self.w.order.index(self.anchor);self.selected=set(self.w.order[min(a,i):max(a,i)+1])
        elif ctrl:
            if ident in self.selected:self.selected.remove(ident)
            else:self.selected.add(ident)
            self.anchor=ident
        elif ident not in self.selected:self.selected={ident};self.anchor=ident
        self.start=pos;self.pressed=ident;self.modifiers=e.modifiers();self.w.activate(ident);self.update()
    def mouseMoveEvent(self,e):
        p=e.position().toPoint();self.hover=self.hit(p)
        if self.start is not None and e.buttons()&Qt.LeftButton:
            if (p-self.start).manhattanLength()>QApplication.startDragDistance():self.dragging=True
            if self.dragging:
                if p.x()<25:self.offset=max(0,self.offset-12)
                if p.x()>self.width()-30:self.offset=min(self.max_offset(),self.offset+12)
                self.slot=max(0,min(len(self.w.order),(p.x()+self.offset+self.width_tab()//2)//self.width_tab()))
        self.update()
    def mouseReleaseEvent(self,e):
        if e.button()!=Qt.LeftButton:return
        if self.dragging and self.rect().contains(e.position().toPoint()) and self.slot is not None:
            self.w.order=move_block(self.w.order,self.selected,self.slot);self.selected={self.w.active};self.anchor=self.w.active
        elif self.start is not None and not self.modifiers&(Qt.ShiftModifier|Qt.ControlModifier):self.selected={self.w.active};self.anchor=self.w.active
        self.start=None;self.dragging=False;self.slot=None;self.update()
    def keyPressEvent(self,e):
        if e.key()==Qt.Key_Escape:self.start=None;self.dragging=False;self.slot=None;self.update()
        else:super().keyPressEvent(e)
    def leaveEvent(self,e):self.hover=-1;self.update()
    def max_offset(self):return max(0,len(self.w.order)*self.width_tab()+36-self.width())
    def wheelEvent(self,e):self.offset=max(0,min(self.max_offset(),self.offset-e.angleDelta().y()));self.update()
    def reveal(self):
        if self.w.active not in self.w.order:return
        r=self.rect_tab(self.w.order.index(self.w.active))
        if r.left()<0:self.offset=max(0,self.offset+r.left())
        elif r.right()>self.width()-35:self.offset+=r.right()-self.width()+35
        self.offset=min(self.offset,self.max_offset());self.update()
    def contextMenuEvent(self,e):
        i=self.hit(e.pos())
        if i<0:return
        ident=self.w.order[i]
        if ident not in self.selected:self.selected={ident};self.anchor=ident;self.w.activate(ident)
        self.update();menu=QMenu(self)
        menu.addAction('Nova aba',self.w.new_tab)
        menu.addAction('Recarregar abas selecionadas',lambda:[self.w.tabs[x].reload() for x in self.selected if x in self.w.tabs])
        keep=menu.addAction('Manter abas selecionadas ativas');keep.setCheckable(True);keep.setChecked(all(self.w.tabs[x].pinned for x in self.selected));keep.triggered.connect(lambda checked:[setattr(self.w.tabs[x],'pinned',checked) for x in self.selected])
        menu.addSeparator();menu.addAction(f'Fechar {len(self.selected)} aba(s) selecionada(s)',lambda:self.w.close_tabs(set(self.selected)))
        menu.addAction('Fechar outras abas',lambda:self.w.close_tabs(set(self.w.order)-self.selected));menu.exec(e.globalPos())

class Page(QWebEnginePage):
    def __init__(self,profile,tab):super().__init__(profile,tab);self.tab=tab
    def acceptNavigationRequest(self,url,kind,main):
        if url.scheme() not in ('http','https','about','data','blob'):return False
        if self.tab.whatsapp and main and kind==QWebEnginePage.NavigationType.NavigationTypeLinkClicked and url.host()!='web.whatsapp.com':
            self.tab.w.new_tab(url.toString());return False
        return super().acceptNavigationRequest(url,kind,main)

class Tab(QWidget):
    def __init__(self,window,ident,whatsapp=False):
        super().__init__();self.w=window;self.ident=ident;self.whatsapp=whatsapp;self.title='Nova guia';self.url='';self.view=None;self.home=None;self.loading=False;self.ready=False;self.pinned=whatsapp;self.downloads=0;self.hidden=time.monotonic();self.revision=0
        self.box=QVBoxLayout(self);self.box.setContentsMargins(0,0,0,0);self.box.setSpacing(0)
        if not whatsapp:self.show_home()
    def show_home(self):
        if self.view:self.box.removeWidget(self.view);self.view.stop();self.view.page().deleteLater();self.view.deleteLater();self.view=None
        if not self.home:self.home=Home(self.w);self.box.addWidget(self.home)
        self.title='Nova guia';self.url='';self.ready=False;self.w.refresh()
    def ensure_view(self):
        if not self.view:
            view=QWebEngineView(self);page=Page(self.w.profile,self);view.setPage(page);self.view=view
            page.setBackgroundColor(QColor(self.w.bg));page.settings().setAttribute(QWebEngineSettings.FullScreenSupportEnabled,True)
            page.settings().setAttribute(QWebEngineSettings.PlaybackRequiresUserGesture,True)
            page.settings().setAttribute(QWebEngineSettings.JavascriptCanOpenWindows,True)
            view.urlChanged.connect(self.url_changed);view.titleChanged.connect(self.title_changed);view.loadStarted.connect(self.started);view.loadFinished.connect(self.finished)
            page.newWindowRequested.connect(self.new_window);page.permissionRequested.connect(self.permission)
            page.fullScreenRequested.connect(self.fullscreen);page.renderProcessTerminated.connect(self.crashed)
            page.recentlyAudibleChanged.connect(lambda _:self.w.header.update())
            if not self.whatsapp:
                script=QWebEngineScript();script.setName('VOLT media');script.setInjectionPoint(QWebEngineScript.DocumentCreation);script.setWorldId(QWebEngineScript.MainWorld);script.setRunsOnSubFrames(True)
                script.setSourceCode('('+self.w.media_source+')(false,false,0);');page.scripts().insert(script)
            if self.home:self.box.removeWidget(self.home);self.home.deleteLater();self.home=None
            self.box.addWidget(view);self.w.install_click_filters()
    def navigate(self,text):
        target=address(text);self.ensure_view()
        self.view.page().setLifecycleState(ACTIVE);self.view.setUrl(QUrl(target));self.url=target;self.w.refresh()
    def new_window(self,request):
        if request.isUserInitiated():
            tab=self.w.new_tab();tab.ensure_view();request.openIn(tab.view.page())
    def permission(self,permission):
        origin=permission.origin().toDisplayString();kind=permission.permissionType().name
        reply=QMessageBox.question(self.w,'Permissão do site',f'{origin}\nSolicita: {kind}.\nPermitir?')
        if reply==QMessageBox.Yes:permission.grant()
        else:permission.deny()
    def fullscreen(self,request):
        request.accept()
        if request.toggleOn():self.w.showFullScreen()
        else:self.w.showNormal()
    def crashed(self,*_):
        self.loading=False;self.ready=False;self.title='Página interrompida — recarregue';self.w.refresh()
    def url_changed(self,url):self.url=url.toString();self.w.refresh()
    def title_changed(self,title):self.title=title or 'Nova guia';self.w.refresh()
    def started(self):self.loading=True;self.w.refresh()
    def finished(self,ok):self.loading=False;self.ready=True;self.media();self.w.refresh()
    def media(self):
        if self.view and not self.whatsapp:
            self.revision+=1
            hidden=self.w.active!=self.ident
            self.view.page().runJavaScript('('+self.w.media_source+')('+json.dumps(self.w.settings.get('pause_media',False))+','+json.dumps(hidden)+','+str(self.revision)+');')
    def reload(self):
        if self.view:self.view.page().setLifecycleState(ACTIVE);self.view.reload()
    def can_sleep(self):
        visible=self.w.wa_visible if self.whatsapp else self.w.active==self.ident
        page=self.view.page() if self.view else None
        return eligible(visible,bool(page and self.ready),self.loading,self.pinned,self.downloads,bool(page and page.recentlyAudible()))
    def activate(self):
        if self.view:self.view.page().setLifecycleState(ACTIVE)
        self.media()

class OverlayGrip(QWidget):
    def __init__(self,window,parent):
        super().__init__(parent);self.w=window;self.start=None;self.setCursor(Qt.SizeHorCursor);self.setObjectName('overlayGrip')
    def mousePressEvent(self,event):
        if event.button()==Qt.LeftButton:
            self.start=(event.globalPosition().x(),self.w.panel_width);self.grabMouse()
    def mouseMoveEvent(self,event):
        if self.start:
            self.w.panel_width=int(self.start[1]+event.globalPosition().x()-self.start[0]);self.w.layout_whatsapp()
    def mouseReleaseEvent(self,event):
        if event.button()==Qt.LeftButton:self.start=None;self.releaseMouse()

class Browser(QMainWindow):
    def __init__(self):
        super().__init__();self.setWindowTitle('VOLT — Navegue sem peso.');self.setWindowIcon(QIcon(str(ASSETS/'Volt.png')));self.resize(1240,820);self.setMinimumSize(760,500)
        self.state_dir=Path(QStandardPaths.writableLocation(QStandardPaths.AppDataLocation));self.state_dir.mkdir(parents=True,exist_ok=True)
        try:self.settings=json.loads((self.state_dir/'settings.json').read_text())
        except (OSError,ValueError):self.settings={}
        self.dark=self.settings.get('dark',True);self.tabs={};self.order=[];self.active=None;self.next_id=0;self.wa=None;self.wa_visible=False;self.download_items=[];self.pressured=False;self.last_clean=0;self.closing=False;self.filter_scan_pending=False
        self.media_source=(ASSETS/'MediaGuard.js').read_text()
        self.profile=QWebEngineProfile('VOLT',self);self.profile.setPersistentStoragePath(str(self.state_dir/'Profile'));self.profile.setCachePath(QStandardPaths.writableLocation(QStandardPaths.CacheLocation));self.profile.setPersistentCookiesPolicy(QWebEngineProfile.AllowPersistentCookies);self.profile.setHttpCacheMaximumSize(128*1024*1024)
        self.filter=Filter(self.profile);self.profile.setUrlRequestInterceptor(self.filter);self.profile.downloadRequested.connect(self.download)
        root=QWidget();root.setObjectName('root');self.setCentralWidget(root);outer=QHBoxLayout(root);outer.setContentsMargins(0,0,0,0);outer.setSpacing(0)
        self.sidebar=QWidget();self.sidebar.setObjectName('sidebar');self.sidebar.setFixedWidth(48);side=QVBoxLayout(self.sidebar);side.setContentsMargins(6,8,6,8)
        logo=QLabel();logo.setPixmap(QIcon(str(ASSETS/'Volt.png')).pixmap(28,28));logo.setAlignment(Qt.AlignCenter);side.addWidget(logo)
        side.addWidget(button('⌂','Início',self.go_home));side.addWidget(button('♡','Favoritos',self.bookmark_menu));side.addStretch();wa=button('◉','Abrir ou ocultar WhatsApp',self.toggle_whatsapp);wa.setObjectName('whatsapp');self.wa_button=wa;side.addWidget(wa);side.addStretch();outer.addWidget(self.sidebar)
        main=QWidget();col=QVBoxLayout(main);col.setContentsMargins(0,0,0,0);col.setSpacing(0);outer.addWidget(main,1)
        self.header=Tabs(self);col.addWidget(self.header)
        nav=QWidget();nav.setObjectName('nav');row=QHBoxLayout(nav);row.setContentsMargins(6,3,6,3);row.setSpacing(4)
        self.back=button('‹','Voltar',lambda:self.current_view_action('back'));row.addWidget(self.back)
        self.forward=button('›','Avançar',lambda:self.current_view_action('forward'));row.addWidget(self.forward)
        row.addWidget(button('⟳','Recarregar',lambda:self.current_view_action('reload')));row.addWidget(button('⌂','Início',self.go_home))
        self.address=QLineEdit();self.address.setObjectName('address');self.address.setMinimumHeight(32);self.address.setPlaceholderText('Busque o que quiser sem peso, ou digite a sua URL');self.address.returnPressed.connect(lambda:self.navigate(self.address.text()));row.addWidget(self.address,1)
        row.addWidget(button('♡','Salvar favorito',self.add_bookmark));row.addWidget(button('↓','Downloads',self.download_menu));row.addWidget(button('◐','Alternar modo claro e escuro',self.toggle_theme));row.addWidget(button('☰','Menu',self.menu));col.addWidget(nav)
        self.bookmarks=QWidget();self.bookmarks.setFixedHeight(27);self.bookmark_layout=QHBoxLayout(self.bookmarks);self.bookmark_layout.setContentsMargins(8,0,8,0);col.addWidget(self.bookmarks)
        self.viewport=QWidget();col.addWidget(self.viewport,1);self.panel_width=720
        viewport_layout=QVBoxLayout(self.viewport);viewport_layout.setContentsMargins(0,0,0,0);viewport_layout.setSpacing(0)
        self.stack=QStackedWidget();viewport_layout.addWidget(self.stack)
        self.wa_shell=QWidget(self.viewport);self.wa_shell.setObjectName('waPanel');self.wa_shell.setAttribute(Qt.WA_StyledBackground,True);wa_layout=QVBoxLayout(self.wa_shell);wa_layout.setContentsMargins(0,0,0,0);wa_layout.setSpacing(0)
        wa_head=QWidget();wa_head.setFixedHeight(40);wr=QHBoxLayout(wa_head);wr.setContentsMargins(8,3,6,3);wr.addWidget(QLabel('WhatsApp'));wr.addStretch();self.keep_wa=QCheckBox('Manter ativo');self.keep_wa.setChecked(True);self.keep_wa.toggled.connect(self.pin_whatsapp);wr.addWidget(self.keep_wa);wr.addWidget(button('⟳','Recarregar WhatsApp',lambda:self.wa.reload() if self.wa else None));wr.addWidget(button('×','Ocultar WhatsApp',self.toggle_whatsapp));wa_layout.addWidget(wa_head);self.wa_box=wa_layout
        self.wa_shell.hide();self.wa_grip=OverlayGrip(self,self.viewport);self.wa_grip.hide()

        self.status=QLabel();self.status.setObjectName('status');self.status.setFixedHeight(21);col.addWidget(self.status)
        self.apply_theme();self.render_bookmarks();self.new_tab()
        for key,callback in [('Ctrl+T',self.new_tab),('Ctrl+W',lambda:self.close_tabs(self.header.selected or {self.active})),('Ctrl+L',self.focus_address),('Ctrl+R',lambda:self.current_view_action('reload')),('F5',lambda:self.current_view_action('reload')),('F11',lambda:self.showNormal() if self.isFullScreen() else self.showFullScreen()),('Ctrl+Tab',lambda:self.cycle(1)),('Ctrl+Shift+Tab',lambda:self.cycle(-1))]:
            QShortcut(QKeySequence(key),self,activated=callback)
        self.timer=QTimer(self);self.timer.timeout.connect(self.sweep);self.timer.start(15000);self.install_click_filters()
    def save(self):
        target=self.state_dir/'settings.json';temp=target.with_suffix('.tmp');temp.write_text(json.dumps(self.settings,ensure_ascii=False,indent=2));temp.replace(target)
    def apply_theme(self):
        self.bg='#202431' if self.dark else '#e8e3e3';self.chrome='#181e27' if self.dark else '#eef4f9';self.fg='#e5ecf4' if self.dark else '#253343';surface='#232b36' if self.dark else '#ffffff'
        self.setUpdatesEnabled(False)
        try:
            palette=QApplication.palette();palette.setColor(QPalette.Window,QColor(self.bg));palette.setColor(QPalette.WindowText,QColor(self.fg));palette.setColor(QPalette.Text,QColor(self.fg));palette.setColor(QPalette.Base,QColor(surface));palette.setColor(QPalette.Button,QColor(self.chrome));palette.setColor(QPalette.ButtonText,QColor(self.fg));QApplication.setPalette(palette)
            self.setStyleSheet(f'''
            QWidget {{ color:{self.fg};font-family:Sans Serif;font-size:12px; }}
            QMainWindow,#root {{ background:{self.bg}; }}
            #sidebar,#nav,#status,#waPanel,#overlayGrip {{ background:{self.chrome}; }}
            QPushButton {{ border:0;background:transparent;border-radius:8px;padding:3px; }}
            QPushButton:hover {{ background:{'#344354' if self.dark else '#dce5ef'}; }}
            #address {{ background:{surface};border:0;border-radius:16px;padding:0 14px;selection-background-color:#49749d; }}
            #search {{ background:{'#181f29' if self.dark else '#ffffff'};border-radius:27px; }}
            #homeInput {{ background:transparent;border:0;font-size:14px; }}
            #brandTitle {{ font-size:46px;font-weight:300; }}
            #pill {{ border-radius:18px;background:qlineargradient(x1:0,y1:0,x2:0,y2:1,stop:0 {'#181e27' if self.dark else '#ffffff'},stop:1 {'#252e3d' if self.dark else '#dce9f4'}); }}
            #pill:hover {{ background:{'#344354' if self.dark else '#cfdfec'}; }}
            #whatsapp {{ color:#25d366;font-size:25px; }}
            QMenu {{ background:{surface};border:1px solid {'#344354' if self.dark else '#c4d0db'};padding:6px; }}
            QMenu::item {{ padding:8px 18px; }} QMenu::item:selected {{ background:#425c7a;color:white; }}
            QSplitter::handle {{ background:{self.chrome};width:4px; }}
            #status {{ font-size:10px;padding-left:8px; }}
            ''')
            for b in self.findChildren(QPushButton):
                if b.property('voltSymbol'):refresh_icon(b,self.dark)
            QApplication.styleHints().setColorScheme(Qt.ColorScheme.Dark if self.dark else Qt.ColorScheme.Light)
            for tab in list(self.tabs.values())+([self.wa] if self.wa else []):
                if tab.home:tab.home.update()
                if tab.view:tab.view.page().setBackgroundColor(QColor(self.bg))
        finally:self.setUpdatesEnabled(True);self.update()
    def toggle_theme(self):self.dark=not self.dark;self.settings['dark']=self.dark;self.apply_theme();self.save()
    def new_tab(self,url=None):
        self.next_id+=1;ident=self.next_id;tab=Tab(self,ident);self.tabs[ident]=tab
        idx=self.order.index(self.active)+1 if self.active in self.order else len(self.order);self.order.insert(idx,ident);self.stack.addWidget(tab);self.header.selected={ident};self.header.anchor=ident;self.activate(ident)
        if isinstance(url,str) and url:tab.navigate(url)
        else:QTimer.singleShot(0,lambda:self.focus_address() if self.active==ident else None)
        self.install_click_filters()
        return tab
    def activate(self,ident):
        if ident not in self.tabs:return
        previous=self.tabs.get(self.active)
        self.active=ident;tab=self.tabs[ident];self.stack.setCurrentWidget(tab);self.address.setText(tab.url);tab.activate()
        if previous and previous is not tab:previous.hidden=time.monotonic();previous.media()
        self.header.reveal();self.refresh()
    def focus_address(self):self.address.setFocus();self.address.selectAll()
    def navigate(self,text):
        if text.strip() and self.active in self.tabs:self.tabs[self.active].navigate(text)
    def current_view_action(self,action):
        tab=self.tabs.get(self.active)
        if tab and tab.view:
            tab.view.page().setLifecycleState(ACTIVE);getattr(tab.view,action)()
    def cycle(self,step):
        if self.order:
            ident=self.order[(self.order.index(self.active)+step)%len(self.order)];self.header.selected={ident};self.header.anchor=ident;self.activate(ident)
    def go_home(self):
        tab=self.tabs.get(self.active)
        if not tab:return
        if tab.downloads:
            QMessageBox.information(self,'Download em andamento','Aguarde o download terminar antes de voltar ao início.');return
        if tab.view and QMessageBox.question(self,'Início','Voltar ao início? Dados não salvos nesta página podem ser perdidos.')!=QMessageBox.Yes:return
        tab.show_home();self.address.clear();self.focus_address()
    def close_tabs(self,ids):
        ids=set(ids)&set(self.order)
        if not ids:return
        target=survivor(self.order,ids,self.active)
        if target is None:self.close();return
        self.setUpdatesEnabled(False)
        try:
            self.activate(target)
            for ident in ids:
                tab=self.tabs.pop(ident);self.order.remove(ident);self.stack.removeWidget(tab)
                if tab.view:tab.view.stop()
                tab.deleteLater()
            self.header.selected={target};self.header.anchor=target;self.header.offset=min(self.header.offset,self.header.max_offset());self.header.reveal();self.refresh()
        finally:self.setUpdatesEnabled(True)
    def refresh(self):
        if not hasattr(self,'address'):return
        tab=self.tabs.get(self.active)
        if tab:
            if not self.address.hasFocus():self.address.setText(tab.url)
            self.setWindowTitle((tab.title+' — VOLT') if tab.view else 'VOLT — Navegue sem peso.')
            self.back.setEnabled(bool(tab.view and tab.view.history().canGoBack()));self.forward.setEnabled(bool(tab.view and tab.view.history().canGoForward()))
        self.header.update();self.update_status()
    def update_status(self):
        if not hasattr(self,'status'):return
        frozen=sum(bool(t.view and t.view.page().lifecycleState()==FROZEN) for t in self.tabs.values());discarded=sum(bool(t.view and t.view.page().lifecycleState()==DISCARDED) for t in self.tabs.values())
        self.status.setText(f'VOLT {VERSION} • {len(self.order)} aba(s) • {frozen} suspensa(s) • {discarded} liberada(s) • RAM ≈ {process_memory()/1_000_000:.0f} MB')
    def render_bookmarks(self):
        while self.bookmark_layout.count():
            item=self.bookmark_layout.takeAt(0)
            if item.widget():item.widget().deleteLater()
        for mark in self.settings.get('bookmarks',[]):
            b=QPushButton(mark['title'][:24]);b.setToolTip(mark['url']);b.clicked.connect(lambda _,u=mark['url']:self.navigate(u));self.bookmark_layout.addWidget(b)
        if not self.settings.get('bookmarks'):self.bookmark_layout.addWidget(QLabel('Seus favoritos ficam aqui. Use o coração para salvar uma página.'))
        self.bookmark_layout.addStretch()
    def add_bookmark(self):
        tab=self.tabs.get(self.active)
        if tab and urlparse(tab.url).scheme in ('http','https'):
            marks=self.settings.setdefault('bookmarks',[])
            if not any(x['url']==tab.url for x in marks):marks.append({'title':tab.title,'url':tab.url});self.save();self.render_bookmarks()
    def bookmark_menu(self):
        menu=QMenu(self)
        for mark in self.settings.get('bookmarks',[]):menu.addAction(mark['title'],lambda u=mark['url']:self.navigate(u))
        if menu.isEmpty():menu.addAction('Nenhum favorito salvo').setEnabled(False)
        menu.exec(self.sidebar.mapToGlobal(QPoint(48,100)))
    def toggle_whatsapp(self):
        self.wa_visible=not self.wa_visible
        if self.wa_visible:
            if not self.wa:
                self.wa=Tab(self,-1,True);self.wa_box.addWidget(self.wa,1);self.wa.pinned=self.keep_wa.isChecked();self.wa.navigate('https://web.whatsapp.com/')
            self.layout_whatsapp();self.wa_shell.show();self.wa_grip.show();self.wa_shell.raise_();self.wa_grip.raise_();self.wa.activate()
        else:self.hide_whatsapp()
    def hide_whatsapp(self):
        self.wa_visible=False;self.wa_shell.hide();self.wa_grip.hide()
        if self.wa:self.wa.hidden=time.monotonic()
        self.viewport.update()
    def layout_whatsapp(self):
        available=self.viewport.width();maximum=max(0,available-265)
        self.panel_width=max(min(340,maximum),min(self.panel_width,maximum))
        self.wa_shell.setGeometry(0,0,self.panel_width,self.viewport.height())
        self.wa_grip.setGeometry(self.panel_width,0,5,self.viewport.height())
    def install_click_filters(self):
        self.filter_scan_pending=False
        if self.closing:return
        # Filter GUI widgets only. Chromium also owns internal QObjects on other threads.
        for widget in [self]+self.findChildren(QWidget):widget.installEventFilter(self)
    def eventFilter(self,watched,event):
        if not hasattr(self,'wa_grip') or self.closing:return False
        if event.type()==QEvent.ChildAdded and not self.filter_scan_pending:
            self.filter_scan_pending=True;QTimer.singleShot(0,self.install_click_filters)
        if watched is self.viewport and event.type()==QEvent.Resize and self.wa_visible:self.layout_whatsapp()
        if self.wa_visible and event.type()==QEvent.MouseButtonPress and isinstance(watched,QWidget) and (watched is self or self.isAncestorOf(watched)):
            point=event.globalPosition().toPoint()
            def inside(widget):return widget.rect().contains(widget.mapFromGlobal(point))
            if not inside(self.wa_shell) and not inside(self.wa_grip) and not inside(self.wa_button):self.hide_whatsapp()
        return False
    def pin_whatsapp(self,checked):
        if self.wa:
            self.wa.pinned=checked
            if checked:self.wa.activate()
    def release(self,manual=False):
        if manual and QMessageBox.question(self,'Liberar memória','Liberar abas inativas? Elas serão recarregadas ao voltar. Formulários e posição da página podem ser perdidos.')!=QMessageBox.Yes:return
        for tab in list(self.tabs.values())+([self.wa] if self.wa else []):
            if tab.can_sleep():
                page=tab.view.page()
                if page.lifecycleState()!=DISCARDED:page.setLifecycleState(DISCARDED)
        self.update_status()
    def sweep(self):
        now=time.monotonic();all_tabs=list(self.tabs.values())+([self.wa] if self.wa else [])
        loaded=sum(bool(t.view and t.view.page().lifecycleState()!=DISCARDED) for t in all_tabs)
        for tab in all_tabs:
            if tab.can_sleep() and now-tab.hidden>=45:
                page=tab.view.page()
                if self.settings.get('economy',False) and (loaded>2 or now-tab.hidden>=180):page.setLifecycleState(DISCARDED)
                elif page.lifecycleState()==ACTIVE:page.setLifecycleState(FROZEN)
        used=process_memory();self.pressured=pressure(self.pressured,used)
        if self.settings.get('auto_memory',True) and self.pressured and now-self.last_clean>=30:self.last_clean=now;self.release()
        self.update_status()
    def download(self,item):
        owner=next((t for t in list(self.tabs.values())+([self.wa] if self.wa else []) if t.view and t.view.page()==item.page()),None)
        filename,_=QFileDialog.getSaveFileName(self,'Salvar download',str(Path(QStandardPaths.writableLocation(QStandardPaths.DownloadLocation))/item.downloadFileName()))
        if not filename:item.cancel();return
        item.setDownloadDirectory(str(Path(filename).parent));item.setDownloadFileName(Path(filename).name)
        if owner:owner.downloads+=1
        self.download_items.append(item)
        def finished():
            if item.isFinished() and not getattr(item,'_volt_done',False):
                item._volt_done=True
                if owner:owner.downloads=max(0,owner.downloads-1)
        item.stateChanged.connect(lambda _:finished());item.accept()
    def download_menu(self):
        menu=QMenu(self)
        for item in self.download_items:
            state=item.state().name;menu.addAction(f'{item.downloadFileName()} — {state}').setEnabled(False)
        if menu.isEmpty():menu.addAction('Nenhum download nesta sessão').setEnabled(False)
        menu.exec(self.address.mapToGlobal(QPoint(self.address.width()-100,36)))
    def menu(self):
        menu=QMenu(self);menu.addAction('Nova aba\tCtrl+T',self.new_tab);menu.addAction('Liberar abas em segundo plano…',lambda:self.release(True))
        for text,key,default in [('Limpeza automática acima de 1,2 GB','auto_memory',True),('Economia forte de memória','economy',False),('Pausar mídia ao sair da aba','pause_media',False)]:
            act=menu.addAction(text);act.setCheckable(True);act.setChecked(self.settings.get(key,default));act.toggled.connect(lambda value,k=key:self.preference(k,value))
        menu.addSeparator();menu.addAction('Senhas — importar e gerenciar…',self.password_manager)
        menu.addSeparator();menu.addAction('Modo claro' if self.dark else 'Modo escuro',self.toggle_theme)
        menu.addAction('Sobre o VOLT',lambda:QMessageBox.information(self,'VOLT',f'VOLT {VERSION}\nBase: Windows 0.6.0\nQt WebEngine / Chromium\nUma aba ativa ou com áudio pode ultrapassar a meta de memória.'))
        menu.exec(self.address.mapToGlobal(QPoint(self.address.width()-140,36)))
    def preference(self,key,value):
        self.settings[key]=value;self.save()
        if key=='pause_media':
            for tab in self.tabs.values():tab.media()
    def password_manager(self):
        from password_ui import PasswordDialog
        dialog=PasswordDialog(self)
        try:dialog.exec()
        finally:dialog.entries.clear();dialog.deleteLater()
    def fill_password(self,entry):
        from passwords import origin,fill_script
        tab=self.tabs.get(self.active)
        if not tab or not tab.view or origin(tab.url)!=entry['origin']:return
        def filled(result):
            if result!='ok':QMessageBox.information(self,'Senhas','Não foi possível identificar um formulário de login seguro nesta página. Nenhum formulário foi enviado.')
        tab.view.page().runJavaScript(fill_script(entry),filled)
    def closeEvent(self,event):
        self.closing=True;self.timer.stop();self.save()
        for tab in list(self.tabs.values())+([self.wa] if self.wa else []):
            if tab.view:tab.view.stop();tab.view.page().deleteLater();tab.view.deleteLater();tab.view=None
        self.profile.deleteLater();event.accept()

def main():
    if hasattr(os,'geteuid') and os.geteuid()==0:
        print('Execute o VOLT com seu usuário normal, sem sudo. O sandbox do Chromium permanece ativado.',file=sys.stderr);return 1
    QApplication.setApplicationName('VOLT');QApplication.setOrganizationName('VOLT');QApplication.setDesktopFileName('volt')
    app=QApplication(sys.argv);app.setStyle('Fusion');window=Browser();window.show()
    if '--self-test' in sys.argv:
        tab=window.tabs[window.active];tab.ensure_view()
        tab.view.setHtml('<html><title>VOLT engine OK</title><body>VOLT local smoke test</body></html>',QUrl('https://volt.test/'))
        def report(ok):
            if ok and tab.view.title()=='VOLT engine OK':
                print('VOLT_SELF_TEST_OK',flush=True);QTimer.singleShot(100,app.quit)
        tab.view.loadFinished.connect(report)
        QTimer.singleShot(12000,lambda:app.exit(2))
    if len(sys.argv)>1 and urlparse(sys.argv[1]).scheme in ('http','https'):window.navigate(sys.argv[1])
    return app.exec()
if __name__=='__main__':sys.exit(main())
