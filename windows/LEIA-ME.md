# Atualização 0.6.2

- Área clicável das abas estendida até o topo da faixa, sem alterar o desenho.
- Pequenos espaços entre abas também aceitam seleção. O X mantém sua área própria.
- No Windows, o redimensionamento da borda externa é preservado sem capturar cliques
  na faixa superior pertencente às abas.
- Fechamento individual ou em grupo sem pergunta genérica do VOLT, inclusive com página
  carregada. Fechar uma aba encerra seu estado temporário e pode interromper downloads.
- As confirmações de outras ações (por exemplo, liberar memória manualmente) permanecem.

Código-fonte atualizado: https://github.com/ithiagops/volt-browser

---

# VOLT 0.6.1 — Windows

## Instalar ou usar sem instalação

- **VOLT-0.6.1-Setup.exe**: feche o VOLT anterior e abra o instalador. Instala para seu
  usuário em `%LocalAppData%/Programs/VOLT`, cria atalhos no menu Iniciar e na área
  de trabalho e oferece desinstalação nas configurações do Windows.
- **Pacote portátil**: extraia o ZIP inteiro e abra `VOLT-Windows/VOLT.exe`.
- O instalador e o pacote portátil usam o mesmo perfil de dados já existente,
  em `%LocalAppData%/VOLT`. A desinstalação preserva esse perfil e seus favoritos/logins.
- O .NET está incluído. Se o WebView2 estiver ausente, o instalador usa o bootstrapper
  oficial da Microsoft, que precisa de internet. Não remove nem desinstala o WebView2
  compartilhado com outros aplicativos.

## Mudanças 0.6.1

- WhatsApp abre sobre a página. Abrir/ocultar não reduz nem reposiciona o conteúdo
  da aba que fica atrás. O painel continua redimensionável pela borda direita.
- Clique em uma área do VOLT fora do painel para ocultá-lo. Clique dentro do
  WhatsApp, no botão lateral ou na borda de redimensionamento não aciona esse fechamento.
  O X continua disponível. Clicar em outro aplicativo não fecha o painel.
- Ocultar mantém a sessão conforme a opção “Manter ativo”, sem recarregar o WhatsApp.
- O botão usa o SVG enviado, com o fundo verde e cantos arredondados. O SVG original
  está incorporado; a pintura nativa usa uma imagem de 256 px derivada dele para DPI alto.
- Página inicial e área principal repintam todo o fundo quando o tamanho muda,
  evitando reaproveitar gradientes ou desenhos calculados para um tamanho anterior.

A observação de cliques fica ativa somente enquanto o painel está aberto; não bloqueia
cliques, não salva coordenadas, não captura teclas e não envia dados.

Compilação e 241 verificações passaram. Não foi possível executar a interface WinForms
ou o instalador neste ambiente Linux: confirme no Windows a abertura/ocultação, o clique
fora em páginas WebView2, o redimensionamento e o ciclo de instalar/desinstalar.
O instalador não tem assinatura digital de editor.

## Código-fonte

Incluído em `Codigo-fonte/`. Publicar com SDK .NET 10:

```text
dotnet publish VOLT.csproj -c Release -r win-x64 --self-contained true -o dist/VOLT-0.6.1
```

O instalador é gerado com NSIS 3.11, usando `Installer/VOLT.nsi`. O bootstrapper
oficial do WebView2 está em `Installer/MicrosoftEdgeWebview2Setup.exe`.
As versões anteriores da documentação aparecem abaixo como histórico.

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

# VOLT 0.5.0 — interface da referência

Extraia o ZIP em uma pasta nova e execute `VOLT-Windows/VOLT.exe` com a versão anterior fechada. Windows 10/11 x64; .NET incluído. Precisa do Microsoft Edge WebView2 Runtime.

## Interface

- Abas no topo, botão + separado, X ao passar o mouse e arraste entre abas.
- Navegação compacta, endereço arredondado, coração para favoritar, downloads, interruptor claro/escuro e menu.
- Favoritos reais abaixo do endereço: coração salva a página; clique abre; botão direito remove. A barra começa vazia e não copia os endereços da imagem de referência.
- Página inicial nativa com fundo escuro em degradê, montanhas geométricas, busca arredondada e atalhos em cápsulas.
- Nova aba foca a busca central; Enter pesquisa. Ctrl+L seleciona o endereço. Ctrl+T abre uma aba.
- Controles de janela no canto superior direito. Arraste uma área vazia da faixa de abas para mover; duplo clique maximiza/restaura. Bordas permitem redimensionar.
- Modo escuro por padrão em instalações sem preferência anterior; interruptor respeita e salva sua escolha. Sites compatíveis recebem preferência de cor.

## Memória e áudio

O tema e o início usam controles nativos, sem WebView extra. O contador é uma soma estimada de Working Set; pode contar memória compartilhada e não equivale à coluna do Gerenciador de Tarefas.

Acima de 1,2 GB no contador, a limpeza automática utiliza a mesma liberação das abas elegíveis que o botão manual. Verifica a cada 15 s, com intervalo mínimo de 30 s entre tentativas. Preserva a atual, áudio, downloads, carregamentos e Manter ativa. Não impõe teto rígido: páginas protegidas podem manter a RAM acima do gatilho.

Abas liberadas recarregam ao voltar; formulários, rolagem, histórico e estado podem ser perdidos. Para editores, uploads e chamadas, marque Manter esta aba ativa; a detecção não cobre todas essas atividades.

Para ouvir YouTube ao sair da aba, desmarque Pausar mídia ao sair da aba no menu. A página que toca áudio continua carregada.

Perfil, tema e favoritos ficam em `%LocalAppData%/VOLT`. Não apague essa pasta para atualizar. O ZIP também inclui o código-fonte e o comando de compilação.
