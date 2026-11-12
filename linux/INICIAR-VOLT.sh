#!/bin/sh
set -eu
volt_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
if [ "$(id -u)" = 0 ]; then
  echo 'Abra o VOLT com seu usuário normal, sem sudo.' >&2;exit 1
fi
# Executable audited to require glibc >= 2.39; older supported distributions use Python/Qt wheels.
volt_glibc=$(getconf GNU_LIBC_VERSION 2>/dev/null | cut -d ' ' -f 2 || true)
if [ "${VOLT_USAR_PYTHON:-0}" != 1 ] && [ -x "$volt_dir/portatil/VOLT" ] && [ "$(uname -m)" = x86_64 ] && [ -n "$volt_glibc" ] && awk -v v="$volt_glibc" 'BEGIN {split(v,a,".");exit !(a[1]>2 || (a[1]==2 && a[2]>=39))}'; then
  exec "$volt_dir/portatil/VOLT" "$@"
fi
command -v python3 >/dev/null 2>&1 || { echo 'Instale Python 3.10 ou superior para iniciar nesta distribuição.' >&2;exit 1; }
python3 -c 'import sys;sys.exit(sys.version_info < (3,10))' || { echo 'É necessário Python 3.10 ou superior.' >&2;exit 1; }
volt_venv="$volt_dir/.venv"
if [ ! -x "$volt_venv/bin/python" ]; then
  echo 'Preparando o VOLT para esta distribuição…'
  python3 -m venv "$volt_venv" || { echo 'Instale python3-venv e tente novamente. Veja LEIA-ME.md.' >&2;exit 1; }
fi
if ! "$volt_venv/bin/python" -c 'import PySide6.QtWebEngineWidgets' >/dev/null 2>&1; then
  echo 'Baixando o motor de navegação. A primeira execução exige internet.'
  "$volt_venv/bin/python" -m pip install -r "$volt_dir/codigo-fonte/requirements.txt"
fi
exec "$volt_venv/bin/python" "$volt_dir/codigo-fonte/volt.py" "$@"
