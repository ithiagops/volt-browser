from PySide6.QtCore import QThread,Signal,Qt,QTimer,QRectF
from PySide6.QtWidgets import QDialog,QVBoxLayout,QHBoxLayout,QLabel,QPushButton,QTreeWidget,QTreeWidgetItem,QFileDialog,QMessageBox,QWidget
from PySide6.QtGui import QPainter,QPen,QColor
from passwords import SecretVault,VaultError,origin

class VaultJob(QThread):
    result=Signal(object)
    failed=Signal(str)
    def __init__(self,operation,parent):super().__init__(parent);self.operation=operation
    def run(self):
        try:self.result.emit(self.operation())
        except VaultError as exc:self.failed.emit(str(exc))
        except Exception:self.failed.emit('Não foi possível concluir a operação. Nenhuma senha será exibida em mensagens de erro.')

class VaultMark(QWidget):
    def __init__(self,dark):super().__init__();self.dark=dark;self.setFixedSize(64,64)
    def paintEvent(self,event):
        p=QPainter(self);p.setRenderHint(QPainter.Antialiasing);p.setPen(Qt.NoPen);p.setBrush(QColor('#212b39' if self.dark else '#e5edf7'));p.drawEllipse(3,3,58,58)
        p.setBrush(Qt.NoBrush);p.setPen(QPen(QColor('#afc9e7' if self.dark else '#4b688b'),1.8));p.drawArc(QRectF(25,16,14,19),0,180*16);p.drawRoundedRect(QRectF(20,28,24,20),4,4);p.drawLine(32,35,32,40)

