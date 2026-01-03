# バックアップ・レストア機能 仕様書

## 概要

AutoDealerSphereのデータベース全体をバックアップし、必要に応じて復元する機能を提供します。

**作成日**: 2025-12-21
**最終更新**: 2026-01-03
**バージョン**: 2.0

---

## 1. 機能概要

### 1.1 目的

- システムデータの定期的なバックアップ
- データ損失時の迅速な復旧
- データの移行やテスト環境への複製

### 1.2 主要機能

1. **バックアップ機能**: 全データベーステーブルをCSV形式（ZIP圧縮）でエクスポート
2. **レストア機能**: バックアップファイルから全データを復元（置換モード）
3. **下位互換性**: 従来のJSON形式バックアップのレストアにも対応

---

## 2. メニュー構成

### 2.1 メニュー構造

既存の「車両データインポート」メニューを「データ管理」に統合し、サブメニュー形式で提供します。

```
データ管理 (e-icons e-folder)
├─ データインポート（車両データCSVインポート）
├─ バックアップ（全データのバックアップ）
└─ レストア（バックアップからの復元）
```

### 2.2 メニュー表示仕様

- **親メニュー**: 「データ管理」（フォルダーアイコン表示）
- **展開方式**: クリック式の展開・折りたたみ
- **サブメニュー**: サイドバー内に表示（ポップアップ不可）
- **展開状態表示**: シェブロンアイコン（▶/▼）で視覚的に表現

---

## 3. バックアップ機能

### 3.1 基本仕様

| 項目 | 内容 |
|------|------|
| **対象データ** | 全データベーステーブル |
| **データ形式** | CSV形式（ZIP圧縮） |
| **ファイル名** | `bkYYYYMMDD-HHMM.zip` |
| **実行権限** | 管理者のみ（Role == 2） |
| **文字エンコーディング** | UTF-8（BOM付き） |
| **圧縮形式** | ZIP（標準圧縮レベル） |

### 3.2 バックアップ対象テーブル

1. Users（ユーザー管理）
2. Clients（顧客情報）
3. Vehicles（車両情報）
4. Parts（部品管理）
5. VehicleCategories（車両カテゴリ）
6. StatutoryFees（法定費用）
7. Invoices（請求書）
8. InvoiceDetails（請求明細）
9. IssuerInfo（発行者情報）

### 3.3 バックアップファイル構造

#### ZIPファイル内容

```
bk20260103-1145.zip
├── meta.json          # メタデータ
├── users.csv          # ユーザー情報
├── clients.csv        # 顧客情報
├── vehicles.csv       # 車両情報
├── parts.csv          # 部品情報
├── categories.csv     # 車両カテゴリ
├── fees.csv           # 法定費用
├── invoices.csv       # 請求書
├── details.csv        # 請求明細
└── issuer.csv         # 発行者情報
```

#### meta.json構造

```json
{
  "version": "2.0",
  "format": "csv-zip",
  "timestamp": "2026-01-03T11:45:00Z",
  "database": "crm01",
  "statistics": {
    "users": 5,
    "clients": 120,
    "vehicles": 300,
    "parts": 50,
    "categories": 10,
    "fees": 8,
    "invoices": 200,
    "details": 800,
    "issuer": 1
  }
}
```

#### CSV形式仕様

- **文字エンコーディング**: UTF-8（BOM付き）
- **区切り文字**: カンマ（`,`）
- **改行コード**: CRLF（`\r\n`）
- **ヘッダー行**: 1行目にカラム名を含む
- **NULL値**: 空文字列として表現
- **日付時刻形式**: ISO 8601形式（`YYYY-MM-DDTHH:mm:ss`）
- **特殊文字**: カンマ、改行、ダブルクォートを含むフィールドはダブルクォートで囲む

**CSVサンプル（users.csv）:**

```csv
Id,Username,Email,PasswordHash,Role,CreatedAt
1,admin,admin@example.com,hashed_password,2,2025-12-21T10:00:00
2,user1,user1@example.com,hashed_password,1,2025-12-22T11:00:00
```

### 3.4 画面構成

#### ページURL
`/data-backup`

#### 表示内容

