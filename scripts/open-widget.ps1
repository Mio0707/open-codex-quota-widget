$ErrorActionPreference = 'Stop'

$widgetPath = Join-Path $env:LOCALAPPDATA 'Programs\Open Codex Quota Widget\OpenCodexQuotaWidget.exe'
if (-not (Test-Path -LiteralPath $widgetPath)) {
    throw '未找到已安装的额度浮窗。请从仓库 Releases 下载并安装 OpenCodexQuotaWidget-Setup-x64.exe。'
}

if (Get-Process -Name 'OpenCodexQuotaWidget' -ErrorAction SilentlyContinue) {
    Write-Output 'Codex quota widget is already running.'
    exit 0
}

Start-Process -FilePath $widgetPath -WorkingDirectory (Split-Path -Parent $widgetPath)
Write-Output 'Codex quota widget opened.'

