#!/bin/sh
set -eu
volt_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
command -v python3 >/dev/null || { echo 'Instale python3 para criar o atalho.';exit 1; }
python3 - "$volt_dir" <<'PY'
import sys,os
from pathlib import Path
root=Path(sys.argv[1]);start=root/'INICIAR-VOLT.sh'
def esc(value):
 return str(value).replace('\\','\\\\').replace('"','\\"').replace('`','\\`').replace('$','\\$').replace('%','%%')
folder=Path(os.environ.get('XDG_DATA_HOME',str(Path.home()/'.local/share')))/'applications';folder.mkdir(parents=True,exist_ok=True)
(folder/'volt.desktop').write_text('[Desktop Entry]\nType=Application\nName=VOLT\nComment=Navegue sem peso\nExec="'+esc(start)+'" %u\nIcon='+str(root/'codigo-fonte/assets/Volt.png')+'\nTerminal=false\nCategories=Network;WebBrowser;\nMimeType=x-scheme-handler/http;x-scheme-handler/https;\nStartupWMClass=VOLT\n')
print('Atalho VOLT criado no menu de aplicativos. Mantenha esta pasta no mesmo lugar.')
PY
