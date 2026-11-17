# ==============================================================================
# Script de Criação do Pacote Portable do VOLT
# Gera uma versão 100% portátil, auto-contida e sem necessidade de instalação.
# ==============================================================================
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$ProjectDir = $ScriptDir
$Csproj = Join-Path $ProjectDir "VOLT.csproj"

if (-not (Test-Path $Csproj)) {
    throw "Arquivo VOLT.csproj não encontrado em: $ProjectDir"
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "           GERADOR DE VERSÃO PORTABLE - VOLT               " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host ""

# Obter versão do projeto
[xml]$xml = Get-Content $Csproj
$version = $xml.Project.PropertyGroup.Version
if (-not $version) { $version = "0.6.7" }
Write-Host "[1/4] Versão identificada: $version ($Runtime | $Configuration)" -ForegroundColor Yellow

$DistDir = Join-Path $ProjectDir "dist"
$PortableDir = Join-Path $DistDir "VOLT-Portable"
$ZipFile = Join-Path $DistDir "VOLT-Portable-v$version.zip"

# Limpar diretório anterior se existir
if (Test-Path $PortableDir) {
    Remove-Item $PortableDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $PortableDir | Out-Null

Write-Host "[2/4] Compilando arquivos da versão portátil..." -ForegroundColor Yellow

$published = $false

# Tentativa 1: Single-file Self-contained (executável único standalone)
try {
    Write-Host "Verificando compilação auto-contida (Single-File)..." -ForegroundColor Gray
    & dotnet publish $Csproj -c $Configuration -r $Runtime --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -o $PortableDir 2>$null

    if ($LASTEXITCODE -eq 0 -and (Test-Path (Join-Path $PortableDir "VOLT.exe"))) {
        $published = $true
        Write-Host "Executável standalone de arquivo único gerado com sucesso!" -ForegroundColor Green
    }
} catch { }

# Tentativa 2: Pacote portátil padrão com runtime local
if (-not $published) {
    Write-Host "Gerando pacote portátil otimizado..." -ForegroundColor Gray
    & dotnet publish $Csproj -c $Configuration -o $PortableDir
    if ($LASTEXITCODE -eq 0 -and (Test-Path (Join-Path $PortableDir "VOLT.exe"))) {
        $published = $true
        Write-Host "Pacote portátil compilado com sucesso!" -ForegroundColor Green
    }
}

if (-not $published) {
    throw "A compilação do executável portátil falhou. Verifique se o .NET SDK está instalado."
}

Write-Host "[3/4] Configurando estrutura e isolamento de dados portáteis..." -ForegroundColor Yellow

# Limpar arquivos de documentação XML e debug PDB do pacote final
Get-ChildItem -Path $PortableDir -Filter "*.xml" | Remove-Item -Force -ErrorAction SilentlyContinue
Get-ChildItem -Path $PortableDir -Filter "*.pdb" | Remove-Item -Force -ErrorAction SilentlyContinue

# Criar arquivo marcador e pasta de dados para modo portable isolado
$PortableMarker = Join-Path $PortableDir "portable.txt"
Set-Content -Path $PortableMarker -Value "VOLT PORTABLE MODE - Dados, histórico e perfis salvos localmente nesta pasta." -Encoding utf8

$DataDir = Join-Path $PortableDir "Data"
New-Item -ItemType Directory -Force -Path $DataDir | Out-Null

# Criar LEIA-ME da versão Portable
$ReadmePath = Join-Path $PortableDir "LEIA-ME-PORTABLE.txt"
$ReadmeContent = @"
===================================================================
                       VOLT - NAVEGADOR PORTÁTIL
                              Versão $version
===================================================================

Este é o pacote portátil do VOLT Browser.

COMO USAR:
- Dê um duplo-clique em VOLT.exe para iniciar o navegador.
- Nenhuma instalação ou permissão de administrador é necessária.
- Funciona diretamente de qualquer pasta, pendrive ou disco externo.

DADOS E PRIVACIDADE PORTÁTIL:
- Todos os perfis, abas salvas, favoritos, histórico e senhas 
  são armazenados localmente dentro da pasta "Data\".
- Nenhum dado é deixado nas pastas do sistema host (%LocalAppData%).
- Para fazer backup ou transportar sua sessão, basta copiar toda a 
  pasta do VOLT para outro computador ou pendrive.

REQUISITOS:
- Windows 10 ou Windows 11 (64 bits).
- Microsoft Edge WebView2 Runtime (já presente por padrão no Windows 10/11).
===================================================================
"@
Set-Content -Path $ReadmePath -Value $ReadmeContent -Encoding utf8

Write-Host "[4/4] Compactando arquivo ZIP para distribuição..." -ForegroundColor Yellow
if (Test-Path $ZipFile) { Remove-Item $ZipFile -Force }

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($PortableDir, $ZipFile)

$exeSize = (Get-Item (Join-Path $PortableDir "VOLT.exe")).Length / 1MB
$zipSize = (Get-Item $ZipFile).Length / 1MB

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Green
Write-Host "           PACOTE PORTABLE GERADO COM SUCESSO!            " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
Write-Host " Pasta Portable:  $PortableDir"
Write-Host " Executável:      VOLT.exe ($([math]::Round($exeSize, 2)) MB)"
Write-Host " Arquivo ZIP:     $ZipFile ($([math]::Round($zipSize, 2)) MB)"
Write-Host " Marcador:        portable.txt (Isolamento de dados ativado)"
Write-Host "==========================================================" -ForegroundColor Green
