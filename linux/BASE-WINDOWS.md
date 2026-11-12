# Base utilizada

Arquivo original: VOLT-0.6.0-Windows.zip, versão 23 do arquivo de distribuição.
SHA-256: fc1f47d7fcb24d05c99c879e5b9bd8d6bba9d4054b3f28866cf7d58e7927be3e.

Foram lidos Program.cs, ResourcePolicy.cs, HomePage.cs, HoverTabs.cs,
WhatsAppSidebar.cs e os demais componentes da distribuição.
O script MediaGuard.js e a identidade visual oficial foram reaproveitados.
As políticas de seleção, movimento em bloco, fechamento e pressão de memória
foram portadas para Python; WinForms e WebView2 foram substituídos por Qt Widgets
e Qt WebEngine. Este é um port Linux, não uma recompilação do executável Windows.

A solução de renderização evita reordenar/remover os widgets de páginas ao arrastar:
somente a lista de IDs e a pintura da faixa de abas mudam. O fechamento seleciona
o sobrevivente final antes de remover páginas. A página inicial e os temas usam
widgets nativos, sem capturar imagens da tela do usuário.

O pacote Windows original permanece separado e não foi modificado.
