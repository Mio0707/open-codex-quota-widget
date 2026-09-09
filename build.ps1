$ErrorActionPreference = "Stop"

$workspaceRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$bundledDotnet = Join-Path $workspaceRoot "tools\dotnet-sdk8\dotnet.exe"
$dotnetCommand = if (Test-Path -LiteralPath $bundledDotnet) {
    $bundledDotnet
} else {
    (Get-Command dotnet -ErrorAction Stop).Source
}

& $dotnetCommand publish (Join-Path $workspaceRoot "app\Codex额度悬浮窗.csproj") `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o (Join-Path $workspaceRoot "dist")
