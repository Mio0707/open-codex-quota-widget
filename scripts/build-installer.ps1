$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$installerScript = Join-Path $repoRoot 'installer\OpenCodexQuotaWidget.iss'
$appExecutable = Join-Path $repoRoot 'assets\app\CodexQuotaWidget.exe'

if (-not (Test-Path -LiteralPath $appExecutable)) {
    throw 'Missing assets\app\CodexQuotaWidget.exe.'
}

$iscc = Get-Command ISCC.exe -ErrorAction SilentlyContinue
if (-not $iscc) {
    throw 'Inno Setup was not found. Install Inno Setup and try again.'
}

& $iscc.Source $installerScript
