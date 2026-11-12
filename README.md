# VOLT — Navegue sem peso.

Navegador para Windows e Linux com foco em velocidade, uso consciente de memória e uma
interface leve e minimalista. O projeto combina gerenciamento de abas, economia automática
de RAM e WhatsApp integrado à barra lateral.

**Versão atual do código: 0.6.2 — experimental.**

A faixa de abas aceita cliques até seu topo; fechar uma aba ou grupo não abre uma
confirmação genérica do VOLT. Dados temporários da aba fechada podem ser perdidos.

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
dotnet publish windows/VOLT.csproj -c Release -r win-x64 --self-contained true -o windows/dist/VOLT-0.6.2
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

Os pacotes 0.6.1 foram compilados e tiveram suas verificações documentadas em
`windows/VALIDACAO.md` e `linux/VALIDACAO.md`. Os testes offscreen do Linux não substituem
validação em um desktop físico. O instalador e a interface Windows precisam de teste no
Windows; login real do WhatsApp, áudio físico, codecs e serviços com DRM também precisam
de validação específica. Não há atualizador automático de versões do aplicativo.

## Componentes de terceiros

As bibliotecas mantêm suas próprias licenças. Os avisos de Qt/PySide6 e outros componentes
incluídos na distribuição Linux estão em `linux/LICENCAS/`. O projeto ainda não definiu
uma licença geral para o código do VOLT.
