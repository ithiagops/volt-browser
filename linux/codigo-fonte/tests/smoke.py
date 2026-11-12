import os,sys,time,json
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from PySide6.QtCore import QTimer,Qt,QPoint,QUrl
from PySide6.QtWidgets import QApplication,QMessageBox
from PySide6.QtTest import QTest
from volt import Browser,ACTIVE,FROZEN,DISCARDED
QApplication.setApplicationName('VOLT-Test');app=QApplication([]);app.setStyle('Fusion')
w=Browser();w.show();errors=[];checks=[]
def check(value,label):
 if not value:raise AssertionError(label)
 checks.append(label)
def wait_until(predicate,timeout=10000):
 deadline=time.monotonic()+timeout/1000
 while time.monotonic()<deadline:
  app.processEvents();QTest.qWait(25)
  if predicate():return
 raise AssertionError('timeout')
def run():
 try:
  app.processEvents();check(w.tabs[w.active].view is None,'home has no web engine view')
  first=w.active;w.address.setText('endereço anterior')
  for _ in range(5):w.new_tab()
  app.processEvents();check(w.address.hasFocus(),'new tab focuses address');check(w.address.text()=='','new tab clears previous address')
  ids=w.order[:];h=w.header
  QTest.mouseClick(h,Qt.LeftButton,Qt.NoModifier,h.rect_tab(3).center());QTest.mouseClick(h,Qt.LeftButton,Qt.ShiftModifier,h.rect_tab(5).center());check(h.selected==set(ids[3:]),'shift selects inclusive range')
  QTest.mousePress(h,Qt.LeftButton,Qt.NoModifier,h.rect_tab(4).center());QTest.mouseMove(h,QPoint(8,18),100);QTest.mouseRelease(h,Qt.LeftButton,Qt.NoModifier,QPoint(8,18));check(w.order[:3]==ids[3:],'group drag preserves order');check(h.selected=={w.active},'drop clears multi selection')
  for _ in range(12):w.toggle_theme();app.processEvents()
  check(w.dark,'theme toggles synchronously')
  w.grab().save('/tmp/volt-linux-dark.png');w.toggle_theme();w.grab().save('/tmp/volt-linux-light.png');w.toggle_theme()
  closing={x for x in w.order if x!=first};w.close_tabs(closing);app.processEvents();check(w.order==[first] and w.active==first,'bulk close selects final survivor')
  t=w.tabs[first];t.navigate('https://example.invalid');t.view.stop();t.view.setHtml('<html><title>Local smoke</title><body style="background:#181e27;color:white">VOLT local engine test<a href="https://example.com">link</a></body></html>',QUrl('https://example.test/'))
  wait_until(lambda:t.ready and not t.loading);check(t.title=='Local smoke','Chromium renders local page')
  page=t.view.page();bg=w.new_tab();t.hidden=time.monotonic()-60;w.sweep();check(page.lifecycleState()==FROZEN,'hidden idle page freezes')
  w.release();check(page.lifecycleState()==DISCARDED,'manual release discards inactive page');w.activate(first);check(page.lifecycleState()==ACTIVE,'activation restores discarded page')
  # Save image of live engine and native chrome without contacting real services.
  page.setHtml('<html><body style="background:#181e27;color:white;font:24px sans-serif">VOLT — motor Chromium funcionando</body></html>');wait_until(lambda:not t.loading)
  w.grab().save('/tmp/volt-linux-web.png')
  from shiboken6 import isValid
  old_page=t.view.page();t.show_home();app.sendPostedEvents(None,52);app.processEvents();check(not isValid(old_page),'home disposes browser page')
  page_size=w.stack.size();w.toggle_whatsapp();check(w.stack.size()==page_size,'opening overlay keeps full page size');w.wa.view.stop();w.wa.view.setHtml('<title>WhatsApp panel test</title><body>Local sidebar test</body>');wait_until(lambda:w.wa.ready and not w.wa.loading)
  check(w.wa_visible and w.wa.pinned,'WhatsApp lazy panel starts protected');w.toggle_whatsapp();check(not w.wa.can_sleep(),'hidden pinned WhatsApp protected');w.keep_wa.setChecked(False);check(w.wa.can_sleep(),'hidden unpinned WhatsApp releasable')
  w.toggle_whatsapp();w.grab().save('/tmp/volt-linux-sidebar.png');check(not w.wa.can_sleep(),'visible WhatsApp protected')
  wa_view=w.wa.view
  QTest.mouseClick(w.keep_wa,Qt.LeftButton);check(w.wa_visible,'click inside panel keeps it open')
  QTest.mouseClick(w.address,Qt.LeftButton);check(not w.wa_visible,'click on address dismisses panel');check(w.stack.size()==page_size,'closing overlay keeps full page size');check(w.wa.view is wa_view,'dismiss preserves WhatsApp page')
  QTest.mouseClick(w.wa_button,Qt.LeftButton);check(w.wa_visible,'sidebar button reopens without instant dismissal')
  home=w.tabs[w.active].home
  QTest.mouseClick(home,Qt.LeftButton,Qt.NoModifier,QPoint(home.width()-20,100));check(not w.wa_visible,'click on native home outside dismisses panel')
  t.ensure_view();t.view.setHtml('<title>Outside web click</title><body>Local outside click test</body>',QUrl('https://volt.test/'));wait_until(lambda:t.ready and not t.loading);app.processEvents()
  w.toggle_whatsapp();delegate=t.view.focusProxy();check(delegate is not None,'Chromium input widget found')
  QTest.mouseClick(delegate,Qt.LeftButton,Qt.NoModifier,QPoint(delegate.width()-20,100));check(not w.wa_visible,'click on Chromium page outside dismisses panel')
  w.toggle_whatsapp();w.panel_width=500;w.layout_whatsapp();check(w.wa_shell.width()==500 and w.stack.size()==page_size,'resizing overlay keeps page dimensions')
  app.processEvents();QTest.qWait(100);check(w.wa_shell.height()==w.viewport.height(),'overlay fills viewport height');w.grab().save('/tmp/volt-linux-sidebar-061.png')
  print(json.dumps({'passed':len(checks),'checks':checks},ensure_ascii=False),flush=True)
 except Exception as exc:
  import traceback;traceback.print_exc();errors.append(str(exc))
 finally:
  QMessageBox.question=lambda *a,**k:QMessageBox.Yes
  w.close();QTimer.singleShot(200,app.quit)
QTimer.singleShot(100,run);app.exec();sys.exit(bool(errors))
