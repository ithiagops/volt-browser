# VOLT Windows 0.6.7 — experimental

Correção da restauração pela barra de tarefas: layout da tela inicial preservado enquanto minimizada, sincronização da geometria nativa ao voltar e um redesenho completo coalescido após a ativação/restauração. Callback antigo é invalidado se a janela minimizar ou fechar. Mantém as páginas existentes e a visibilidade das abas de fundo, sem recarregá-las.

Binário novo: `VOLT-0.6.7-Setup.exe`. Linux não foi alterado: último AppImage é `VOLT-0.6.6-x86_64.AppImage`.

Compilação/publicação win-x64 e testes de política passaram em Linux. Minimizar/restaurar, Snap, DPI e composição nativa precisam ser verificados em Windows real.
