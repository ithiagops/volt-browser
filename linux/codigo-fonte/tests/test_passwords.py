import unittest,tempfile,sys,json,subprocess
from pathlib import Path
from unittest.mock import patch
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from passwords import origin,read_csv,merge,SecretVault,VaultError,fill_script
class Passwords(unittest.TestCase):
 def csv(self,text):
  folder=tempfile.TemporaryDirectory();self.addCleanup(folder.cleanup);p=Path(folder.name)/'export.csv';p.write_text(text,encoding='utf-8');return p
 def test_export_quotes_bom_multiline(self):
  p=self.csv('\ufeffname,url,username,password\nExample,https://example.test/login,"user,one","FAKE quote "" and\nnewline"\n')
  entries,skip=read_csv(p);self.assertEqual(skip,0);self.assertEqual(entries[0],{'origin':'https://example.test','username':'user,one','password':'FAKE quote " and\nnewline'})
 def test_other_formats(self):
  for text in ['url;username;password\nhttps://example.test;u;FAKE\n','login_uri,login_username,login_password\nhttps://example.test,u,FAKE\n']:
   self.assertEqual(read_csv(self.csv(text))[0][0]['password'],'FAKE')
 def test_skip_invalid_and_empty(self):
  entries,skipped=read_csv(self.csv('url,username,password\njavascript:bad,u,FAKE\nhttps://u:p@example.test,u,FAKE\nhttps://example.test,u,\nhttps://good.test,u,FAKE\n'))
  self.assertEqual((len(entries),skipped),(1,3))
 def test_bad_headers(self):
  for text in ['url,password,password\nhttps://example.test,p,p','name,note\nsite,FAKE']:
   with self.assertRaises(VaultError):read_csv(self.csv(text))
 def test_exact_origins(self):
  self.assertEqual(origin('https://EXAMPLE.test:443/login'),'https://example.test')
  self.assertNotEqual(origin('http://example.test'),origin('https://example.test'))
  self.assertNotEqual(origin('https://example.test:8443'),origin('https://example.test'))
  self.assertNotEqual(origin('https://example.test.evil.test'),origin('https://example.test'))
  self.assertEqual(origin('http://example.test:0'),'http://example.test:0')
  self.assertIsNone(origin('file:///tmp/login'));self.assertIsNone(origin('https://a:b@example.test'))
 def test_merge_updates_only_same_account(self):
  old=[{'origin':'https://example.test','username':'u','password':'FAKE old'}]
  new=[{'origin':'https://example.test','username':'u','password':'FAKE new'},{'origin':'https://example.test','username':'v','password':'FAKE second'}]
  values,added,updated=merge(old,new);self.assertEqual((len(values),added,updated),(2,1,1));self.assertEqual(values[0]['password'],'FAKE new')
 def test_fail_closed_without_system_store(self):
  vault=SecretVault();vault.tool=None
  with self.assertRaises(VaultError):vault.save([])
  with self.assertRaises(VaultError):vault.load()
 def test_lookup_failure_is_not_an_empty_vault(self):
  vault=SecretVault()
  with patch.object(vault,'_run',return_value=subprocess.CompletedProcess([],1,'','locked')):
   with self.assertRaises(VaultError):vault.load()
  with patch.object(vault,'_run',return_value=subprocess.CompletedProcess([],0,'not json','')):
   with self.assertRaises(VaultError):vault.load()
 def test_save_secret_only_on_stdin(self):
  vault=SecretVault();vault.tool='/usr/bin/secret-tool';entries=[{'origin':'https://example.test','username':'u','password':'FAKE PRIVATE'}]
  with patch('passwords.subprocess.run',return_value=subprocess.CompletedProcess([],0,'','')) as run:
   vault.save(entries);args,kwargs=run.call_args;self.assertNotIn('FAKE PRIVATE',' '.join(args[0]));self.assertIn('FAKE PRIVATE',kwargs['input']);self.assertEqual(json.loads(kwargs['input'])['entries'],entries)
 def test_import_does_not_overwrite_unreadable_store(self):
  vault=SecretVault()
  with patch.object(vault,'load',side_effect=VaultError('locked')),patch.object(vault,'save') as save:
   with self.assertRaises(VaultError):vault.import_csv(self.csv('url,username,password\nhttps://example.test,u,FAKE'))
   save.assert_not_called()
 def test_full_vault_is_not_overwritten(self):
  vault=SecretVault()
  with patch('passwords.MAX_VAULT_BYTES',10),patch.object(vault,'_run') as run:
   with self.assertRaises(VaultError):vault.save([{'origin':'https://example.test','username':'u','password':'FAKE'}])
   run.assert_not_called()
 def test_fill_never_submits(self):
  script=fill_script({'origin':'https://example.test','username':'u','password':'FAKE"</script>\n'})
  self.assertIn('location.origin!==site',script);self.assertNotIn('.submit(',script);self.assertNotIn('navigator.clipboard',script)
if __name__=='__main__':unittest.main()
