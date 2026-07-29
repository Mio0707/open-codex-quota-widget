param([string]$Configuration = 'Release')

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'src\OpenCodexQuotaWidget\OpenCodexQuotaWidget.csproj'
$publishDirectory = Join-Path $repoRoot 'publish\win-x64'
$installerScript = Join-Path $repoRoot 'installer\OpenCodexQuotaWidget.iss'

dotnet publish $project -c $Configuration -r win-x64 --self-contained true -p:PublishSingleFile=true -o $publishDirectory
$iscc = Get-Command ISCC.exe -ErrorAction SilentlyContinue
if (-not $iscc) { throw '未找到 Inno Setup。请安装 Inno Setup 后重新运行。' }
& $iscc.Source $installerScript

