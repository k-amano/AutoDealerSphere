# ビルド・配布手順

## 構成

| コンポーネント | 種別 | 役割 |
|---|---|---|
| AutoDealerSphere.Server.exe | Windowsサービス | ASP.NET Core Webサーバー（http://localhost:5259） |
| AutoDealerSphere.exe | WinFormsアプリ | ランチャー（WebView2でサーバーに接続） |

---

## インストーラーが作成するもの（インストール後の全体像）

`AutoDealerSphere_Setup.exe`（Inno Setup製、要管理者権限）を実行すると、以下が作成される。

### ファイル・フォルダ

| パス | 内容 | 作成タイミング |
|---|---|---|
| `C:\AutoDealerSphere\Server\` | サーバー本体（AutoDealerSphere.Server.exe、Blazor WASMクライアント、appsettings.json 等） | インストール時 |
| `C:\AutoDealerSphere\Launcher\` | ランチャー本体（AutoDealerSphere.exe） | インストール時 |
| `C:\AutoDealerSphere\unins000.exe` | アンインストーラー | インストール時 |
| `C:\ProgramData\AutoDealerSphere\` | **データフォルダ**（全アカウント書き込み可） | インストール時（空フォルダ） |
| `C:\ProgramData\AutoDealerSphere\crm01.db` | **SQLiteデータベース（全業務データはここ）** | サービス初回起動時に自動生成 |
| `C:\ProgramData\AutoDealerSphere\logs\app-YYYYMMDD.log` | サーバーログ（Serilog、日次ローテーション、30日保持） | サービス起動時から |
| `C:\ProgramData\AutoDealerSphere\keys\` | データ保護キー（メール設定のSMTPパスワード暗号化に使用）。**削除するとメール設定のパスワードが復号不能になり再登録が必要** | サービス起動時に自動生成 |
| デスクトップ / スタートメニュー | 「AutoDealerSphere」ショートカット（ランチャーへのリンク） | インストール時 |

### システムへの登録

| 登録先 | 内容 |
|---|---|
| Windowsサービス | サービス名 `AutoDealerSphere`（自動起動、`C:\AutoDealerSphere\Server\AutoDealerSphere.Server.exe`）。インストーラーが `sc create` で登録し、即時開始する |
| ランタイム | WebView2ランタイム（未インストールの場合のみ同梱版をサイレントインストール）。**.NETランタイムのインストールは不要**（サーバー・ランチャーとも自己完結型発行でランタイム同梱済み） |

> **注意**: データベースはインストール先（`C:\AutoDealerSphere`）ではなく `C:\ProgramData\AutoDealerSphere` に作られる。プログラムとデータが分離されているため、プログラムを消してもデータは残る（後述「再インストールで消えるもの」参照）。
>
> 開発時（Visual Studioデバッグ、ASPNETCORE_ENVIRONMENT=Development）のみ、DBは `Server\Data\crm01.db`、ログは exe と同じ場所の `logs\` を使う。本番サービスとはデータが別物なので混同しないこと。

---

## 初回セットアップ（初期パスワードについて）

**初期パスワードは存在しない。** 管理者アカウントはインストール後の初回アクセス時に自分で登録する方式。

1. インストール完了後、デスクトップの「AutoDealerSphere」を起動する
2. 管理者ユーザーが未登録の場合、自動的に**「初回セットアップ」画面**（`/setup`）へリダイレクトされる
3. 名前・メールアドレス・パスワード（6文字以上）を入力し「管理者を登録」を押す
4. 続けて**発行者情報（自社情報）の登録画面**へ誘導されるので入力する
5. 以降はここで登録したメールアドレス／パスワードでログインする

ログイン画面が表示される（セットアップ画面が出ない）場合は、既に管理者が登録済み。パスワードを忘れた場合は、ログイン画面の「パスワードを忘れた方」からリセットする（メール設定が必要）。メール設定が未登録・使用不能でリセットメールを送れない場合は、下記「トラブルシューティング > 管理者パスワードを忘れた場合」の復旧コマンドを使う（データは消えない）。

---

## 初期化がどこまで進んでいるかの確認方法

初期化は「①インストーラーによる配置 → ②サービス起動時のDB自動生成 → ③画面からの初回セットアップ」の3段階。どこまで進んだかは以下で確認できる。

### ① ファイル配置とサービス登録の確認

```
C:\AutoDealerSphere\Server\AutoDealerSphere.Server.exe があるか
sc.exe query AutoDealerSphere   ← STATE が RUNNING なら起動済み
```

（`services.msc` で「AutoDealerSphere」が「実行中」でも同じ）

### ② データベース初期化の確認

サービスが一度でも正常起動していれば、以下が自動生成されている。

```
C:\ProgramData\AutoDealerSphere\crm01.db          ← DB本体（テーブル作成＋初期マスタ投入済み）
C:\ProgramData\AutoDealerSphere\logs\app-*.log    ← サーバーログ
```

- **crm01.db が無い** → サービスがまだ一度も正常起動していない。「トラブルシューティング」参照
- **ログの見方**: 最新の `app-YYYYMMDD.log` を開き、`[ERR]` / `[FTL]` 行が無ければ正常。「データベースの初期化に失敗しました」があればDB初期化で失敗している
- DB初期化では全テーブル（Users, Clients, Vehicles, Invoices 等）の作成と、初期マスタデータ（車両カテゴリ5件、法定費用、部品マスタ約50件）の投入まで自動で行われる。**ユーザーは1件も作られない**（③で登録する）

### ③ 初回セットアップ（管理者登録）まで完了しているかの確認

- ブラウザまたはランチャーで `http://localhost:5259` を開く
  - **「初回セットアップ」画面が出る** → 管理者未登録（③が未完了）
  - **発行者情報の登録画面が出る** → 管理者は登録済み、発行者情報が未登録
  - **ログイン画面が出る** → 初期化はすべて完了済み
