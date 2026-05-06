# Windowsサービス起動問題 デバッグ全記録

## 問題の発端
インストーラーでインストール後、デスクトップアイコンをクリックしても何も起動しない。

---

## エラーの変遷

### エラー1: サービスタイムアウト（エラー1053）
- 現象: サービスが30秒以内に起動シグナルを返さない
- インストール先: `C:\Program Files (x86)\AutoDealerSphere\`
- Server.exe を直接実行したエラー:
  ```
  System.NotSupportedException: The content root changed from
  "C:\Users\Administrator\..." to "C:\Program Files (x86)\AutoDealerSphere\".
  Changing the host configuration using WebApplicationBuilder.Host is not supported.
  ```
- 原因: `builder.Host.UseContentRoot()` は WebApplicationBuilder では使えない
- 修正: `WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = exeFolder })` に変更

---

### エラー2: hostfxr.dll 読み込み失敗
- 現象:
  ```
  Failed to load the dll from [...\hostfxr.dll], HRESULT: 0x800700C1
  ```
- 原因1: `{autopf}` が 32bit フォルダ（Program Files (x86)）を指していた
  - 修正: `{autopf64}` に変更、`ArchitecturesInstallIn64BitMode=x64compatible` 追加
- 原因2: Server と Launcher のファイルを同じフォルダに混在させていた
  - Launcher（WinForms, self-contained）の hostfxr.dll が Server フォルダに混入
  - 修正: `{app}\Server\` と `{app}\Launcher\` に分離
- 原因3: Server の publish が self-contained になっていた
  - 修正: `FolderProfile.pubxml` に `<SelfContained>false</SelfContained>` を追加
  - Launcher の pubxml にも同様に追加（ただし Launcher は未解決のまま）

---

### エラー3: ポート5259 が使用中
- 現象:
  ```
  System.IO.IOException: Failed to bind to address http://127.0.0.1:5259: address already in use.
  ```
- 原因: 古い `C:\Program Files\AutoDealerSphere\Server\AutoDealerSphere.Server.exe` がまだ動いていた
- 対処: `Stop-Process -Name 'AutoDealerSphere.Server' -Force` で停止
- 注: その後 PC 再起動でも解消

---

### エラー4: SQLite Error 14（最後まで未解決）
- 現象:
  ```
  Microsoft.Data.Sqlite.SqliteException (0x80004005): SQLite Error 14: 'unable to open database file'.
  ```
- インストール先を `C:\Program Files` → `C:\AutoDealerSphere` に変更しても継続

**確認済みで問題なかった事項:**
| 確認内容 | 結果 |
|---|---|
| exeFolder のパス | `C:\AutoDealerSphere\Server`（正しい）|
| dbFolder のパス | `C:\AutoDealerSphere\Server\Data`（正しい）|
| Data フォルダの存在 | 存在する |
| SYSTEM の権限 | FullControl（icacls で付与済み）|
| e_sqlite3.dll の存在 | `runtimes\win-x64\native\e_sqlite3.dll` 存在する |
| AppContext.BaseDirectory の問題 | `Process.GetCurrentProcess().MainModule.FileName` に変更済み |

**試みたこと:**
- DBファイル（crm01.db, -shm, -wal）を削除して再起動 → 同じエラー
- `icacls C:\AutoDealerSphere /grant "SYSTEM:(OI)(CI)F" /T` → 効果なし
- タスクスケジューラで SYSTEM として実行 → 同じエラー
- サービスアカウントを Administrator に変更 → ログオン権限エラー（1069）

**未確認:**
- Visual Studio を完全に閉じた状態で Administrator として直接実行した結果
  （これまでの「直接実行で成功」は Visual Studio のデバッグサーバーが動いていた可能性がある）

---

## インストーラー関連の問題と修正

| 問題 | 修正 |
|---|---|
| `{autopf}` が x86 フォルダを指す | `{autopf64}` に変更 |
| Server/Launcher が同じフォルダに混在 | Server/ と Launcher/ サブフォルダに分離 |
| アンインストールしてもフォルダが残る | `[UninstallDelete]` セクションを追加 |
| 再インストール時にサービスが Disabled になる | `[Run]` の先頭に `sc stop/delete` を追加 |
| インストール先が Program Files で権限問題 | `C:\AutoDealerSphere` に変更 |

---

## .NET バージョン問題

- SDK が 9.0 のみなのにプロジェクトが net8.0 → WASM0005 エラー
- 全プロジェクトを net9.0 に変更（Client, Server, Shared, Launcher）
- パッケージも更新: `8.0.4` → `9.0.4`、Syncfusion `33.1.37` → `33.1.44`

---

## 現在のファイル構成

```
C:\AutoDealerSphere\
├── Server\          ← AutoDealerSphere.Server.exe（サービス）
│   └── Data\        ← crm01.db（SQLite DB）
└── Launcher\        ← AutoDealerSphere.exe（デスクトップアイコン）
```

---

## 現在のサービス設定

```
BINARY_PATH_NAME: C:\AutoDealerSphere\Server\AutoDealerSphere.Server.exe
START_TYPE: AUTO_START
SERVICE_START_NAME: LocalSystem
```

---

## 関連ファイル

- `Server/Program.cs` - ContentRoot・DBパス設定
- `installer/setup.iss` - インストーラースクリプト
- `Server/Properties/PublishProfiles/FolderProfile.pubxml` - 発行設定
- `Launcher/Properties/PublishProfiles/FolderProfile.pubxml` - 発行設定
