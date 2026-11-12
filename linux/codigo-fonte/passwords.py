"""Password import with Secret Service storage. Never persist an unencrypted vault."""
import csv,json,shutil,subprocess,sys
from pathlib import Path
from urllib.parse import urlsplit

class VaultError(Exception):pass
MAX_BYTES=10*1024*1024
MAX_ROWS=5000
MAX_VAULT_BYTES=20*1024*1024

def origin(url):
    try:
        u=urlsplit(url.strip())
        if u.scheme.lower() not in ('http','https') or not u.hostname or u.username is not None or u.password is not None:return None
        scheme=u.scheme.lower();host=u.hostname.encode('idna').decode().lower();port=u.port
        if ':' in host:host='['+host+']'
        return scheme+'://'+host+(':'+str(port) if port is not None and port!={'http':80,'https':443}[scheme] else '')
    except (ValueError,UnicodeError):return None

def read_csv(path):
    if Path(path).stat().st_size>MAX_BYTES:raise VaultError('O CSV deve ter no máximo 10 MB.')
    entries=[];skipped=0
    try:
        with open(path,encoding='utf-8-sig',newline='') as f:
            sample=f.read(4096);f.seek(0)
            delimiter=';' if ';' in sample.splitlines()[0] and ',' not in sample.splitlines()[0] else ','
            reader=csv.DictReader(f,delimiter=delimiter)
            headers=[h.strip().lower() for h in (reader.fieldnames or [])]
            if len(set(headers))!=len(headers):raise VaultError('O CSV contém cabeçalhos duplicados.')
            def column(names):return next((h for h in names if h in headers),None)
            url_key=column(('url','website','origin','login_uri','hostname','site','endereço','endereco'))
            user_key=column(('username','user_name','login_username','usuario','usuário','nome de usuário','nome de usuario','utilizador','login'))
            pass_key=column(('password','senha','login_password'))
            if not url_key or not pass_key:raise VaultError('Use um CSV com as colunas url e password (ou equivalentes).')
            reader.fieldnames=headers
            for count,row in enumerate(reader,1):
                if count>MAX_ROWS:raise VaultError('Importe no máximo 5.000 linhas por arquivo.')
                if None in row:raise VaultError('O CSV está malformado. Exporte novamente do navegador.')
                site=origin(row.get(url_key) or '');password=row.get(pass_key) or '';username=row.get(user_key) or '' if user_key else ''
                if not site or not password or len(password)>16384 or len(username)>4096:skipped+=1;continue
                entries.append({'origin':site,'username':username,'password':password})
    except (UnicodeError,csv.Error,IndexError):raise VaultError('Não foi possível ler o CSV. Exporte em UTF-8 e tente novamente.') from None
    return entries,skipped

def merge(existing,incoming):
    values={(x['origin'],x['username']):dict(x) for x in existing};added=updated=0
    for entry in incoming:
        key=(entry['origin'],entry['username'])
        if key not in values:added+=1
        elif values[key]['password']!=entry['password']:updated+=1
        values[key]=dict(entry)
    return list(values.values()),added,updated

class SecretVault:
    def __init__(self):
        bundled=Path(getattr(sys,'_MEIPASS',Path(__file__).resolve().parent))/'tools/secret-tool'
        self.tool=str(bundled) if bundled.is_file() else shutil.which('secret-tool')
    def _run(self,args,payload=None):
        if not self.tool:raise VaultError('Instale libsecret-tools e habilite o cofre de senhas da sessão Linux para usar este recurso.')
        try:return subprocess.run([self.tool]+args,input=payload,capture_output=True,text=True,encoding="utf-8",timeout=120,check=False)
        except (OSError,subprocess.TimeoutExpired):raise VaultError('Não foi possível abrir o cofre do sistema. Desbloqueie-o e tente novamente.') from None
    def load(self):
        result=self._run(['lookup','application','volt-browser','kind','passwords-v1'])
        if result.returncode==1 and not result.stdout and not result.stderr.strip():return []
        if result.returncode or result.stderr.strip():raise VaultError('O cofre do sistema está indisponível ou bloqueado. Nenhum dado foi alterado.')
        try:
            data=json.loads(result.stdout)
            if data.get('version')!=1 or not isinstance(data.get('entries'),list):raise ValueError()
            entries=data['entries']
            if any(not isinstance(x,dict) or set(x)!= {'origin','username','password'} or not all(isinstance(v,str) for v in x.values()) or origin(x['origin'])!=x['origin'] for x in entries):raise ValueError()
            return entries
        except (ValueError,TypeError,AttributeError):raise VaultError('O cofre existente não pôde ser lido. Ele não será substituído.') from None
    def save(self,entries):
        payload=json.dumps({'version':1,'entries':entries},ensure_ascii=False)
        if len(payload.encode('utf-8'))>MAX_VAULT_BYTES:raise VaultError('O cofre atingiu o limite de tamanho. Nenhuma entrada foi alterada.')
        result=self._run(['store','--label=VOLT — Senhas','application','volt-browser','kind','passwords-v1'],payload)
        if result.returncode:raise VaultError('O cofre do sistema não confirmou a gravação. A importação não foi concluída.')
    def import_csv(self,path):
        incoming,skipped=read_csv(path)
        if not incoming:raise VaultError('Nenhuma senha de site válida foi encontrada no CSV.')
        entries,added,updated=merge(self.load(),incoming);self.save(entries)
        return entries,added,updated,skipped

# Top frame only; exact scheme/host/port; no submission, clipboard or background injection.
def fill_script(entry):
    args=json.dumps([entry['origin'],entry['username'],entry['password']],ensure_ascii=True)
    return r'''((data)=>{
      const [site,user,secret]=data;
      if(location.origin!==site)return 'origin';
      const visible=e=>!e.disabled&&!e.readOnly&&e.getClientRects().length>0;
      const passwords=[...document.querySelectorAll('input[type="password"]')].filter(e=>visible(e)&&e.autocomplete!=='new-password');
      if(passwords.length!==1)return 'fields';
      const password=passwords[0],scope=password.form||document;
      const candidates=[...scope.querySelectorAll('input')].filter(e=>visible(e)&&['text','email','tel'].includes(e.type)&&e.autocomplete!=='one-time-code');
      const username=candidates.find(e=>e.autocomplete==='username')||candidates.find(e=>e.type==='email')||candidates.find(e=>/user|login|email/i.test(e.name+' '+e.id))||(candidates.length===1?candidates[0]:null);
      const set=(e,v)=>{Object.getOwnPropertyDescriptor(HTMLInputElement.prototype,'value').set.call(e,v);e.dispatchEvent(new Event('input',{bubbles:true}));e.dispatchEvent(new Event('change',{bubbles:true}));};
      if(username&&user)set(username,user);set(password,secret);return 'ok';
    })('''+args+')'
