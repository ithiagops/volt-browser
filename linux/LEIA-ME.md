# Atualização 0.6.3 — Importar senhas

Menu → Senhas — importar e gerenciar… → Importar CSV… . Use o CSV exportado do seu
navegador. As entradas podem ser usadas pelo botão Preencher nesta página, sem envio
automático. O protocolo, domínio e porta precisam coincidir com o login salvo.

Windows usa DPAPI do usuário; Linux usa o cofre Secret Service da sessão, sem fallback
para arquivo em texto claro. No Linux, é necessário um cofre de sessão disponível e
 desbloqueado. O AppImage inclui a ferramenta de acesso; para rodar pelo código-fonte,
instale libsecret-tools. Exclua sua cópia do CSV após importar, pois a exportação original
contém as senhas em texto claro.

Inclui a área clicável das abas até o topo e o fechamento sem confirmação genérica.
Consulte o README do repositório para detalhes do armazenamento e limitações.

---

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

# VOLT 0.6.1 — Linux

## WhatsApp sem deslocar a página

O painel agora é sobreposto. Abrir, ocultar ou redimensionar o WhatsApp não muda o tamanho
nem a posição da página que fica atrás. Clique numa área do VOLT fora do WhatsApp para
ocultá-lo; o clique também continua sendo processado pela página ou controle escolhido.
Cliques dentro do painel e em sua borda direita mantêm-no aberto. O botão lateral e o X
continuam disponíveis. Ocultar preserva a página do WhatsApp e segue a opção “Manter ativo”.
O SVG enviado continua no botão, com verde e cantos arredondados.

## Formatos

- AppImage: habilite a permissão de execução e abra `VOLT-0.6.1-x86_64.AppImage`.
- TAR.GZ: extraia a pasta e abra `INICIAR-VOLT.sh`; `INSTALAR-ATALHO.sh` cria o atalho.
- AppImage requer x86_64 e glibc 2.39+. O TAR.GZ mantém a alternativa por Python para
  distribuições anteriores compatíveis, com download de dependências na primeira execução.

O perfil, favoritos e cookies continuam no mesmo diretório do VOLT. Substituir o arquivo
AppImage ou a pasta do programa não apaga o perfil. Feche a versão anterior antes de trocar.

## Verificação desta atualização

8 testes de políticas e 28 verificações integradas passaram em Debian 13, com Qt offscreen,
CPU/motor Chromium e sandbox ativo. Incluem cliques dentro e fora, clique no conteúdo
Chromium, preservação da instância do WhatsApp, dimensões da página e altura do painel.
Login real do WhatsApp, YouTube e desktop físico X11/Wayland ainda precisam de validação.

---

# VOLT 0.6.0 para Linux

Primeira versão Linux, baseada no código e nos recursos do VOLT 0.6.0 para Windows.
Interface nativa Qt Widgets; navegação com Qt WebEngine/Chromium. Não depende de Wine.

## Abrir

1. Extraia o arquivo inteiro para uma pasta do seu usuário.
2. Abra **INICIAR-VOLT.sh**. Se seu gerenciador de arquivos perguntar, escolha executar.
3. Se necessário, abra o terminal nessa pasta e use:

```sh
chmod +x INICIAR-VOLT.sh INSTALAR-ATALHO.sh
./INICIAR-VOLT.sh
```

Para adicionar o VOLT ao menu de aplicativos:

```sh
./INSTALAR-ATALHO.sh
```

Mantenha a pasta no mesmo lugar depois de criar o atalho. Não execute com sudo.

## Compatibilidade

O pacote inclui um executável **Linux x86_64**, com Python e Qt incluídos, auditado para
**glibc 2.39 ou superior**. Ubuntu 24.04 e Debian 13 atendem a esse requisito de glibc,
mas não foram executados como distribuições separadas neste teste.
É preciso ter uma sessão gráfica e bibliotecas de desktop/OpenGL funcionando.

Em versões anteriores de glibc, o iniciador usa o código-fonte incluído e cria um ambiente
Python isolado na própria pasta. Esse primeiro preparo exige internet e baixa o Qt.
Requer **Python 3.10+**, suporte a venv e glibc 2.34+. Ubuntu 22.04 e Debian 12 podem usar
essa rota, ainda sem validação específica nessas distribuições. Alpine/musl e ARM não
são suportados por este pacote.

