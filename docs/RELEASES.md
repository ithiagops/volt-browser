# VOLT 0.6.4 — experimental

Tag sugerida: `v0.6.4`, baseada no commit atual de `main`.

Novidades Windows e Linux: prazo de hibernação configurável (padrão 5 minutos), prioridade das primeiras N abas, sugestões locais de endereços e favicons de 16 × 16. Inclui área clicável do botão + até o topo. Limpeza por RAM continua independente do prazo e respeita a prioridade.

Arquivos de distribuição devem ser gerados desta revisão:
- `VOLT-0.6.4-Setup.exe`: instalador Windows x64.
- `VOLT-0.6.4-x86_64.AppImage`: Linux x86_64, glibc 2.39+.

Não reutilize os binários 0.6.3: eles não contêm estas mudanças. A publicação em Releases é separada do envio do código.

Verificação: compilação Windows sem avisos/erros, verificações C# de política, testes Python e testes integrados offscreen do Linux. Interface e armazenamento nativo do Windows ainda exigem teste em Windows real. Linux precisa de Secret Service disponível/desbloqueado para o cofre de senhas.
