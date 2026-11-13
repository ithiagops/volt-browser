import sys,time,json
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from PySide6.QtWidgets import QApplication
from PySide6.QtCore import Qt
from PySide6.QtTest import QTest
from volt import Browser
import password_ui
class FakeVault:
 def load(self):return [{'origin':'https://example.test','username':'usuario@exemplo.test','password':'FAKE-ONLY'}]
password_ui.SecretVault=FakeVault
app=QApplication([]);app.setApplicationName('VOLT-Polish-Test');w=Browser();w.show();w.timer.stop();checks=[]
def check(ok,label):
 assert ok,label
 checks.append(label)
for dark in (True,False):
 if w.dark!=dark:w.toggle_theme()
 w.settings['bookmarks']=[{'title':'Local','url':'https://example.test/'}];w.update_completions();w.focus_address();w.address.setText('example');w.completer.setCompletionPrefix('example');w.completer.complete();app.processEvents()
 style=w.completer.popup().styleSheet();check(('#232b36' if dark else '#ffffff') in style,'popup uses theme surface')
 w.completer.popup().grab().save('/tmp/volt-suggestions-'+('dark' if dark else 'light')+'.png');w.completer.popup().hide()
 d=password_ui.PasswordDialog(w);d.show()
 deadline=time.monotonic()+3
 while time.monotonic()<deadline:
  app.processEvents();QTest.qWait(20)
  if d.worker is None and d.entries:break
 check(d.table.topLevelItemCount()==1,'vault entries rendered')
 check(d.table.columnCount()==2 and d.table.topLevelItem(0).text(1)=='usuario@exemplo.test','only site and username shown')
 check('FAKE-ONLY' not in d.table.topLevelItem(0).text(0)+d.table.topLevelItem(0).text(1),'secret not visible')
 d.grab().save('/tmp/volt-vault-'+('dark' if dark else 'light')+'.png');d.loaded([]);check(not d.table.isVisible() and d.empty.isVisible(),'empty vault hides unused table');check(not d.fill.isVisible() and d.empty_import.isVisible(),'empty vault has one import action');d.grab().save('/tmp/volt-vault-empty-'+('dark' if dark else 'light')+'.png');d.reject();d.entries.clear();d.deleteLater();app.processEvents()
w.close();app.processEvents();print(json.dumps({'passed':len(checks),'checks':checks}))
