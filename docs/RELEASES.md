# VOLT Windows 0.6.8

Correção da composição visual e polimento de transições:
- Páginas em contêiner gerenciado, sem faixa nativa de TabControl escondida.
- Reordenação sem desanexar páginas nem recriar WebView2.
- Removidas capturas da tela, janela de cobertura e bloqueio de redesenho nas transições.
- Tela inicial com desenho opaco em uma superfície; painel WhatsApp com deslizamento curto e reversível, sem alterar o tamanho da página.
- Preservados menu, modo portátil, atualizador, senhas, áudio e regras de memória.

Arquivo: `VOLT-0.6.8-Setup.exe`. Linux não mudou; o último AppImage continua 0.6.6.

Validação: 393 verificações de política e 279 verificações nativas Windows passaram no [GitHub Actions](https://github.com/ithiagops/volt-browser/actions/runs/44316932698). Capturas reais inspecionadas. Outras GPUs, monitores e DPI precisam de validação no computador do usuário.
