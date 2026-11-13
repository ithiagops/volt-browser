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

# Validação Windows 0.6.1

- Publicação win-x64 autocontida concluída sem erros.
- 241 verificações de políticas passaram, incluindo os limites de largura do painel
  e a preservação de espaço clicável fora dele em diferentes tamanhos de janela.
- SVG fornecido preservado e imagem nativa com transparência e cantos arredondados gerada.
- Instalador NSIS compilado com páginas em português, atalhos, desinstalação por usuário,
  detecção do WebView2 e bootstrapper oficial se ausente.
- Painel usa Dock.None, sem reservar uma faixa do layout principal. O controlador de
  cliques observa pressionamentos somente com o painel aberto e a janela VOLT em foco,
  incluindo cliques no conteúdo WebView2. Desinstala a observação ao ocultar ou sair.
- A transição exclui os controles ocultados do congelamento WM_SETREDRAW: reativar esse
  sinal em um HWND oculto pode restaurar WS_VISIBLE e reapresentar áreas antigas.

Limite: build/testes em Linux. Interface WinForms, hook de mouse e instalação/desinstalação
não foram executados em Windows. A correção visual precisa de confirmação no computador
real, inclusive em página inicial e site carregado, nos dois temas e após redimensionar.

Checklist Windows: abrir/ocultar pelo botão e X; clicar na busca, abas, menus e conteúdo
web fora do WhatsApp; arrastar sua borda; fechar VOLT com painel aberto; instalar e
atualizar com VOLT fechado; conferir atalhos; desinstalar e confirmar perfil preservado.

---

# VOLT 0.6.0 — barra lateral com WhatsApp

Clique no ícone verde da barra esquerda para abrir/ocultar o WhatsApp Web ao lado da página atual. Arraste a divisória para ajustar a largura. O X do painel apenas oculta; não fecha as abas. Lupa/WhatsApp, chamadas e mensagens são operados pelo usuário dentro do serviço oficial; não há envio automático.

Primeiro acesso exige conectar a conta pelo QR code ou opção fornecida pelo WhatsApp. Usa o mesmo perfil persistente do VOLT; reabrir o programa não apaga a sessão, mas o serviço pode exigir nova autenticação. Links externos abrem uma aba comum do VOLT.

Nenhum WebView é criado para o painel antes do primeiro clique. Manter ativo vem marcado para manter a conexão em segundo plano; isso consome RAM. Se desmarcado, o painel oculto pode ser suspenso após 45 s com economia ligada, ou liberado pelo gatilho de memória acima de 1,2 GB. Áudio e downloads impedem essas ações; para chamadas/uploads mantenha a opção marcada. Suspensão ou liberação podem interromper a atualização de mensagens/notificações até reabrir. O contador de RAM inclui os processos desse painel; o contador de abas mostra apenas as abas comuns.

A barra também tem Início e Favoritos funcionais. Sem Telegram/Spotify ou badges de mensagens simulados.

Compilação Windows x64 concluída e 196 verificações de regressão passaram. O ambiente Linux não executa WebView2/WinForms; login, compatibilidade com WhatsApp Web, notificações, chamadas e comportamento visual precisam ser verificados no Windows. As verificações automatizadas não conectam uma conta WhatsApp.

# VOLT 0.5.9 — repintura ao fechar abas

Fechamento individual e em grupo usam uma transação única. A aba sobrevivente definitiva é selecionada antes de remover as páginas escolhidas, com eventos intermediários de seleção suspensos. A cobertura visual permanece durante seleção, descarte e repintura final. Fechar abas em segundo plano mantém a ativa; fechar a ativa prefere a próxima à direita, depois a da esquerda. Fechar todas fecha a janela diretamente, sem mostrar conteúdo vazio antes.

Seis cenários verificam a escolha da sobrevivente em fechamento simples, de fundo, última aba, grupo, todas e ausência de seleção. Verificação visual do fechamento no Windows pendente neste ambiente Linux.

# VOLT 0.5.8 — reordenação individual sem remover a aba ativa

A reordenação de uma aba agora usa a mesma transação de grupos: quando a aba ativa é movida, ela permanece anexada ao controle e as vizinhas são reposicionadas ao redor. Mantém a cobertura temporária durante a alteração nativa e não chama navegação ou descarte de WebView. Elimina o caminho anterior de remoção/reinserção direta da aba individual que expunha uma página em repintura.

42 casos adicionais verificam equivalência entre destinos individuais e a função de movimento em bloco, preservando a ordem das demais abas. Conferência gráfica da piscada depende da execução no Windows.

# VOLT 0.5.7 — ícone oficial

Logo do SVG fornecido pelo usuário aplicada ao recurso de ícone do executável e ao ícone da janela. ICO transparente com tamanhos 16, 20, 24, 32, 40, 48, 64, 128 e 256 px, sem alterar o desenho do SVG. Identidade de aplicativo VOLT.Browser definida antes de abrir a janela para agrupamento na barra de tarefas. Fonte SVG preservada em Assets/Volt_logo.svg.

Feche versões anteriores antes de abrir. Atalhos fixados antigos podem manter o ícone em cache: desafixe o antigo e fixe novamente a nova versão. Validação gráfica da barra do Windows pendente neste ambiente Linux.

# VOLT 0.5.6 — desfazer seleção ao soltar

Depois de um arraste concluído dentro da faixa, a seleção múltipla volta para apenas a aba ativa. A nova ordem é mantida. Escape ou soltar fora da faixa cancela o movimento e preserva o grupo selecionado. O fechamento em grupo com o botão direito continua disponível antes de arrastar. Validação visual depende do Windows.

# VOLT 0.5.5 — arrastar bloco e preparar nova aba

