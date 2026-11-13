from PySide6.QtCore import QThread,Signal,Qt,QTimer
from PySide6.QtWidgets import QDialog,QVBoxLayout,QHBoxLayout,QLabel,QPushButton,QTreeWidget,QTreeWidgetItem,QFileDialog,QMessageBox
from passwords import SecretVault,VaultError,origin

class VaultJob(QThread):
    result=Signal(object)
    failed=Signal(str)
    def __init__(self,operation,parent):super().__init__(parent);self.operation=operation
    def run(self):
        try:self.result.emit(self.operation())
        except VaultError as exc:self.failed.emit(str(exc))
        except Exception:self.failed.emit('Não foi possível concluir a operação. Nenhuma senha será exibida em mensagens de erro.')

class PasswordDialog(QDialog):
    def __init__(self,browser):
        super().__init__(browser);self.browser=browser;self.entries=[];self.worker=None;self.setWindowTitle('Senhas — VOLT');self.resize(780,540);self.setMinimumSize(680,460)
        bg='#181e27' if browser.dark else '#f3f7fb';surface='#232b36' if browser.dark else '#ffffff';fg='#e5ecf4' if browser.dark else '#253343';muted='#9eaec1' if browser.dark else '#5c6f84';selected='#344354' if browser.dark else '#dfebf8'
        self.setObjectName('passwordDialog')
        self.setStyleSheet(f'''QDialog#passwordDialog {{background:{bg};color:{fg};}} QLabel {{color:{muted};}} QLabel#vaultTitle {{color:{fg};font-size:28px;font-weight:400;}} QTreeWidget {{background:{surface};color:{fg};border:0;border-radius:12px;padding:8px;outline:0;}} QTreeWidget::item {{height:40px;padding:0 10px;border:0;}} QTreeWidget::item:selected {{background:{selected};color:{fg};border-radius:7px;}} QHeaderView::section {{background:{surface};color:{muted};border:0;padding:12px 10px;font-weight:600;}} QPushButton {{background:{surface};color:{fg};border:0;border-radius:18px;padding:8px 16px;min-height:22px;}} QPushButton:hover {{background:{selected};}} QPushButton:disabled {{color:{muted};}} QPushButton#primary {{background:{selected};}}''')
        layout=QVBoxLayout(self);layout.setContentsMargins(24,22,24,22);layout.setSpacing(16)
        title=QLabel('Senhas');title.setObjectName('vaultTitle');layout.addWidget(title)
        layout.addWidget(QLabel('Seus acessos, guardados no cofre da sessão.'))
        layout.addWidget(QLabel('Importe o CSV do Chrome, Opera, Edge, Firefox ou Bitwarden.'))
        help=QLabel('O CSV contém senhas em texto claro. Exclua a cópia exportada após importar.\nO VOLT guarda os dados no cofre da sua sessão Linux; não cria um arquivo de senhas em texto claro.');help.setWordWrap(True);layout.addWidget(help)
        self.table=QTreeWidget();self.table.setHeaderLabels(['Site','Usuário']);self.table.setRootIsDecorated(False);layout.addWidget(self.table,1)
        self.status=QLabel('Abrindo cofre do sistema…');self.status.setWordWrap(True);layout.addWidget(self.status)
        buttons=QHBoxLayout();self.import_button=QPushButton('Importar CSV…');self.import_button.setObjectName('primary');self.import_button.clicked.connect(self.import_csv);buttons.addWidget(self.import_button)
        self.fill=QPushButton('Preencher nesta página');self.fill.clicked.connect(self.fill_selected);buttons.addWidget(self.fill)
        self.remove=QPushButton('Remover');self.remove.clicked.connect(self.remove_selected);buttons.addWidget(self.remove)
        close=QPushButton('Fechar');close.clicked.connect(self.reject);buttons.addWidget(close);layout.addLayout(buttons)
        QTimer.singleShot(0,lambda:self.job(lambda:SecretVault().load(),self.loaded))
    def job(self,operation,done):
        if self.worker is not None:return
        for b in (self.import_button,self.fill,self.remove):b.setEnabled(False)
        self.worker=VaultJob(operation,self);worker=self.worker
        worker.result.connect(done);worker.failed.connect(self.status.setText)
        def finished():
            self.worker=None;worker.deleteLater()
            for b in (self.import_button,self.fill,self.remove):b.setEnabled(True)
        worker.finished.connect(finished);worker.start()
    def loaded(self,entries):
        self.entries=entries;self.table.clear()
        for index,entry in enumerate(entries):
            item=QTreeWidgetItem([entry['origin'],entry['username'] or '(sem usuário)']);item.setData(0,Qt.UserRole,index);self.table.addTopLevelItem(item)
        self.table.resizeColumnToContents(0);self.status.setText(f'{len(entries)} login(s) no cofre. Selecione um para preencher; não haverá envio automático do formulário.')
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