1. **ページタイトル**: 「データバックアップ」
2. **説明文**: 「全データをCSV形式（ZIP圧縮）でバックアップします」
3. **バックアップ対象リスト**:
   - 各テーブル名と件数を表示
   - 例: 「ユーザー情報: 5件」
4. **実行ボタン**: 「バックアップを実行」

#### 処理フロー

1. ユーザーが「バックアップを実行」ボタンをクリック
2. サーバーAPI（`/api/datamanagement/backup`）を呼び出し
3. 全テーブルのデータをCSV形式に変換
4. meta.jsonを作成
5. 全CSVファイルとmeta.jsonをZIPに圧縮
6. Base64エンコードされたZIPファイルをクライアントで受信
7. JavaScriptでファイルダウンロードを実行
8. ファイル名: `bkYYYYMMDD-HHMM.zip`

### 3.5 ファイルサイズの削減効果

- **JSON（インデント付き）→ CSV**: 約50-70%削減
- **CSV → ZIP圧縮**: さらに70-80%削減
- **総合**: 元のJSONファイルの**10-20%程度**に削減

---

## 4. レストア機能

### 4.1 基本仕様

| 項目 | 内容 |
|------|------|
| **対象ファイル** | ZIP形式（CSV）またはJSON形式のバックアップファイル |
| **復元モード** | 置換モード（既存データを全削除してから復元） |
| **実行権限** | 管理者のみ（Role == 2） |
| **安全対策** | 確認チェックボックス必須 |
| **バージョン判定** | ファイル形式を自動判定（ZIP/JSON） |

### 4.2 画面構成

#### ページURL
`/data-restore`

#### 表示内容

1. **ページタイトル**: 「データレストア」
2. **警告メッセージ**:
   - 「既存のデータは全て削除されます」
   - 「この操作は取り消せません」
3. **ファイルアップロード**: ZIP形式またはJSON形式のバックアップファイル
4. **確認チェックボックス**: 「上記の内容を理解し、実行します」
5. **実行ボタン**: 「レストアを実行」（チェックボックスONで有効化）

#### 処理フロー

1. ユーザーがバックアップファイル（.zipまたは.json）を選択
2. 確認チェックボックスをON
3. 「レストアを実行」ボタンをクリック
4. サーバーAPI（`/api/datamanagement/restore`）にファイルを送信
5. サーバー側でファイル形式を自動判定:
   - ZIP形式（0x504B）→ CSV+ZIP形式として処理
   - その他 → JSON形式として処理
6. **ZIP形式の場合**:
   - ZIPファイルを解凍
   - meta.jsonでバージョン確認
   - 各CSVファイルを読み込み
7. **JSON形式の場合（下位互換）**:
   - JSON形式の妥当性チェック
   - バージョン確認
8. トランザクション開始
9. 既存データを外部キー制約を考慮した順序で削除
10. バックアップデータを正しい順序で復元
11. トランザクションコミット（エラー時はロールバック）
12. 復元結果のレポート表示

### 4.3 データ削除・復元順序

#### 削除順序（子→親）
1. InvoiceDetails（請求明細）
2. Invoices（請求書）
3. StatutoryFees（法定費用）
4. Vehicles（車両）
5. Clients（顧客）
6. Users（ユーザー）
7. Parts（部品）
8. VehicleCategories（車両カテゴリ）
9. IssuerInfo（発行者情報）

#### 復元順序（親→子）
1. Users（ユーザー）
2. VehicleCategories（車両カテゴリ）
3. Clients（顧客）
4. Parts（部品）
5. Vehicles（車両）
6. StatutoryFees（法定費用）
7. Invoices（請求書）
8. InvoiceDetails（請求明細）
9. IssuerInfo（発行者情報）

---

## 5. セキュリティと安全性

### 5.1 アクセス制御

- **権限要件**: 管理者（Role == 2）のみ実行可能
- **認証**: 既存のログイン認証機構を利用

### 5.2 安全対策

1. **レストア時の確認**:
   - 確認チェックボックス必須
   - 警告メッセージの明示
2. **トランザクション処理**:
   - 全処理を1トランザクションで実行
   - エラー時は自動ロールバック
3. **エラーハンドリング**:
   - 詳細なエラーメッセージ表示
   - ログ記録（サーバー側）