Selecione um intervalo com clique na primeira aba e Shift+clique na última. Solte Shift, pressione o título de qualquer aba destacada e arraste: o bloco inteiro muda de posição, mantendo sua ordem e a aba ativa. Um clique sem arrastar volta à seleção individual. Escape ou soltar fora da faixa cancela o arraste. O menu de fechar o grupo permanece disponível.

O reordenamento remove/reinsere os mesmos objetos TabPage em lote, com seleção intermediária suprimida; não navega, não descarta WebViews e mantém áudio. Prévia do bloco e vizinhos é desenhada na faixa; grupos grandes ficam limitados visualmente à largura disponível.

Nova aba é construída e recebe tema antes da seleção. A cobertura transitória do quadro anterior permanece durante a construção de controles e a apresentação, sem manter bitmap depois. Não há cobertura durante navegação/requisição de rede.

Publicação Windows x64 concluída. 148 verificações passaram, incluindo as três últimas para o início, as três primeiras para o fim, soltar dentro do bloco, bloco inteiro, vazio e preservação da ordem/identidade em todos os destinos. O ambiente Linux não executa WinForms; confirmar no Windows a interação do arraste e a ausência de piscadas na abertura.

# VOLT 0.5.4 — selecionar intervalo de abas

Clique na primeira aba; segure Shift e clique na última, em qualquer sentido. Todas as abas entre elas ficam destacadas, inclusive as fora da faixa visível. Botão direito dentro da seleção preserva o grupo e oferece Fechar N abas selecionadas. Uma confirmação fecha o lote inteiro. Botão direito fora do grupo seleciona apenas aquela aba. Clique normal substitui a seleção. O X individual continua fechando uma só aba.

Fechar o grupo encerra também áudio e downloads das abas escolhidas, e pode perder formulários não salvos. A confirmação é única por grupo. Fechar todas as abas fecha a janela. Durante a remoção em lote, as mudanças intermediárias de seleção não carregam páginas; a aba restante é ativada ao terminar.

Publicação Windows x64 concluída. 129 verificações passaram, incluindo intervalo inclusivo, direção inversa, âncora original, mesmo extremo e remoção da âncora. Eventos de mouse e desenho ainda precisam de verificação no Windows.

# VOLT 0.5.3 — quadro intermediário da troca de tema

A troca mantém uma cobertura visual temporária com o quadro anterior enquanto os controles nativos repintam. Aguarda a apresentação pelo compositor antes de retirar a cobertura. Janela sem ativação, sem timer e sem captura persistida: bitmap somente em RAM, descartado ao fim da troca. Se a captura falhar, usa a repintura anterior como fallback. A cobertura custa memória transitória proporcional à área da janela; não permanece após a troca. Não altera sites, áudio ou políticas de descarte.

Publicação Windows x64 executada. O ambiente Linux não confirma o resultado gráfico/compositor do Windows; verificar alternâncias repetidas nos dois sentidos e foco da busca no Windows. Não há medição de latência.

# VOLT 0.5.2 — correção dos cantos dos atalhos

Os botões de atalho agora têm região de janela em cápsula, atualizada ao criar o handle e redimensionar. O fundo volta a ser opaco; os pixels fora da cápsula não pertencem ao botão e não recebem a pintura nativa retangular. Gradiente e hover mantidos, nos dois temas. Compilação Windows x64 executada; confirmação visual no Windows pendente.

# VOLT 0.5.1 — acabamento visual

- Troca de tema: bloqueia o redesenho apenas dos controles nativos visíveis durante a aplicação da paleta e repinta a árvore ao concluir. Não cria handles nem torna abas ocultas visíveis. Removido WS_EX_COMPOSITED da página inicial; mantém double buffer local. A cor dos sites é enviada uma vez ao perfil compartilhado, após a atualização nativa; o tempo de resposta dos sites continua dependendo deles.
- Atalhos: fundo transparente fora das cápsulas e preenchimento em gradiente vertical nos temas claro e escuro. Estado de hover preservado.
- Endereço e busca central usam um contêiner que centraliza o TextBox pela altura preferida da fonte, incluindo mudanças de DPI.
- Ícone direito da busca segue a referência de lente e abre Google Lens para pesquisa por imagem. Enter ou lupa continuam pesquisando texto.
- Publicação Release Windows x64 concluída. Sem testes visuais executáveis neste Linux: confirmar ausência de cores misturadas, gradiente, arredondamento e alinhamento do cursor no Windows. Não foram medidos tempos de troca de tema.

# Validação — VOLT 0.5.0

## Executado

- Publicação .NET Release autocontida Windows x64 concluída sem erros.
- 123 verificações de políticas e geometria passaram: navegação, domínios, proteções, reordenação, limites de memória e abas visíveis em diferentes larguras.
- Revisão da composição: faixa de título/abas acima da navegação e favoritos, página preenchendo a área restante, status inferior. Sem novo WebView ou timer para desenhar a interface.
- Botão de tema usa o mesmo evento da opção do menu e o mesmo arquivo de preferência.
- Ícones desenhados em GDI+, sem dependência de glifos emoji ou imagens remotas.
- Favoritos carregados/salvos em JSON local com validação HTTP/HTTPS; links da referência não foram incorporados.

## Limites de validação

O ambiente é Linux e não executa a interface WinForms/WebView2 para Windows. A compilação e os testes de política não comprovam fidelidade visual, foco, DPI, redimensionamento, maximização em múltiplos monitores, animações ou consumo real. Esses comportamentos precisam de execução no Windows.

Conferência recomendada: abrir aba (+ e Ctrl+T), digitar na busca, pesquisar, favoritar/remover, trocar tema/reabrir, arrastar/reordenar abas, arrastar/maximizar/restaurar/redimensionar janela e ouvir áudio em fundo. A liberação automática continua sujeita às proteções descritas no LEIA-ME.
