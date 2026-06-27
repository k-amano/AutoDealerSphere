# ビルド・配布手順

## 構成

| コンポーネント | 種別 | 役割 |
|---|---|---|
| AutoDealerSphere.Server.exe | Windowsサービス | ASP.NET Core Webサーバー（http://localhost:5259） |
| AutoDealerSphere.exe | WinFormsアプリ | ランチャー（WebView2でサーバーに接続） |

インストール先: `C:\AutoDealerSphere\`

---

## 発行手順

ソリューションルート（`AutoDealerSphere.sln` があるフォルダ）で実行する:

```powershell
cd C:\Users\Administrator\VisualStudioProjects\blazor\AutoDealerSphere
dotnet publish Server -c Release -o publish\Server
dotnet publish Launcher -c Release -o publish\Launcher
```

発行結果は以下に出力される:
- `publish\Server\` — サーバー一式
- `publish\Launcher\` — ランチャー一式

---

## インストーラー作成手順

### 前提
- Inno Setup がインストールされていること

### インストーラースクリプトの場所

```
C:\Users\Administrator\VisualStudioProjects\blazor\AutoDealerSphere\installer\setup.iss
```

インストーラースクリプトは発行先（`publish\Server\`、`publish\Launcher\`）を参照するため、
**発行手順を先に完了してから**インストーラーを作成すること。

### 手順

1. Inno Setup Compiler を開く
2. 上記パスの `setup.iss` を開く
3. Build → Compile（F9）
4. `installer_output\AutoDealerSphere_Setup.exe` が生成される

---

## インストール・再インストール手順

### 初回インストール

`installer_output/AutoDealerSphere_Setup.exe` を管理者として実行する。

### 再インストール（アップデート）

既存のサービスを停止・削除してからインストーラーを実行する。

**コマンドプロンプト（管理者）で実行:**
```
sc stop AutoDealerSphere
sc delete AutoDealerSphere
```

その後、`AutoDealerSphere_Setup.exe` を実行する。

---

## 動作確認

インストール後、以下を確認する:

1. `services.msc` を開き「AutoDealerSphere」が「実行中」になっていること
2. デスクトップの「AutoDealerSphere」アイコンをダブルクリックして画面が表示されること

---

## トラブルシューティング

### サービスが起動しない場合

イベントビューアー → Windowsログ → システム で「AutoDealerSphere」のエラーを確認する。

PowerShellで直接実行してエラーを確認する:
```powershell
cd "C:\Program Files (x86)\AutoDealerSphere"
.\AutoDealerSphere.Server.exe
```

### ランチャーが画面を表示しない場合

サービスが起動していない可能性が高い。上記のサービス確認を先に行う。

---

## ポート設定

| 用途 | HTTP | HTTPS |
|------|------|-------|
| 本番（Windowsサービス） | 5259 | - |
| 開発（Visual Studioデバッグ） | 5260 | 7188 |

開発ポートと本番サービスポートを分けることで、本番サービスを停止せずに Visual Studio でデバッグできる。

- 本番ポートは `Server/appsettings.json` および `Launcher/MainForm.cs` の `AppUrl` で定義
- 開発ポートは `Server/Properties/launchSettings.json` および `Client/Properties/launchSettings.json` で定義
