# Verificação 0.6.6

Compilação e publicação win-x64 concluídas; 313 verificações C# passaram, incluindo três casos de recorte da área cliente maximizada. Linux: 41 verificações integradas e 12 verificações do acabamento (dois temas, estados vazio/preenchido e ausência de senha nas colunas). O novo estado vazio não mostra tabela nem ações desabilitadas. A interface nativa e o corte em maximização precisam de validação em Windows real.

## Registros anteriores

# Verificação 0.6.5

- Publicação Windows win-x64 concluída; 310 verificações C# de políticas e geometria passaram.
- Testes de monitores simulam monitor à esquerda/acima, área útil deslocada, janela em monitor desconectado, meia tela e empacotamento de coordenadas assinadas do cursor.
- Linux: 20 testes unitários, 41 verificações integradas e 24 verificações de endereços/hibernação passaram. Mais 8 verificações do acabamento nos dois temas usando entradas fictícias.
- Interface Windows, encaixe nativo/Snap, mudanças reais de DPI e múltiplos monitores não são executáveis neste ambiente Linux. Exigem validação em Windows real.
- A janela de senhas não exibe senhas em sua tabela; mantém somente site e usuário. Regras do cofre não foram alteradas.

## Registros anteriores

# Verificação da versão 0.6.4

- Windows: compilação e publicação win-x64 sem avisos/erros, 301 verificações C# de política.
- Linux: 20 testes unitários, 41 verificações integradas existentes e 24 novas verificações offscreen; Chromium acessou um servidor HTTP local para obter favicon.
- Novos casos: limites do prazo, Nunca, proteção por posição e reordenação, prioridade durante limpeza, tamanho e retenção do favicon, sugestões locais e escolha pelo teclado, limite de 200 endereços, exclusão de URL com credenciais e persistência.
- A interface nativa, o autocomplete e o DPAPI do Windows não foram executados no Linux; precisam de validação em Windows real. Offscreen não comprova fluidez nem economia de RAM no computador do usuário.
- Mantidas as limitações anteriores do cofre e as proteções de abas com áudio/download/carregamento.

## Registro anterior

# Verificação 0.6.3 — Senhas e usabilidade das abas

- Windows: 287 verificações de geometria e políticas, incluindo CSV com BOM, aspas,
  separadores, múltiplas linhas, atualização por origem/usuário e isolamento de endereço.
- Linux: 20 testes unitários e 41 verificações integradas offscreen. Importação CSV,
  bloqueio na falta do cofre, ausência de senha nos argumentos do processo, preservação
  de cofre ilegível, preenchimento real de formulário local sem submeter, bloqueio de
  outra origem e ausência da senha nas colunas visíveis do gerenciador.
- Testes usam somente credenciais fictícias. Nenhum CSV de senhas real foi importado.
- Windows: publicação autocontida compilada; DPAPI e interface/instalador não foram
  executados neste ambiente Linux. Testar com o usuário real do Windows.
- Linux: acesso ao Secret Service real não pôde ser validado no ambiente sem sessão D-Bus.
  O backend foi testado com respostas simuladas e falha fechada; o desktop precisa ter
  um serviço de cofre disponível. O helper libsecret é incluído no AppImage.
- Cabeçalho clicável em toda a altura; no Windows maximizado, remove o recuo superior
  da janela para o clique na borda da tela atingir as abas. Em janela normal, a borda
  externa continua redimensionável. Fechamento individual/grupo sem pergunta genérica.

---

# Verificação 0.6.2

Regressões: alvos de abas no topo em 100%, 125%, 150% e 200% de DPI;
limites fora da faixa; espaços entre abas; seleção acima do X sem fechar;
fechamento direto de uma aba carregada e de um grupo sem perguntar.
Linux: 35 verificações integradas offscreen passaram, além de 8 testes de políticas.
Windows: compilação e testes de geometria/políticas; sem execução da interface WinForms
ou instalador neste ambiente Linux. O ajuste da borda nativa precisa de validação no Windows.

---

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
