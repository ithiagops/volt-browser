# Distribuição 0.6.1

Crie uma Release com tag `v0.6.1` depois de enviar o código-fonte.
Confirme a branch/commit que corresponde aos pacotes e marque a versão como experimental
(pre-release) enquanto a validação no computador real estiver pendente.

Arquivos de distribuição produzidos anteriormente:

- `VOLT-0.6.1-Setup.exe` — instalador Windows por usuário.
- `VOLT-0.6.1-Windows.zip` — Windows portátil e código-fonte.
- `VOLT-0.6.1-x86_64.AppImage` — Linux x86_64, glibc 2.39+.
- `VOLT-0.6.1-Linux-x86_64.tar.gz` — pacote Linux com alternativa por Python.

Anexe esses arquivos como assets da Release, e não pelo envio de arquivos do repositório.
Os pacotes não são gerados nem publicados automaticamente por este repositório.

Notas sugeridas: painel do WhatsApp sobreposto, página sem redimensionamento ao ocultar,
fechamento ao clicar fora, SVG oficial enviado para o botão e distribuição Windows/Linux.

O código-fonte do instalador está em `windows/Installer/VOLT.nsi`. Seu bootstrapper
WebView2 é baixado pelo script `build.ps1`, a partir do endereço oficial da Microsoft;
não é versionado no Git.