### 5.3 データ整合性

- **外部キー制約**: 正しい順序で削除・復元
- **ID採番**: SQLiteのauto_incrementカウンターをリセット
- **バリデーション**: CSV/JSON形式、データ型、必須項目のチェック

---

## 6. 技術実装

### 6.1 サーバーサイド

#### APIエンドポイント

| エンドポイント | メソッド | 説明 |
|---------------|---------|------|
| `/api/datamanagement/backup` | GET | 全データをCSV形式（ZIP圧縮）でエクスポート |
| `/api/datamanagement/restore` | POST | ZIP/JSONファイルからデータを復元 |

#### 主要クラス

1. **DataManagementController**: APIエンドポイントの提供
2. **DataManagementService**: バックアップ/レストアのビジネスロジック

#### 技術スタック

- **CSV処理**: `CsvHelper` (v30.0.1)
- **圧縮処理**: `System.IO.Compression`
- **シリアライゼーション**: `System.Text.Json`
- **トランザクション**: Entity Framework Core
- **エンコーディング**: UTF-8（BOM付き）

### 6.2 クライアントサイド

#### ページコンポーネント

1. **DataBackup.razor**: バックアップページ
2. **DataRestore.razor**: レストアページ

#### JavaScript関数

- **downloadFile**: Base64エンコードされたZIPファイルのダウンロード

#### 技術スタック

- **Blazor WebAssembly**: UIフレームワーク
- **Syncfusion Components**: Card、Button等のUIコンポーネント
- **HttpClient**: サーバーAPI通信

---

## 7. UI/UXデザイン

### 7.1 デザインポリシー

- 既存の「車両データインポート」ページのデザインを踏襲
- Syncfusion Cardコンポーネントでセクション分割
- 統一感のあるレイアウトとスタイル

### 7.2 フィードバック表示

- **バックアップ実行中**: 「バックアップ中...」メッセージ
- **レストア実行中**: 「レストア中...」メッセージ
- **完了時**: 成功メッセージと件数表示
- **エラー時**: エラーメッセージの詳細表示

---

## 8. エラーハンドリング

### 8.1 バックアップ時のエラー

| エラー | 原因 | 対処 |
|-------|------|------|
| データベース接続エラー | DB接続失敗 | エラーメッセージ表示 |
| CSV変換エラー | データ形式不正 | ログ記録とエラー表示 |
| ZIP圧縮エラー | メモリ不足等 | エラーメッセージ表示 |
| ダウンロード失敗 | ブラウザ制限 | ブラウザ設定確認を促す |

### 8.2 レストア時のエラー

| エラー | 原因 | 対処 |
|-------|------|------|
| ファイル形式エラー | 不正なZIP/JSON | バリデーションエラー表示 |
| バージョン不一致 | 古いバックアップ | 非対応メッセージ表示 |
| CSV読み込みエラー | CSV形式不正 | エラー詳細表示 |
| データ整合性エラー | 外部キー制約違反 | ロールバックとエラー詳細表示 |
| トランザクションエラー | DB処理失敗 | ロールバックとエラーログ |

---

## 9. バージョン互換性

### 9.1 バックアップファイル形式

| バージョン | 形式 | 説明 |
|-----------|------|------|
| 1.0 | JSON | 旧形式（インデント付きJSON） |
| 2.0 | CSV+ZIP | 新形式（CSV形式、ZIP圧縮） |

### 9.2 下位互換性

- **v2.0システム**: v1.0（JSON）とv2.0（CSV+ZIP）の両方をレストア可能
- **ファイル形式判定**: ファイルヘッダー（マジックバイト）で自動判定
  - `0x504B`（PK）→ ZIP形式として処理
  - その他 → JSON形式として処理

---

## 10. 今後の拡張可能性

### 10.1 機能拡張案

1. **選択的バックアップ**:
   - テーブルごとのバックアップ
   - 顧客データのみ、請求書データのみ等
2. **マージモード**:
   - 既存データに追加（ID競合時はスキップ）
   - 上書きモード（同一IDを上書き）
3. **スケジュールバックアップ**:
   - 定期的な自動バックアップ
   - バックアップ履歴の管理
