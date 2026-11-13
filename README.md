# VOLT — Navegue sem peso.

Navegador para Windows e Linux com foco em velocidade, uso consciente de memória e uma
interface leve e minimalista. O projeto combina gerenciamento de abas, economia automática
de RAM e WhatsApp integrado à barra lateral.

**Versão atual do código: 0.6.6 — experimental.**

A faixa de abas aceita cliques até seu topo; fechar uma aba ou grupo não abre uma
confirmação genérica do VOLT. Dados temporários da aba fechada podem ser perdidos.

## Abas, memória e endereços — 0.6.6

- **Menu → Tempo para hibernar:** 1, 5 (padrão), 10, 15 ou 30 minutos; Nunca desliga a hibernação por tempo. A verificação ocorre a cada 15 segundos. Suspensão conserva a página; economia forte descarta e recarrega ao voltar.
- **Primeiras abas protegidas…:** de 0 (padrão) a 100 abas da esquerda para a direita. A proteção acompanha a ordem atual e vale também para limpeza por RAM e liberação manual. Uma página já descartada é recarregada quando ganha prioridade.
- A limpeza automática acima de 1,2 GB continua independente do prazo, inclusive em Nunca; preserva abas protegidas, ativas, carregando, com downloads ou áudio.
- Favicons de 16 × 16 obtidos do motor, sem serviço externo de ícones. São mantidos por aba, inclusive quando hiberna, e liberados ao fechar ou voltar ao início. Não há ganho de RAM comparativo medido.
- Sugestões locais de favoritos e dos últimos 200 endereços visitados com sucesso. Use setas e Enter para escolher; Windows sugere por prefixo e Linux também por trecho. O menu permite limpar esse histórico sem apagar favoritos. Nenhuma digitação é enviada a um serviço de sugestões.
- Prazo, quantidade protegida e histórico persistem localmente. No Windows: `%LocalAppData%/VOLT/browsing.json`; no Linux: `settings.json` no diretório de dados do VOLT. Endereços com credenciais embutidas são excluídos das sugestões.
- O botão + aceita cliques até o topo da faixa de abas, preservando seu desenho circular.

## Senhas importadas

Abra **Menu → Senhas — importar e gerenciar… → Importar CSV…** e escolha o CSV
exportado do navegador ou gerenciador. Chrome, Opera, Edge, Firefox e Bitwarden são
aceitos quando incluem URL e senha nas colunas esperadas. Registros do mesmo endereço
(protocolo, domínio e porta) e usuário são atualizados; linhas inválidas são ignoradas.

O VOLT não faz cópia do CSV original. Esse arquivo de exportação contém senhas em texto
claro: exclua sua cópia após concluir a importação. Nunca o adicione ao repositório.

- **Windows:** cofre em `%LocalAppData%/VOLT/passwords.dpapi`, criptografado com DPAPI
  do usuário atual. Não é sincronizado nem portável entre usuários ou instalações do Windows.
- **Linux:** dados no cofre Secret Service da sessão (por exemplo, GNOME Keyring ou
  KWallet com Secret Service). O AppImage inclui `secret-tool`; a sessão precisa ter um
  serviço de cofre disponível e desbloqueado. No código-fonte, instale `libsecret-tools`.
- Sem cofre Linux disponível, a operação informa o erro e não grava senhas em arquivo
  desprotegido. Um cofre existente ilegível não é substituído por um vazio na importação.
- Para usar um login, abra sua página, selecione-o no gerenciador e clique em
  **Preencher nesta página**. O endereço deve coincidir exatamente (protocolo, domínio e
  porta); a página também é conferida no instante do preenchimento.
- Não há envio automático do formulário, cópia para a área de transferência nem
  preenchimento em frames de outros sites. Não há captura automática de novos logins.
  Formulários com várias senhas, campos novos/de cadastro ou estruturas especiais podem
  exigir preenchimento manual.

## Recursos

- Página inicial nativa com busca e atalhos arredondados com degradê.
- Abas com seleção por Shift/Ctrl, fechamento em grupo e reordenação por arraste.
- Temas claro e escuro com alternância rápida.
- Favoritos persistentes e downloads.
- WhatsApp em painel sobreposto e redimensionável: clicar fora oculta o painel sem
  deslocar a página. A opção “Manter ativo” controla o funcionamento em segundo plano.
- Áudio em segundo plano e opção de pausar mídia ao trocar de aba.
- Suspensão de abas inativas e limpeza automática de páginas elegíveis quando a
  estimativa de memória ultrapassa 1,2 GB.

A meta de memória não é um limite rígido: abas em uso, com áudio, downloads ou proteção
podem ultrapassá-la. Descartar uma aba exige recarregar e pode perder estado temporário.
Não há medição comparativa publicada contra outros navegadores.

## Código

