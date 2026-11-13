$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$Payload = Join-Path $ProjectRoot 'dist/VOLT-0.6.5'
$OutputDirectory = Join-Path $ProjectRoot 'dist'
$Output = Join-Path $OutputDirectory 'VOLT-0.6.5-Setup.exe'
$Bootstrapper = Join-Path $PSScriptRoot 'MicrosoftEdgeWebview2Setup.exe'
$Compiler = (Get-Command makensis.exe -ErrorAction SilentlyContinue).Source
if (-not $Compiler) {
    $Compiler = Join-Path ${env:ProgramFiles(x86)} 'NSIS/makensis.exe'
}
if (-not (Test-Path $Compiler)) { throw 'Instale NSIS e adicione makensis.exe ao PATH.' }
& dotnet publish (Join-Path $ProjectRoot 'VOLT.csproj') -c Release -r win-x64 --self-contained true -o $Payload
if ($LASTEXITCODE -ne 0) { throw 'A compilação do VOLT falhou.' }
Invoke-WebRequest -Uri 'https://go.microsoft.com/fwlink/p/?LinkId=2124703' -OutFile $Bootstrapper
Push-Location $PSScriptRoot
try {
    & $Compiler "/DPAYLOAD=$Payload" "/DOUTPUT=$Output" (Join-Path $PSScriptRoot 'VOLT.nsi')
    if ($LASTEXITCODE -ne 0) { throw 'A geração do instalador falhou.' }
} finally { Pop-Location }
Write-Host "Instalador gerado: $Output"
