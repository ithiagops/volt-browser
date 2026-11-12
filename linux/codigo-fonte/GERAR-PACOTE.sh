#!/bin/sh
set -eu
cd -- "$(dirname -- "$0")"
python3 -m venv .build-env
.build-env/bin/pip install -r requirements.txt pyinstaller==6.22.3
secret_tool=$(command -v secret-tool || true)
[ -n "$secret_tool" ] || { echo 'Instale libsecret-tools antes de gerar o pacote.' >&2; exit 1; }
.build-env/bin/pyinstaller --add-binary "$secret_tool:tools" --noconfirm --clean --onedir --windowed --name VOLT --add-data "$PWD/assets:assets" volt.py
echo 'Executável gerado em dist/VOLT/VOLT. Compatibilidade depende da distribuição usada na compilação.'
