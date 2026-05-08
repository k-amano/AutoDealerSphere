#define AppName "AutoDealerSphere"
#define AppVersion "1.0.0"
#define AppPublisher "Your Company"
#define AppInstallDir "C:\AutoDealerSphere"
#define ServiceName "AutoDealerSphere"
#define ServiceExe "AutoDealerSphere.Server.exe"
#define LauncherExe "AutoDealerSphere.exe"

[Setup]
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={#AppInstallDir}
DefaultGroupName={#AppName}
OutputDir=..\installer_output
OutputBaseFilename=AutoDealerSphere_Setup
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\Launcher\{#LauncherExe}

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Files]
Source: "..\publish\Server\*"; DestDir: "{app}\Server"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\publish\Launcher\*"; DestDir: "{app}\Launcher"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\installer\dotnet-runtime\windowsdesktop-runtime-9.0.15-win-x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: IsX64Compatible
Source: "..\installer\dotnet-runtime\windowsdesktop-runtime-9.0.15-win-arm64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: IsArm64
Source: "..\installer\webview2\MicrosoftEdgeWebView2RuntimeInstallerX64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: IsX64Compatible
Source: "..\installer\webview2\MicrosoftEdgeWebView2RuntimeInstallerARM64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: IsArm64

[Dirs]
; データフォルダを作成（ProgramData は全アカウントが書き込み可能）
Name: "{commonappdata}\AutoDealerSphere"

[Icons]
; デスクトップにショートカット
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\Launcher\{#LauncherExe}"
; スタートメニュー
Name: "{group}\{#AppName}"; Filename: "{app}\Launcher\{#LauncherExe}"
Name: "{group}\{#AppName}のアンインストール"; Filename: "{uninstallexe}"

[Run]
; .NET 9 デスクトップランタイム（未インストールの場合のみインストール）
Filename: "{tmp}\windowsdesktop-runtime-9.0.15-win-x64.exe"; Parameters: "/silent /norestart"; Flags: runhidden waituntilterminated; Check: IsX64Compatible
Filename: "{tmp}\windowsdesktop-runtime-9.0.15-win-arm64.exe"; Parameters: "/silent /norestart"; Flags: runhidden waituntilterminated; Check: IsArm64
; WebView2 ランタイム（未インストールの場合のみインストール）
Filename: "{tmp}\MicrosoftEdgeWebView2RuntimeInstallerX64.exe"; Parameters: "/silent /install"; Flags: runhidden waituntilterminated; Check: IsX64Compatible
Filename: "{tmp}\MicrosoftEdgeWebView2RuntimeInstallerARM64.exe"; Parameters: "/silent /install"; Flags: runhidden waituntilterminated; Check: IsArm64
; 既存サービスを停止・削除（再インストール時の残骸対策）
Filename: "sc.exe"; Parameters: "stop ""{#ServiceName}"""; Flags: runhidden waituntilterminated
Filename: "sc.exe"; Parameters: "delete ""{#ServiceName}"""; Flags: runhidden waituntilterminated
; サービスを登録して開始
Filename: "sc.exe"; Parameters: "create ""{#ServiceName}"" binPath= ""{app}\Server\{#ServiceExe}"" start= auto DisplayName= ""{#AppName}"""; Flags: runhidden waituntilterminated
Filename: "sc.exe"; Parameters: "start ""{#ServiceName}"""; Flags: runhidden waituntilterminated
; インストール完了後にアプリを起動
Filename: "{app}\Launcher\{#LauncherExe}"; Flags: postinstall nowait skipifsilent

[UninstallRun]
; アンインストール時にサービスを停止・削除
Filename: "sc.exe"; Parameters: "stop ""{#ServiceName}"""; Flags: runhidden waituntilterminated
Filename: "sc.exe"; Parameters: "delete ""{#ServiceName}"""; Flags: runhidden waituntilterminated

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
