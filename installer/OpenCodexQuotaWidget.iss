#define AppName "Codex 额度悬浮窗"
#define AppVersion "1.1.1"
#define AppExeName "CodexQuotaWidget.exe"

[Setup]
AppId={{B1E74B73-343A-4F52-8CC7-3068AA50F2B0}
AppName={#AppName}
AppVersion={#AppVersion}
DefaultDirName={localappdata}\Programs\Open Codex Quota Widget
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\installer-output
OutputBaseFilename=CodexQuotaWidget-Setup-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
UninstallDisplayIcon={app}\{#AppExeName}

[InstallDelete]
Type: files; Name: "{app}\OpenCodexQuotaWidget.exe"
Type: filesandordirs; Name: "{localappdata}\Temp\.net\CodexQuotaWidget"

[Files]
Source: "..\assets\app\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "快捷方式："; Flags: checkedonce

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "立即打开 Codex 额度悬浮窗"; Flags: nowait postinstall skipifsilent
