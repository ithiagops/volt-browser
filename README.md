# ⚡ VOLT — Navegue sem peso.

> Navegador leve, rápido e consciente de recursos para **Windows** e **Linux**, desenvolvido para oferecer alta performance, consumo inteligente de memória e uma experiência minimalista sem distrações.

[![Windows Version](https://img.shields.io/badge/Windows-v0.6.8-blue?logo=windows)](VALIDACAO.md)
[![Linux Version](https://img.shields.io/badge/Linux-v0.6.6-orange?logo=linux)](linux/VALIDACAO.md)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple?logo=dotnet)](VOLT.csproj)
[![WebView2](https://img.shields.io/badge/Engine-WebView2%20%7C%20QtWebEngine-green)](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)

---

## 🌟 Principais Recursos

### 🧠 Gestão Inteligente de Memória e Abas
- **Hibernação Programada:** Suspenda abas inativas automaticamente após 1, 5 (padrão), 10, 15 ou 30 minutos (ou desative definindo *Nunca*). A rotina verifica o estado a cada 15 segundos.
- **Proteção de Abas Prioritárias:** Defina de 0 a 100 abas (da esquerda para a direita) que nunca serão hibernadas ou descartadas automaticamente.
- **Alívio de RAM Automático:** Limpeza preventiva ativada quando a estimativa de consumo do navegador ultrapassa **1,2 GB**, preservando sempre abas protegidas, ativas, com áudio ou em download.
- **Navegação Eficiente de Abas:** Reordenação por arraste suave, seleção múltipla (Shift/Ctrl), fechamento em lote direto e área clicável estendida até o topo da tela.

### 🎨 Interface Moderna & Customizável
- **Novo Painel de Menu:** Botões de ação em formato de pílula, menu de ferramentas com rolagem suave e fechamento automático ao clicar fora.
- **Temas Claro e Escuro:** Suporte completo com transições suaves e sem tremulação (*anti-flickering*).
- **Página Inicial Nativa:** Acesso rápido a sites favoritos, atalhos dinâmicos com visual em degradê e campo de busca integrado.
- **Gerenciamento de Janelas (Snap & Multi-Monitor):** Encaixe nativo (Windows Snap), suporte a coordenadas e posições negativas em múltiplos monitores e sincronização imediata de layout ao restaurar pela barra de tarefas.

### 💬 WhatsApp Web Integrado
- **Painel Lateral Sobreposto:** Abra e oculte o WhatsApp Web instantaneamente na lateral sem redimensionar ou reposicionar a página em exibição.
- **Controle em Segundo Plano:** Opção *“Manter ativo”* para preservar conexões e chamadas, ou permitir liberação de memória quando o painel for ocultado.
- **Privacidade Visual:** Clicar fora do painel oculta o mensageiro instantaneamente.

### 🔒 Privacidade, Cofre & Segurança Nativa
- **Cofre Criptografado do Sistema:**
  - **Windows:** Criptografia local via **DPAPI** do usuário atual (`%LocalAppData%/VOLT/passwords.dpapi`).
  - **Linux:** Armazenamento seguro via **Secret Service API** (GNOME Keyring / KWallet). Sem fallbacks desprotegidos em texto plano.
- **Importação de Senhas via CSV:** Compatível com exportações de Chrome, Edge, Firefox, Opera e Bitwarden. Preenchimento conferido por protocolo, domínio e porta, sem envio automático de formulários.
- **Sugestões de Endereço 100% Locais:** Auto-completar baseado exclusivamente no histórico recente local (últimos 200 sites visitados) e favoritos. Nenhuma tecla digitada é enviada para serviços de terceiros.
- **Favicons Locais:** Ícones 16×16 extraídos diretamente do motor da página, sem consulta a APIs externas.

---

## 📁 Estrutura do Projeto

O VOLT é estruturado de forma modular e independente entre plataformas:

```text
volt-browser/
├── Assets/                 # Ícones, vetores e identidades visuais (Volt, WhatsApp)
├── Engine/                 # Políticas de recursos, ciclo de vida e gerenciador de atualizações
├── Installer/              # Script de compilação e instalador NSIS (Windows)
├── Media/                  # Controle de reprodução em segundo plano e mídia
├── Navigation/             # Histórico, catálogo de sites, sugestões e favoritos
├── Security/               # Cofre de senhas local (DPAPI) e regras de preenchimento
├── Sidebar/                # Painel lateral expansível com WhatsApp Web
├── Tabs/                   # Gestão de abas: hibernação, layout, seleção e arraste
├── UI/                     # Menus modernos, temas, controles de janela e Win32 Chrome
├── tests/                  # Suíte de testes automatizados de política e geometria
├── linux/                  # Implementação Linux (Python, PySide6 / QtWebEngine)
├── Criar-Portable.ps1      # Gerador de distribuição portátil para Windows
├── VOLT.csproj             # Projeto principal C# / .NET 10 (Windows)
└── README.md
```

---

## 🛠️ Como Compilar e Executar

### 🪟 Windows (.NET 10 + WebView2)

#### Pré-requisitos
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) (já incluso no Windows 10/11)

#### Executar em modo de desenvolvimento
```powershell
dotnet run --project VOLT.csproj
```

#### Executar os testes automatizados
```powershell
dotnet run --project tests/PolicyChecks.csproj -c Release
```

#### Gerar Versão Portátil (Standalone)
Gera uma pasta autocontida pronta para uso em `dist/VOLT-Portable/`:
```powershell
.\Criar-Portable.ps1
# ou execute:
.\GERAR-PORTABLE.cmd
```

#### Gerar Instalador (.exe via NSIS)
Requer [NSIS 3.11+](https://nsis.sourceforge.io/):
```powershell
powershell -ExecutionPolicy Bypass -File Installer/build.ps1
```

---

### 🐧 Linux (Python 3.10+ & QtWebEngine)

#### Início Rápido
O script inicializador configura automaticamente o ambiente virtual (`.venv`) e as dependências na primeira execução:
```bash
chmod +x linux/INICIAR-VOLT.sh
./linux/INICIAR-VOLT.sh
```

#### Instalação Manual
```bash
python3 -m venv linux/.venv
linux/.venv/bin/pip install -r linux/codigo-fonte/requirements.txt
linux/.venv/bin/python linux/codigo-fonte/volt.py
```

#### Testes no Linux
```bash
# Testes unitários de política:
python3 -m unittest discover -s linux/codigo-fonte/tests -p 'test_*.py'

# Smoke test integrado (offscreen / sem display):
QT_QPA_PLATFORM=offscreen linux/.venv/bin/python linux/codigo-fonte/tests/smoke.py
```

---

## 💾 Localização dos Dados do Usuário

Os dados de perfil, histórico local, favoritos e o cofre de senhas ficam isolados da pasta de instalação:

| Plataforma | Caminho dos Dados |
| :--- | :--- |
| **Windows** | `%LocalAppData%\VOLT\` (Configurações em `browsing.json`, cofre em `passwords.dpapi`) |
| **Linux** | `~/.local/share/VOLT/` ou diretório correspondente da sessão XDG |

A desinstalação ou atualização do navegador preserva o diretório de dados do usuário.

---

## 🧪 Validação e Qualidade

- **Windows:** Mais de 370 verificações automatizadas de integridade de layout, restauração da barra de tarefas, mitigação de flickering e políticas de consumo de memória. Detalhes completos em [VALIDACAO.md](VALIDACAO.md).
- **Linux:** Suíte de testes com verificações integradas e cobertura de renderização dos temas. Detalhes em [linux/VALIDACAO.md](linux/VALIDACAO.md).

---

## 📄 Licenças & Terceiros

- Componentes e bibliotecas de terceiros preservam suas respectivas licenças.
- Avisos de licença das dependências Qt/PySide6 estão disponíveis em [linux/LICENCAS/](linux/LICENCAS/).

## Estabilidade visual — Windows 0.6.8

- A área de páginas usa um contêiner gerenciado. Ao reordenar abas, os controles e os motores das páginas ficam ligados ao mesmo pai; só a ordem muda.
- Foram removidos a captura da tela, a janela de cobertura e o bloqueio global de redesenho usados nas transições. Temas, seleção, abertura/fechamento e painel lateral compõem a interface real.
- A tela inicial desenha fundo, marca e subtítulo em uma única superfície opaca com buffer. Campo de busca e atalhos continuam sendo controles interativos.
- A faixa de abas mantém o movimento breve dos elementos. O painel do WhatsApp desliza por 150 ms sem redimensionar a página por baixo; inverter a ação parte da posição atual. As animações respeitam a preferência do Windows.
- Validação nativa: [Windows visual regression](https://github.com/ithiagops/volt-browser/actions/runs/44316932698), com 279 verificações incluindo pixels reais de início e WebView2 local após ciclos de restauração. Mais 393 verificações de política passaram. Isso não substitui testes com outras GPUs, dois monitores e escalas diferentes.
- A versão Linux não mudou nesta correção específica do renderizador Windows.