No Ubuntu/Debian, se faltarem dependências da rota por código-fonte:

```sh
sudo apt update
sudo apt install python3 python3-venv libxcb-cursor0 libxkbcommon-x11-0 libnss3 libegl1 libopengl0 libasound2t64
```

No Ubuntu 22.04/Debian 12, substitua `libasound2t64` por `libasound2`.
Não é necessário esse comando se o programa já abrir normalmente.

Para forçar o iniciador a usar Python mesmo em glibc recente:

```sh
VOLT_USAR_PYTHON=1 ./INICIAR-VOLT.sh
```

Para usar o código-fonte diretamente:

```sh
python3 -m venv .venv
.venv/bin/pip install -r codigo-fonte/requirements.txt
.venv/bin/python codigo-fonte/volt.py
```

## Recursos mantidos

- Página inicial nativa, busca central, atalhos arredondados com degradê e logo oficial.
- Nova aba pelo + ou Ctrl+T; endereço com foco automático; X aparece ao passar o mouse.
- Shift+clique seleciona intervalo; Ctrl+clique alterna seleção individual.
- Botão direito fecha as abas selecionadas. Arraste a seleção para mover o bloco;
  a linha indica a posição de destino. A seleção múltipla termina ao soltar.
- Abas excedentes: role a faixa com a roda do mouse.
- Tema claro/escuro pelo botão ao lado do menu. Favoritos persistentes pelo coração.
- WhatsApp na lateral, painel redimensionável, sessão persistente e “Manter ativo”.
  O primeiro acesso exige conectar sua conta pelo QR code. Sem conta pré-configurada.
- Áudio em segundo plano por padrão; opção de pausar mídia ao sair da aba no menu.
- Downloads com escolha do arquivo e permissões de sites mediante pergunta.
- Bloqueio básico dos mesmos seis domínios de rastreamento da versão Windows.
- F11 tela cheia, Ctrl+L endereço, Ctrl+W fechar seleção, Ctrl+Tab alternar.

## Memória

Abas inativas elegíveis são congeladas após 45 segundos. A limpeza automática acima
de 1,2 GB descarta páginas inativas elegíveis, que recarregam ao voltar. Repete no máximo
a cada 30 segundos enquanto a pressão continuar e encerra essa condição abaixo de 1 GB.
O menu “Liberar abas em segundo plano…” usa a mesma regra.

Aba visível, carregando, com áudio, com download ou marcada “Manter ativa” fica protegida.
O WhatsApp vem com “Manter ativo” marcado para preservar o funcionamento em segundo
plano. Desmarque para permitir congelamento/descarte quando o painel estiver oculto.
Isso pode interromper mensagens e notificações até você reabrir o painel.
Para chamadas e envio de arquivos no WhatsApp, mantenha essa opção marcada.

O descarte pode perder formulários, rolagem e estado temporário. A economia forte, opcional,
permite descartar mais cedo quando houver mais de duas páginas carregadas.
A meta de 1,2 GB **não é um limite rígido**: páginas protegidas podem excedê-la.
O indicador soma RSS da árvore de processos e pode contar memória compartilhada mais
de uma vez. Não é uma comparação direta com outros gerenciadores de tarefas.

## Dados e limites desta primeira versão

Perfil, cookies e preferências são próprios do Linux, em diretórios XDG do VOLT
(normalmente `~/.local/share/VOLT/VOLT`). O cache usa o diretório XDG de cache.
Não há importação automática do perfil Windows nem atualização automática do motor.

Testado neste ambiente Debian 13 x86_64 com plataforma gráfica offscreen, sem sessão
física de desktop. Executável e motor renderizaram conteúdo local com sandbox ativo.
A autenticação real do WhatsApp, YouTube, codecs, áudio físico, Wayland, notificações
nativas e uso contínuo precisam de validação no seu computador. Serviços com DRM ou
codecs proprietários podem ter incompatibilidades. Nunca desative o sandbox para iniciar.

Veja VALIDACAO.md para os testes e BASE-WINDOWS.md para a origem desta adaptação.
