# Linux 0.6.1 — validação adicional

- 8 testes de políticas passaram.
- 28 verificações integradas passaram, incluindo 11 verificações novas para o painel:
  página sem redimensionamento ao abrir/fechar/arrastar borda; clique interno sem fechar;
  clique na barra e na página inicial fechando; clique no widget de entrada do Chromium
  fechando; preservação da mesma WebView do WhatsApp; botão lateral sem fechar imediatamente;
  altura do painel igual à área de conteúdo.
- Filtros de clique limitados aos widgets GUI do próprio VOLT, inclusive widgets de entrada
  criados pelo Chromium. Não usa hook global, timer de mouse, registro de cliques ou captura
  de tela para detectar cliques fora.
- Cabeçalho do painel fixo em 40 px; restante da área destinado à WebView.
- Inspeção visual em capturas offscreen; a sessão real do WhatsApp não foi autenticada.
- Compilação PyInstaller e teste local do motor no AppImage executados separadamente.

A validação offscreen não equivale a um teste de login, áudio físico ou todas as GPUs e
compositores X11/Wayland. As instruções e limitações de compatibilidade anteriores seguem abaixo.

---

# Validação — Linux 0.6.0

Ambiente: Debian 13 x86_64, glibc 2.41, Python 3.12.14,
PySide6/Qt 6.12.0, Qt WebEngine Chromium 140.0.7339.225.

- 8 testes de políticas passaram. O teste combinatório cobre todas as seleções
  não vazias e posições de destino em sequências de 1 a 8 abas.
- 17 verificações integradas passaram: página inicial sem WebView; foco na nova aba;
  intervalo Shift; arraste em grupo e ordem preservada; seleção limpa após soltar;
  12 alternâncias de tema; fechamento em grupo com sobrevivente final;
  renderização Chromium local; congelamento, descarte e restauração de página;
  limpeza do endereço da nova aba; destruição da página ao voltar ao início;
  criação tardia do painel WhatsApp; proteção ativo/oculto fixado e liberação
  quando oculto sem fixação.
- Inspeção visual de capturas offscreen dos temas claro e escuro: título, busca,
  degradês, contornos arredondados, logo e ícones vetoriais.
- Executável PyInstaller gerado para Linux x86_64 e iniciado com sandbox ativo.
- Auditoria de símbolos dos ELF empacotados: requisito máximo GLIBC_2.39.
- A execução offscreen registra limitações de GPU, D-Bus e dispositivos deste ambiente.

Sem validação de login WhatsApp, internet real dentro dos sites, áudio físico,
codecs/DRM, sessões desktop X11/Wayland ou medições comparativas de desempenho.
Isso não comprova ausência de piscadas em todas as combinações de GPU/compositor.

Reproduzir com o ambiente Python instalado:

```sh
python -m unittest discover -s tests -p 'test_*.py'
QT_QPA_PLATFORM=offscreen python tests/smoke.py
```

O executável também possui `--self-test`, que abre uma página HTML local,
confirma o título renderizado pelo motor e sai, sem autenticar ou enviar mensagens.
