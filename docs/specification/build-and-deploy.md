# ビルド・配布手順

## 構成

| コンポーネント | 種別 | 役割 |
|---|---|---|
| AutoDealerSphere.Server.exe | Windowsサービス | ASP.NET Core Webサーバー（http://localhost:5259） |
| AutoDealerSphere.exe | WinFormsアプリ | ランチャー（WebView2でサーバーに接続） |

インストール先: `C:\Program Files (x86)\AutoDealerSphere\`

---

## 発行手順

### 1. Server を発行

Visual Studio でServerプロジェクトを右クリック → 「発行」→ 発行先: `publish/Server/`

または PowerShell:
```
cd Server
dotnet publish -c Release -o ..\publish\Server
```

### 2. Launcher を発行

Visual Studio でLauncherプロジェクトを右クリック → 「発行」→ 発行先: `publish/Launcher/`

または PowerShell:
```
cd Launcher
dotnet publish -c Release -o ..\publish\Launcher
```

---

## インストーラー作成手順

### 前提
- Inno Setup がインストールされていること

### 手順

1. Inno Setup Compiler を開く
2. `installer/setup.iss` を開く
3. Build → Compile（F9）
4. `installer_output/AutoDealerSphere_Setup.exe` が生成される

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
