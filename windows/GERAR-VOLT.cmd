@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo Instale o SDK .NET 10 x64 em https://dotnet.microsoft.com/download/dotnet/10.0
  echo Depois abra este arquivo novamente.
  pause
  exit /b 1
)
echo Gerando o VOLT para Windows x64. A primeira vez precisa de internet.
dotnet publish VOLT.csproj -c Release -r win-x64 --self-contained true -o dist\VOLT
if errorlevel 1 (
  echo Falha na compilacao. Copie a mensagem acima para investigarmos.
  pause
  exit /b 1
)
echo Pronto. Abra dist\VOLT\VOLT.exe. Mantenha todos os arquivos da pasta juntos.
explorer "dist\VOLT"
pause