class PasswordDialog(QDialog):
    def __init__(self,browser):
        super().__init__(browser);self.browser=browser;self.entries=[];self.worker=None;self.setWindowTitle('Senhas — VOLT');self.resize(744,504);self.setMinimumSize(680,460)
        bg='#181e27' if browser.dark else '#f3f7fb';surface='#232b36' if browser.dark else '#ffffff';fg='#e5ecf4' if browser.dark else '#253343';muted='#9eaec1' if browser.dark else '#5c6f84';selected='#344354' if browser.dark else '#dfebf8'
        self.setObjectName('passwordDialog')
        self.setStyleSheet(f'''QDialog#passwordDialog {{background:{bg};color:{fg};}} QLabel {{color:{muted};}} QLabel#vaultTitle {{color:{fg};font-size:28px;font-weight:400;}} QTreeWidget {{background:{surface};color:{fg};border:0;border-radius:12px;padding:8px;outline:0;}} QTreeWidget::item {{height:40px;padding:0 10px;border:0;}} QTreeWidget::item:selected {{background:{selected};color:{fg};border-radius:7px;}} QHeaderView::section {{background:{surface};color:{muted};border:0;padding:12px 10px;font-weight:600;}} QPushButton {{background:{surface};color:{fg};border:0;border-radius:10px;padding:8px 16px;min-height:22px;}} QPushButton:hover {{background:{selected};}} QPushButton:disabled {{color:{muted};}} QPushButton#primary {{background:{'#d7e6f7' if browser.dark else '#223147'};color:{'#182331' if browser.dark else '#ffffff'};}}''')
        layout=QVBoxLayout(self);layout.setContentsMargins(24,22,24,22);layout.setSpacing(16)
        title=QLabel('Senhas');title.setObjectName('vaultTitle');layout.addWidget(title)
        layout.addWidget(QLabel('Seu cofre pessoal no VOLT'))
        help=QLabel('O CSV contém senhas em texto claro. Exclua a cópia exportada após importar.\nO VOLT guarda os dados no cofre da sua sessão Linux; não cria um arquivo de senhas em texto claro.');help.setWordWrap(True)
        self.table=QTreeWidget();self.table.setHeaderLabels(['Site','Usuário']);self.table.setRootIsDecorated(False);layout.addWidget(self.table,1)
        self.empty=QWidget();empty_box=QVBoxLayout(self.empty);empty_box.setContentsMargins(0,4,0,0);empty_box.setSpacing(12);empty_box.addStretch();empty_box.addWidget(VaultMark(browser.dark),0,Qt.AlignHCenter)
        heading=QLabel('Seus acessos, em um só lugar');heading.setAlignment(Qt.AlignCenter);heading.setStyleSheet(f'color:{fg};font-size:20px;');empty_box.addWidget(heading)
        note=QLabel('Importe as senhas do seu navegador para começar.');note.setAlignment(Qt.AlignCenter);empty_box.addWidget(note)
        self.empty_import=QPushButton('Importar senhas');self.empty_import.setObjectName('primary');self.empty_import.setFixedWidth(174);self.empty_import.clicked.connect(self.import_csv);empty_box.addWidget(self.empty_import,0,Qt.AlignHCenter)
        formats=QLabel('Chrome · Opera · Edge · Firefox · Bitwarden');formats.setAlignment(Qt.AlignCenter);empty_box.addWidget(formats);empty_box.addStretch();layout.addWidget(self.empty,1);self.empty.hide()
        self.status=QLabel('Abrindo cofre do sistema…');self.status.setWordWrap(True);layout.addWidget(self.status);layout.addWidget(help)
        buttons=QHBoxLayout();self.import_button=QPushButton('Importar CSV…');self.import_button.setObjectName('primary');self.import_button.clicked.connect(self.import_csv);buttons.addWidget(self.import_button)
        self.fill=QPushButton('Preencher nesta página');self.fill.clicked.connect(self.fill_selected);buttons.addWidget(self.fill)
        self.remove=QPushButton('Remover');self.remove.clicked.connect(self.remove_selected);buttons.addWidget(self.remove)
        self.close_button=QPushButton('Fechar');self.close_button.clicked.connect(self.reject);buttons.addWidget(self.close_button);layout.addLayout(buttons)
        QTimer.singleShot(0,lambda:self.job(lambda:SecretVault().load(),self.loaded))
    def job(self,operation,done):
        if self.worker is not None:return
        for b in (self.import_button,self.fill,self.remove,self.empty_import):b.setEnabled(False)
        self.worker=VaultJob(operation,self);worker=self.worker
        worker.result.connect(done);worker.failed.connect(self.status.setText)
        def finished():
            self.worker=None;worker.deleteLater()
            for b in (self.import_button,self.fill,self.remove,self.empty_import):b.setEnabled(True)
        worker.finished.connect(finished);worker.start()
    def loaded(self,entries):
        self.entries=entries;self.table.clear()
        for index,entry in enumerate(entries):
            item=QTreeWidgetItem([entry['origin'],entry['username'] or '(sem usuário)']);item.setData(0,Qt.UserRole,index);self.table.addTopLevelItem(item)
        self.table.resizeColumnToContents(0);has_entries=bool(entries);self.table.setVisible(has_entries);self.empty.setVisible(not has_entries)
        for button in (self.import_button,self.fill,self.remove,self.close_button):button.setVisible(has_entries)
        self.status.setText(f'{len(entries)} acesso(s) · Selecione um login para preencher sem enviar o formulário.' if has_entries else '')
    def selected(self):
        item=self.table.currentItem()
        return self.entries[item.data(0,Qt.UserRole)] if item else None
    def import_csv(self):
        path,_=QFileDialog.getOpenFileName(self,'Importar senhas','', 'CSV de senhas (*.csv)')
        if not path:return
        self.status.setText('Importando para o cofre do sistema…')
        def imported(result):
            entries,added,updated,skipped=result;self.loaded(entries);self.status.setText(f'Importados: {added}. Atualizados: {updated}. Linhas ignoradas: {skipped}.')
        self.job(lambda:SecretVault().import_csv(path),imported)
    def fill_selected(self):
        entry=self.selected()
        if not entry:return
        tab=self.browser.tabs.get(self.browser.active)
        if not tab or not tab.view or origin(tab.url)!=entry['origin']:
            self.status.setText('Abra a página de login do mesmo endereço (protocolo, domínio e porta) antes de preencher.');return
        self.accept();self.browser.fill_password(entry)
    def remove_selected(self):
        entry=self.selected()
        if not entry:return
        if QMessageBox.question(self,'Remover login','Remover este login do cofre do VOLT?')!=QMessageBox.Yes:return
        def remove():
            vault=SecretVault();entries=[x for x in vault.load() if (x['origin'],x['username'])!=(entry['origin'],entry['username'])];vault.save(entries);return entries
        self.job(remove,self.loaded)
    def reject(self):
        if self.worker is not None:self.status.setText('Aguarde a resposta do cofre do sistema ou cancele a solicitação no diálogo do sistema.');return
        super().reject()
    def closeEvent(self,event):
        if self.worker is not None:event.ignore();return
        super().closeEvent(event)