- コマンドで確認する場合: `curl http://localhost:5259/api/User/has-admin` が `true` なら管理者登録済み

---

## 発行手順

ソリューションルート（`AutoDealerSphere.sln` があるフォルダ）で実行する:

```powershell
cd C:\Users\Administrator\VisualStudioProjects\blazor\AutoDealerSphere
Remove-Item -Recurse -Force publish\Server, publish\Launcher -ErrorAction SilentlyContinue
dotnet publish Server -c Release -r win-x64 --self-contained true -o publish\Server
dotnet publish Launcher -c Release -r win-x64 --self-contained true -o publish\Launcher
```

発行結果は以下に出力される:
- `publish\Server\` — サーバー一式（.NET / ASP.NET Core ランタイム同梱）
- `publish\Launcher\` — ランチャー一式（.NET デスクトップランタイム同梱）

> **重要（`-r win-x64 --self-contained true` を省略しないこと）**:
> - 開発機は Windows on ARM（ARM64）のため、ランタイム指定なしで発行すると **ARM64 の EXE** が生成される。これを配布すると、一般的な Intel/AMD の PC では「このアプリはお使いの PC では実行できません」と表示され起動できない。
> - 自己完結型（`--self-contained true`）にすることで、配布先 PC への .NET ランタイムのインストールが不要になる（インストーラーにもランタイムを同梱していない）。
> - x64 の EXE は Windows 11 の ARM 機でもエミュレーションで動作する。
> - 発行前に `publish\` 配下を削除するのは、古い発行結果（別アーキテクチャの EXE 等）が混在したままインストーラーに取り込まれるのを防ぐため。

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

インストーラー自体が既存サービスの停止・削除・再登録を行うため、そのまま `AutoDealerSphere_Setup.exe` を再実行すればよい。

サービスが応答しない等で失敗する場合は、先に手動で停止・削除してから実行する:

**コマンドプロンプト（管理者）で実行:**
```
sc.exe stop AutoDealerSphere
sc.exe delete AutoDealerSphere
```

### 再インストールで消えるもの・残るもの

| 対象 | 再インストール | アンインストール |
|---|---|---|
| `C:\AutoDealerSphere\`（プログラム本体） | 上書きされる | 削除される |
| Windowsサービス登録 | 削除→再登録される | 削除される |
| `C:\ProgramData\AutoDealerSphere\crm01.db`（**データベース**） | **残る（消えない）** | **残る（消えない）** |
| `C:\ProgramData\AutoDealerSphere\logs\`（ログ） | 残る | 残る |
| `C:\ProgramData\AutoDealerSphere\keys\`（暗号化キー） | 残る | 残る |
| デスクトップ／スタートメニューのショートカット | 再作成される | 削除される |

つまり、**再インストールしても登録済みの管理者アカウント・顧客・車両・請求書などの業務データはすべて残り、初回セットアップ画面には戻らない。**

完全に初期状態（初回セットアップから）に戻したい場合は、以下を行う:

```
sc.exe stop AutoDealerSphere
sc.exe query AutoDealerSphere        ← STATE が「1 STOPPED」になるまで数秒待って確認する
del C:\ProgramData\AutoDealerSphere\crm01.db
sc.exe start AutoDealerSphere
```

> **注意1（停止完了を待つ）**: `sc.exe stop` は停止「要求」を出すだけで完了を待たない（直後は STOP_PENDING）。停止完了前に del を実行すると「別のプロセスで使用されているため」エラーになる。PowerShell なら `Stop-Service AutoDealerSphere` が停止完了まで待つので確認不要。
>
> **注意2（PowerShellの落とし穴）**: PowerShell では `sc` は `Set-Content` の別名のため、`sc stop ...` はサービスを停止せず「stop」という名前のファイルを作ってしまう。**必ず `sc.exe` と拡張子まで書くこと**。

※ crm01.db の削除は**全業務データの完全消去**。必要ならファイルコピーでバックアップを取ってから行うこと。

---

## 動作確認

インストール後、以下を確認する:

1. `services.msc` を開き「AutoDealerSphere」が「実行中」になっていること
2. デスクトップの「AutoDealerSphere」アイコンをダブルクリックして画面が表示されること
3. 初回は「初回セットアップ」画面が表示されるので管理者を登録すること（前述）

---

## トラブルシューティング

### サービスが起動しない場合

確認順序:

1. サーバーログを確認する: `C:\ProgramData\AutoDealerSphere\logs\app-YYYYMMDD.log` の `[ERR]` / `[FTL]` 行
2. イベントビューアー → Windowsログ → システム で「AutoDealerSphere」のエラーを確認する
3. PowerShell（管理者）で直接実行してコンソールにエラーを出す:

```powershell
cd C:\AutoDealerSphere\Server
.\AutoDealerSphere.Server.exe
```

※ ポート5259が他プロセスに使用されていると起動に失敗する。`netstat -ano | findstr :5259` で確認。

### 「このアプリはお使いの PC では実行できません」と表示される場合

EXE のアーキテクチャが PC と合っていない（ARM64 ビルドを x64 の PC に配布した等）。開発機が ARM64 のため、`-r win-x64` を付けずに発行するとこの状態になる。「発行手順」のとおり `-r win-x64 --self-contained true` を付けて発行し直し、インストーラーを作り直すこと。

### 管理者パスワードを忘れた場合（リセットメールも送れない場合）

「パスワードを忘れた方」はメール設定が正しく登録されていないと使えない。ログインできず設定画面にも入れない場合は、コマンドプロンプト（管理者）で以下を実行してパスワードを直接リセットする（**データは消えない**）:

```
cd C:\AutoDealerSphere\Server
AutoDealerSphere.Server.exe --reset-password <メールアドレス> <新パスワード>
```

例: `AutoDealerSphere.Server.exe --reset-password admin@example.com NewPass123`

- 新パスワードは6文字以上
- サービスの停止は不要。実行後すぐ新パスワードでログインできる
- ログイン後、必要ならユーザー編集画面でパスワードを変更し、メール設定（SMTPパスワード）も再登録しておくこと

### ランチャーが画面を表示しない場合

サービスが起動していない可能性が高い。上記のサービス確認を先に行う。
ブラウザで `http://localhost:5259` を直接開き、表示されるならランチャー（WebView2）側の問題、表示されないならサービス側の問題と切り分けられる。

---

## ポート設定

| 用途 | HTTP | HTTPS |
|------|------|-------|
| 本番（Windowsサービス） | 5259 | - |
| 開発（Visual Studioデバッグ） | 5260 | 7188 |

開発ポートと本番サービスポートを分けることで、本番サービスを停止せずに Visual Studio でデバッグできる。

- 本番ポートは `Server/appsettings.json` および `Launcher/MainForm.cs` の `AppUrl` で定義
- 開発ポートは `Server/Properties/launchSettings.json` および `Client/Properties/launchSettings.json` で定義
