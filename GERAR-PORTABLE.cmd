@echo off
setlocal
cd /d "%~dp0"
title Gerando VOLT Portable...

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Criar-Portable.ps1"

if %ERRORLEVEL% neq 0 (
    echo.
    echo [ERRO] Ocorreu uma falha ao gerar a versao portable.
    pause
    exit /b %ERRORLEVEL%
)

echo.
pause
