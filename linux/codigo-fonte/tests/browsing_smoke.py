"""Offscreen integration checks: local HTTP only, no user profile."""
import sys,time,json,threading
from pathlib import Path
from http.server import HTTPServer,BaseHTTPRequestHandler
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from PySide6.QtWidgets import QApplication
from PySide6.QtCore import Qt,QPoint,QBuffer,QIODevice
from PySide6.QtGui import QImage,QColor
from PySide6.QtTest import QTest
from volt import Browser,ACTIVE,FROZEN,DISCARDED
from policy import sleep_due,position_priority,safe_history_url
app=QApplication([]);app.setApplicationName('VOLT-Browsing-Test')
image=QImage(16,16,QImage.Format_ARGB32);image.fill(QColor('#21ad65'));buffer=QBuffer();buffer.open(QIODevice.WriteOnly);image.save(buffer,'PNG');png=bytes(buffer.data())
class Handler(BaseHTTPRequestHandler):
 def log_message(self,*args):pass
 def do_GET(self):
  body=png if self.path=='/favicon.png' else b'<html><head><title>Local favicon test</title><link rel="icon" href="/favicon.png"></head><body>VOLT</body></html>'
  self.send_response(200);self.send_header('Content-Type','image/png' if self.path=='/favicon.png' else 'text/html');self.end_headers();self.wfile.write(body)
server=HTTPServer(('127.0.0.1',0),Handler);threading.Thread(target=server.serve_forever,daemon=True).start();url=f'http://127.0.0.1:{server.server_port}/'
w=Browser();w.show();w.timer.stop();checks=[]
def check(value,label):
 if not value:raise AssertionError(label)
 checks.append(label)
def wait(predicate):
 end=time.monotonic()+15
 while time.monotonic()<end:
  app.processEvents();QTest.qWait(25)
  if predicate():return
 raise AssertionError('timeout')
try:
 check(w.sleep_minutes()==5,'default five minutes')
 check(w.settings.get('priority_tabs',0)==0,'default no position protection')
 before=len(w.order);r=w.header.plus_hit_rect();QTest.mouseClick(w.header,Qt.LeftButton,Qt.NoModifier,QPoint(r.center().x(),0));check(len(w.order)==before+1,'plus accepts top-edge click')
 tab=w.tabs[w.active];tab.navigate(url);wait(lambda:tab.ready and not tab.loading);wait(lambda:not tab.favicon.isNull())
 check(tab.favicon.width()==16 and tab.favicon.height()==16,'engine favicon reduced to 16px')
 check(url in w.settings['address_history'],'successful navigation remembered')
 w.settings['bookmarks']=[{'title':'Bookmark','url':'https://bookmark.test/path'}];w.update_completions()
 model=w.completion_model.stringList();check('bookmark.test/path' in model and '127.0.0.1:'+str(server.server_port)+'/' in model,'suggestions contain bookmark and history without requiring scheme')
 for i in range(250):w.remember_address(f'https://history{i}.test/')
 check(len(w.settings['address_history'])==200,'history bounded at 200')
 check('https://history0.test/' not in w.settings['address_history'],'old history evicted')
 check(not safe_history_url('https://name:secret@example.test/'),'credentials excluded from history')
 check(not safe_history_url('javascript:alert(1)'),'non-web URLs excluded')
 w.new_tab();w.focus_address();QTest.keyClicks(w.address,'bookmark.test');app.processEvents();w.completer.complete();app.processEvents();popup=w.completer.popup();QTest.keyClick(popup,Qt.Key_Down);QTest.keyClick(popup,Qt.Key_Return);app.processEvents()
 check('bookmark.test' in w.address.text(),'keyboard chooses address suggestion')
 # Keep test page away from any pending keyboard navigation.
 w.completer.popup().hide();w.new_tab();tab.hidden=time.monotonic()-299;w.sweep();check(tab.view.page().lifecycleState()==ACTIVE,'default does not freeze before five minutes')
 tab.hidden=time.monotonic()-301;w.sweep();check(tab.view.page().lifecycleState()==FROZEN,'default freezes after five minutes')
 w.settings['priority_tabs']=w.order.index(tab.ident)+1;w.resume_priority_tabs();check(tab.view.page().lifecycleState()==ACTIVE and not tab.can_sleep(),'priority wakes frozen page')
 w.release();check(tab.view.page().lifecycleState()==ACTIVE,'priority survives memory cleanup')
 w.settings['priority_tabs']=1;w.order.remove(tab.ident);w.order.insert(0,tab.ident);check(not tab.can_sleep(),'priority follows leftmost position')
 w.order.remove(tab.ident);w.order.append(tab.ident);check(tab.can_sleep(),'moving out of protected range makes tab eligible')
 w.settings['priority_tabs']=0;w.settings['sleep_minutes']=0;tab.hidden=time.monotonic()-86400;w.sweep();check(tab.view.page().lifecycleState()==ACTIVE,'never skips timed freeze')
 w.settings['sleep_minutes']=1;tab.hidden=time.monotonic()-61;w.sweep();check(tab.view.page().lifecycleState()==FROZEN,'configured shorter time applies')
 w.release();check(tab.view.page().lifecycleState()==DISCARDED,'memory cleanup remains independent of timer')
 check(not tab.favicon.isNull(),'favicon survives discard')
 tab.show_home();check(tab.favicon.isNull(),'home clears favicon')
 w.clear_address_history();check(not w.settings['address_history'] and 'bookmark.test/path' in w.completion_model.stringList(),'clear history retains bookmarks')
 w.settings['sleep_minutes']=15;w.settings['priority_tabs']=3;w.save();saved=json.loads((w.state_dir/'settings.json').read_text());check(saved['sleep_minutes']==15 and saved['priority_tabs']==3,'preferences persisted')
 w.grab().save('/tmp/volt-browsing.png')
 print(json.dumps({'passed':len(checks),'checks':checks}))
finally:
 w.close();app.processEvents();server.shutdown()
