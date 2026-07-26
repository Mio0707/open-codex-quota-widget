$ErrorActionPreference = 'Stop'

if (-not $IsWindows -and $PSVersionTable.PSEdition -eq 'Core') {
    throw 'This Codex quota widget is available only on Windows.'
}

$skillRoot = Split-Path -Parent $PSScriptRoot
$widgetPath = Join-Path $skillRoot 'assets\app\CodexQuotaWidget.exe'

if (-not (Test-Path -LiteralPath $widgetPath)) {
    throw "Widget executable not found: $widgetPath"
}

$running = Get-Process -Name 'CodexQuotaWidget' -ErrorAction SilentlyContinue
if ($running) {
    Write-Output 'Codex quota widget is already running.'
    exit 0
}

Start-Process -FilePath $widgetPath -WindowStyle Normal
Write-Output 'Codex quota widget opened.'