4. **差分バックアップ**:
   - 前回バックアップからの変更分のみ
5. **クラウドストレージ連携**:
   - Google Drive、OneDrive等への自動保存

### 10.2 改善案

1. **バックアップ履歴**:
   - 実行日時、ファイルサイズ、件数の記録
   - 履歴一覧ページの追加
2. **プレビュー機能**:
   - レストア前のデータプレビュー
   - 件数の比較表示
3. **バックアップ検証**:
   - バックアップファイルの整合性チェック

---

## 11. 開発履歴

### バージョン 2.0（2026-01-03）

#### 変更内容

1. **ファイル形式の変更**:
   - JSON形式 → CSV形式（ZIP圧縮）に変更
   - ファイルサイズを80-90%削減
   - 可読性の向上（CSVはExcelで直接開ける）

2. **ファイル名の短縮**:
   - `backup_YYYYMMDD_HHmmss.json` → `bkYYYYMMDD-HHMM.zip`
   - 27文字 → 18文字（9文字短縮）

3. **下位互換性の確保**:
   - 従来のJSON形式バックアップもレストア可能
   - ファイル形式の自動判定機能を実装

4. **技術スタック追加**:
   - CsvHelper NuGetパッケージ追加
   - System.IO.Compression使用

### バージョン 1.0（2025-12-21）

#### 実装内容

1. **メニュー構成**:
   - 「車両データインポート」を「データ管理」に統合
   - サブメニュー追加（データインポート、バックアップ、レストア）
   - フォルダーアイコンの追加
   - クリック式展開・折りたたみUI実装

2. **バックアップ機能**:
   - 全テーブルのJSONエクスポート
   - ファイルダウンロード機能
   - 件数表示

3. **レストア機能**:
   - JSONファイルアップロード
   - 置換モード実装
   - トランザクション処理
   - 確認チェックボックス

4. **サーバーサイド**:
   - DataManagementController追加
   - DataManagementService追加
   - API エンドポイント実装

5. **クライアントサイド**:
   - DataBackup.razorページ追加
   - DataRestore.razorページ追加
   - JavaScript downloadFile関数追加

#### 修正履歴

- **12/21 13:04**: サブメニューをクリック式に変更、バックアップダウンロード修正
- **12/21 13:36**: データ管理親メニューにアイコン追加、サブメニューUI改善
- **12/21 13:44**: アイコンクラスをe-folderに修正（e-databaseは非対応）

---

## 12. 参考資料

### 12.1 関連ドキュメント

- [車両データインポート機能](./vehicle-import-system.md)（予定）
- [データベーススキーマ](./database-schema.md)（予定）

### 12.2 技術参考

- [Entity Framework Core Documentation](https://learn.microsoft.com/ja-jp/ef/core/)
- [System.Text.Json Documentation](https://learn.microsoft.com/ja-jp/dotnet/api/system.text.json)
- [CsvHelper Documentation](https://joshclose.github.io/CsvHelper/)
- [System.IO.Compression Documentation](https://learn.microsoft.com/ja-jp/dotnet/api/system.io.compression)
- [Syncfusion Blazor Components](https://blazor.syncfusion.com/)

---

## 付録A: ファイル構成

### A.1 サーバーサイド

```
Server/
├── Controllers/
│   └── DataManagementController.cs
├── Services/
│   ├── IDataManagementService.cs
│   └── DataManagementService.cs
└── AutoDealerSphere.Server.csproj（CsvHelperパッケージ追加）
```

### A.2 クライアントサイド

```
Client/
├── Pages/
│   ├── DataBackup.razor
│   ├── DataBackup.razor.cs
│   ├── DataRestore.razor
│   └── DataRestore.razor.cs
├── Layout/
│   └── MainLayout.razor（メニュー定義）
└── wwwroot/
    └── index.html（downloadFile関数）
```

### A.3 共通

```
Shared/
└── Models/（既存のモデルクラスを使用）
```

---

## 付録B: 設定ファイル

### B.1 appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=Data/crm01.db"
  }
}
```

バックアップ・レストア機能に固有の設定は現在ありません。

---

**文書管理**
- 作成者: Claude Code
- 承認者: k-amano
- 管理場所: `/docs/specification/backup-restore-system.md`