| Pasta | Implementação |
| --- | --- |
| `windows/` | C# / .NET 10, WinForms e WebView2 |
| `windows/Installer/` | Instalador Windows com NSIS |
| `linux/codigo-fonte/` | Python, PySide6 / Qt Widgets e Qt WebEngine |
| `linux/` | Iniciador, atalho e documentação Linux |
| `docs/` | Preparação das versões para download |

As duas plataformas têm implementações próprias. As correções equivalentes devem ser
verificadas em cada uma; o projeto não usa uma única interface compartilhada.

## Windows

Requer SDK .NET 10 para desenvolvimento e WebView2 Runtime para navegar.

```powershell
dotnet run --project windows/VOLT.csproj
dotnet run --project windows/tests/PolicyChecks.csproj -c Release
dotnet publish windows/VOLT.csproj -c Release -r win-x64 --self-contained true -o windows/dist/VOLT-0.6.6
```

Para gerar o instalador, instale NSIS 3.11 ou compatível e execute:

```powershell
powershell -ExecutionPolicy Bypass -File windows/Installer/build.ps1
```

Esse script baixa o bootstrapper oficial do WebView2 e gera o instalador localmente.
A versão autocontida inclui o .NET. O instalador prepara o WebView2, se ausente,
com internet, e instala o VOLT para o usuário atual.

## Linux

Código-fonte: Python 3.10+, glibc 2.34+ e dependências gráficas do Qt. O iniciador cria
um ambiente virtual local e baixa as dependências na primeira execução:

```bash
chmod +x linux/INICIAR-VOLT.sh
./linux/INICIAR-VOLT.sh
```

Ou instale manualmente:

```bash
python3 -m venv linux/.venv
linux/.venv/bin/pip install -r linux/codigo-fonte/requirements.txt
linux/.venv/bin/python linux/codigo-fonte/volt.py
```

Testes de políticas:

```bash
python3 -m unittest discover -s linux/codigo-fonte/tests -p 'test_*.py'
```

Teste integrado em ambiente sem monitor, com dependências Qt instaladas:

```bash
QT_QPA_PLATFORM=offscreen linux/.venv/bin/python linux/codigo-fonte/tests/smoke.py
```

O AppImage 0.6.1 distribuído anteriormente exige x86_64 e glibc 2.39+.
Compatibilidade de um novo executável depende do sistema usado na compilação.
Consulte `linux/LEIA-ME.md` para as dependências de cada distribuição.

## Downloads e validação

Instaladores, AppImage e pacotes portáteis ficam em **Releases**, quando publicados.
Eles não devem ser adicionados ao histórico do código. Veja `docs/RELEASES.md`.

As compilações e verificações estão documentadas em
`windows/VALIDACAO.md` e `linux/VALIDACAO.md`. Os testes offscreen do Linux não substituem
validação em um desktop físico. O instalador e a interface Windows precisam de teste no
Windows; login real do WhatsApp, áudio físico, codecs e serviços com DRM também precisam
de validação específica. Não há atualizador automático de versões do aplicativo.

## Componentes de terceiros

As bibliotecas mantêm suas próprias licenças. Os avisos de Qt/PySide6 e outros componentes
incluídos na distribuição Linux estão em `linux/LICENCAS/`. O projeto ainda não definiu
uma licença geral para o código do VOLT.

## Local de instalação no Windows

O instalador usa `%LocalAppData%\Programs\VOLT` na primeira instalação. Quando encontra uma instalação existente registrada (ou na pasta padrão), informa o local na tela inicial e atualiza essa mesma pasta, sem solicitar outro destino. Favoritos, configurações, perfil e cofre ficam separados em `%LocalAppData%\VOLT`. Feche o VOLT antes de atualizar.

## Acabamento e janelas — 0.6.6

As sugestões de endereço têm painel próprio no Windows e estilo explícito no Linux, com fundo e seleção seguindo o tema. O gerenciador de senhas tem cabeçalho, espaçamento, lista e botões reformulados; as regras do cofre e preenchimento não mudaram.

No Windows, a janela volta a informar os estilos nativos de redimensionamento e encaixe. O cálculo de maximização respeita a área útil do monitor, incluindo monitores em posições negativas. O arraste transmite a posição real do cursor e janelas em monitores desconectados são recuperadas para uma área visível. Win + setas e Win + Shift + setas são tratados pelo próprio Windows; dependem de o encaixe de janelas estar habilitado no sistema. O comportamento nativo precisa de teste em Windows real com dois monitores.

## Ajustes 0.6.6

O cofre vazio agora apresenta uma única ação de importação, sem tabela nem ações desabilitadas. A lista só aparece quando existem logins. No Windows, a área cliente maximizada é limitada à área útil do monitor para não desenhar abas e botões na margem externa da borda nativa. O novo cálculo foi testado com coordenadas positivas e negativas; a aparência real de maximização exige teste em Windows.
