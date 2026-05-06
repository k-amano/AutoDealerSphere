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

[Dirs]
; データフォルダを作成
Name: "{app}\Server\Data"

[Icons]
; デスクトップにショートカット
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\Launcher\{#LauncherExe}"
; スタートメニュー
Name: "{group}\{#AppName}"; Filename: "{app}\Launcher\{#LauncherExe}"
Name: "{group}\{#AppName}のアンインストール"; Filename: "{uninstallexe}"

[Run]
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
