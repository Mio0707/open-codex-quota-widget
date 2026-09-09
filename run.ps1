$ErrorActionPreference = "Stop"

$workspaceRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$executable = Join-Path $workspaceRoot "dist\Codex额度悬浮窗.exe"
if (-not (Test-Path -LiteralPath $executable)) {
    throw "尚未找到成品程序，请先运行 build.ps1。"
}

Start-Process -FilePath $executable -ArgumentList "--skin=plankton-cat"
