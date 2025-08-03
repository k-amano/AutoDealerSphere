# 請求書システム仕様書

最終更新日: 2025/08/04

## 1. 概要

### 1.1 目的
AutoDealerSphereシステムに、車検・修理作業の請求書を作成し、Excel形式で出力する機能を追加する。

### 1.2 スコープ
- 部品マスタの管理
- 法定費用マスタの管理
- 請求書の作成・編集・削除（マスター詳細パターン）
- Excel形式での請求書出力

### 1.3 アーキテクチャ変更（2025/08/04）
- **マスター詳細パターンの採用**: 単一フォーム内での親子テーブル同時編集から、親子データを分離して管理する方式に変更
- **部品マスタ選択方式**: 明細入力時に部品マスタから選択する2段階フロー（部品選択→詳細入力）を実装
- **物理削除の採用**: IsActiveフラグによる論理削除は使用せず、完全削除を実装

## 2. データベース設計

### 2.1 新規テーブル

#### 2.1.1 車両区分マスタ（VehicleCategories）
| カラム名 | データ型 | 制約 | 説明 |
|---------|---------|------|------|
| Id | INTEGER | PRIMARY KEY, AUTOINCREMENT | 主キー |
| CategoryName | TEXT | NOT NULL | 区分名（軽自動車、小型車等） |
| Description | TEXT | | 説明 |
| DisplayOrder | INTEGER | NOT NULL DEFAULT 0 | 表示順 |

#### 2.1.2 部品マスタ（Parts）
| カラム名 | データ型 | 制約 | 説明 |
|---------|---------|------|------|
| Id | INTEGER | PRIMARY KEY, AUTOINCREMENT | 主キー |
| PartName | TEXT | NOT NULL | 部品名 |
| Type | TEXT | | 型式 |
| UnitPrice | DECIMAL(10,2) | NOT NULL DEFAULT 0 | 標準単価 |
| CreatedAt | DATETIME | NOT NULL DEFAULT CURRENT_TIMESTAMP | 作成日時 |
| UpdatedAt | DATETIME | NOT NULL DEFAULT CURRENT_TIMESTAMP | 更新日時 |

#### 2.1.3 法定費用マスタ（StatutoryFees）
| カラム名 | データ型 | 制約 | 説明 |
|---------|---------|------|------|
| Id | INTEGER | PRIMARY KEY, AUTOINCREMENT | 主キー |
| VehicleCategoryId | INTEGER | NOT NULL, FOREIGN KEY | 車両区分ID |
| FeeType | TEXT | NOT NULL | 費用種別 |
| Amount | DECIMAL(10,2) | NOT NULL | 金額 |
| IsTaxable | BOOLEAN | NOT NULL DEFAULT 0 | 課税フラグ |
| EffectiveFrom | DATETIME | NOT NULL | 適用開始日 |
| EffectiveTo | DATETIME | | 適用終了日（NULL=現在有効） |
| CreatedAt | DATETIME | NOT NULL DEFAULT CURRENT_TIMESTAMP | 作成日時 |
| UpdatedAt | DATETIME | NOT NULL DEFAULT CURRENT_TIMESTAMP | 更新日時 |

#### 2.1.4 請求書（Invoices）
| カラム名 | データ型 | 制約 | 説明 |
|---------|---------|------|------|
| Id | INTEGER | PRIMARY KEY, AUTOINCREMENT | 主キー |
| InvoiceNumber | TEXT | NOT NULL UNIQUE | 請求書番号 |
| ClientId | INTEGER | NOT NULL, FOREIGN KEY | 顧客ID |
| VehicleId | INTEGER | NOT NULL, FOREIGN KEY | 車両ID |
| InvoiceDate | DATETIME | NOT NULL | 請求日 |
| WorkCompletedDate | DATETIME | NOT NULL | 作業完了日 |
| NextInspectionDate | DATETIME | | 次回車検日 |
| Mileage | INTEGER | | 走行距離 |
| TaxableSubTotal | DECIMAL(10,2) | NOT NULL DEFAULT 0 | 課税小計 |
| NonTaxableSubTotal | DECIMAL(10,2) | NOT NULL DEFAULT 0 | 非課税小計 |
| TaxRate | DECIMAL(5,2) | NOT NULL DEFAULT 10.0 | 消費税率 |
| Tax | DECIMAL(10,2) | NOT NULL DEFAULT 0 | 消費税額 |
| Total | DECIMAL(10,2) | NOT NULL DEFAULT 0 | 合計金額 |
| Notes | TEXT | | 備考 |
| CreatedAt | DATETIME | NOT NULL DEFAULT CURRENT_TIMESTAMP | 作成日時 |
| UpdatedAt | DATETIME | NOT NULL DEFAULT CURRENT_TIMESTAMP | 更新日時 |

#### 2.1.5 請求書明細（InvoiceDetails）
| カラム名 | データ型 | 制約 | 説明 |
|---------|---------|------|------|
| Id | INTEGER | PRIMARY KEY, AUTOINCREMENT | 主キー |
| InvoiceId | INTEGER | NOT NULL, FOREIGN KEY | 請求書ID |
| PartId | INTEGER | FOREIGN KEY | 部品ID（部品マスタから選択） |
| ItemName | TEXT | NOT NULL | 項目名（部品名を自動設定） |
| Type | TEXT | | 型式 |
| RepairMethod | TEXT | | 修理方法 |
| Quantity | DECIMAL(10,2) | NOT NULL DEFAULT 1 | 数量 |
| UnitPrice | DECIMAL(10,2) | NOT NULL DEFAULT 0 | 単価 |
| LaborCost | DECIMAL(10,2) | NOT NULL DEFAULT 0 | 工賃 |
| IsTaxable | BOOLEAN | NOT NULL DEFAULT 1 | 課税フラグ |
| DisplayOrder | INTEGER | NOT NULL DEFAULT 0 | 表示順 |
| CreatedAt | DATETIME | NOT NULL DEFAULT CURRENT_TIMESTAMP | 作成日時 |

### 2.2 既存テーブルの再作成

#### 2.2.1 車両テーブル（Vehicles）の再作成
既存のVehiclesテーブルを削除して、新しい構造で作り直します。

```sql
-- 既存テーブルの削除
DROP TABLE IF EXISTS Vehicles;

-- 新規作成
CREATE TABLE Vehicles (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ClientId INTEGER NOT NULL,
    LicensePlate TEXT NOT NULL,
    Make TEXT,
    Model TEXT,
    Year INTEGER,
    ChassisNumber TEXT,
    EngineNumber TEXT,
    Color TEXT,
    VehicleCategoryId INTEGER,  -- 新規追加
    Weight INTEGER,             -- 新規追加
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (ClientId) REFERENCES Clients(Id),
    FOREIGN KEY (VehicleCategoryId) REFERENCES VehicleCategories(Id)
);
```

## 3. 機能仕様

### 3.1 部品管理機能

#### 3.1.1 部品一覧画面
- **パス**: `/partlist`
- **機能**:
  - 部品の一覧表示（ID、部品名、タイプ、単価、更新日時）
  - ID順でソート表示
  - 一覧画面上部の検索エリアで絞り込み（部品名、タイプ）
  - 「新規登録」ボタン
  - 部品名をクリックして編集画面へ遷移
  - ページング（SfGridの標準機能）
  - データがない場合は「部品情報が見つかりません。」を表示
  - 価格は「¥」記号付きで表示（文字化け対策済み）

#### 3.1.2 部品登録・編集画面
- **新規登録パス**: `/part/0`
- **編集パス**: `/part/{id}`
- **コンポーネント**: `EditPart.razor`（新規・編集共通）
- **入力項目**:
  - 部品名（必須、最大100文字）
  - タイプ（任意、最大50文字）
  - 単価（必須、0以上、¥記号付きで表示）
- **削除機能**: 物理削除（論理削除ではない）

### 3.2 法定費用管理機能

#### 3.2.1 法定費用一覧画面
- **パス**: `/statutoryfeelist`
- **機能**:
  - 車両区分別の法定費用表示
  - 現在有効な料金の表示
  - 将来の適用予定料金の表示
  - 履歴表示機能

#### 3.2.2 法定費用登録画面
- **入力項目**:
  - 車両区分（選択必須）
  - 費用種別（選択必須）
  - 金額（必須）
  - 適用開始日（必須）
  - 適用終了日（任意）

### 3.3 請求書作成機能

#### 3.3.1 請求書一覧画面
- **パス**: `/invoicelist`
- **機能**:
  - 請求書の一覧表示（ID、請求書番号、顧客名、車名、請求日、合計金額）
  - ID順でソート表示
  - 一覧画面上部の検索エリアで絞り込み（請求書番号、顧客名、期間）
  - 「新規作成」ボタン
  - 請求書番号をクリックして編集画面へ遷移
  - 各行に「Excel出力」ボタン
  - データがない場合は「請求書情報が見つかりません。」を表示
  - 金額は「¥」記号付きで表示

#### 3.3.2 請求書詳細画面（マスター詳細パターン）
- **新規作成パス**: `/invoice/detail/0`
- **既存請求書パス**: `/invoice/detail/{id}`
- **コンポーネント**: `InvoiceDetailPage.razor`（読み取り専用表示）
- **画面構成**:
  1. **新規作成時**
     - 「基本情報登録」ボタンのみ表示
     - 基本情報登録後に明細追加が可能

  2. **既存請求書表示時**
     - 基本情報（読み取り専用）
     - 明細一覧（グリッド表示）
     - 操作ボタン群：
       - 基本編集（基本情報編集ダイアログ）
       - 明細追加（明細追加ダイアログ）
       - Excel出力
       - 削除実行

#### 3.3.3 基本情報編集ダイアログ
- **コンポーネント**: `InvoiceBasicInfoDialog.razor`
- **入力項目**:
  - 顧客選択（必須、ドロップダウン）
  - 車両選択（選択した顧客の車両から選択）
  - 請求日（必須、デフォルト：当日）
  - 作業完了日
  - 次回車検日
  - 走行距離
  - 消費税率（デフォルト：10%）
  - 備考

#### 3.3.4 明細編集ダイアログ（2段階フロー）
- **コンポーネント**: `InvoiceDetailDialog.razor / .razor.cs`
- **ステップ1: 部品選択**
  - 部品マスタから部品を選択
  - 部品名での検索機能
  - タイプでのフィルタリング
  - ラジオボタンで選択
  - ページネーション（10件/ページ）

- **ステップ2: 詳細入力**
  - 選択した部品情報の表示（読み取り専用）
    - 部品名
    - タイプ
    - 単価（部品マスタから自動設定）
  - 編集可能項目：
    - 修理方法（ドロップダウン：交換/修理/調整/清掃/点検/その他）
    - 数量（必須、小数点2桁まで）
    - 単価（上書き可能）
    - 工賃
    - 課税フラグ
  - 小計の自動計算表示

  3. **法定費用**
     - 自賠責保険期間選択（24ヶ月/25ヶ月）
     - 法定費用の自動表示
     - 手動調整可能

  4. **計算**
     - 小計の自動計算
     - 消費税の自動計算
     - 合計金額の表示

### 3.4 Excel出力機能

#### 3.4.1 技術仕様
- **ライブラリ**: Syncfusion.XlsIO.Net.Core
- **バージョン**: 30.1.41
- **ライセンス**: エンタープライズ版（試用版警告なし）
- **ファイル形式**: Excel 2007以降（.xlsx）
- **用紙サイズ**: A4縦向き
- **レイアウト**: 提供された請求書画像に準拠

#### 3.4.2 実装詳細
- **サービス**: ExcelExportService
  - IExcelExportServiceインターフェースを実装
  - InvoiceServiceから呼び出される
- **コントローラー**: InvoicesController
  - GET api/Invoices/{id}/export エンドポイント
  - ContentType: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
- **フロントエンド**: JavaScript関数によるダウンロード処理
  - downloadFileFromStream関数をindex.htmlに定義
  - DotNetStreamReferenceを使用してストリーム転送
  - Blazor JSRuntimeから呼び出し

#### 3.4.3 出力内容
- **ヘッダー部**
  - タイトル「御請求書」（フォントサイズ20、太字、中央寄せ）
  - 請求日（右寄せ）
  - 請求先情報（顧客名）
- **車両情報部**
  - 車両名
  - ナンバープレート
  - 走行距離
- **明細部**
  - 項目名、型式、修理方法、数量、単価、金額、工賃
  - ヘッダー行は背景色付き（RGB: 217,217,217）
  - 罫線付き
- **合計部**
  - 課税対象小計
  - 非課税小計
  - 消費税（税率表示付き）
  - 合計金額（太字、フォントサイズ12）
- **その他**
  - 次回車検日（該当する場合）
  - 備考（該当する場合）

#### 3.4.4 ファイル名規則
- フォーマット: `請求書_{請求書番号}_{yyyyMMdd}.xlsx`
- 例: `請求書_202408001_20240802.xlsx`

## 4. 画面遷移

```mermaid
graph TD
    A[メインメニュー] --> B[部品管理/partlist]
    A --> C[法定費用管理/statutoryfeelist]
    A --> D[請求書管理/invoicelist]
    
    B --> B1[部品一覧/partlist]
    B1 --> B2[部品登録/part/0]
    B1 --> B3[部品編集/part/{id}]
    
    C --> C1[法定費用一覧/statutoryfeelist]
    C1 --> C2[法定費用登録]
    
    D --> D1[請求書一覧/invoicelist]
    D1 --> D2[請求書詳細/invoice/detail/0]
    D1 --> D3[請求書詳細/invoice/detail/{id}]
    D2 --> D4[基本情報ダイアログ]
    D2 --> D5[明細ダイアログ]
    D3 --> D4
    D3 --> D5
    D3 --> D6[Excel出力]
```

## 5. バリデーション

### 5.1 部品登録
- 部品名: 必須、最大100文字
- タイプ: 任意、最大50文字
- 単価: 必須、0以上の数値

### 5.2 請求書作成
- 顧客: 必須選択
- 車両: 任意選択（顧客に紐づく車両のみ選択可）
- 請求日: 必須、未来日付不可
- 作業完了日: 任意
- 明細: 作成時は0件でも可（後から追加）

## 6. 初期データ

### 6.1 車両区分マスタ
```sql
INSERT INTO VehicleCategories (CategoryName, Description, DisplayOrder) VALUES
('軽自動車', '軽自動車全般', 1),
('小型車', '車両重量1.0t以下', 2),
('普通車', '車両重量1.5t以下', 3),
('中型車', '車両重量2.0t以下', 4),
('大型車', '車両重量2.0t超', 5);
```

### 6.2 法定費用マスタ（2025年4月現在）
```sql
-- 軽自動車
INSERT INTO StatutoryFees (VehicleCategoryId, FeeType, Amount, IsTaxable, EffectiveFrom) VALUES
(1, '自賠責保険（24ヶ月）', 17540, 0, '2023-04-01'),
(1, '自賠責保険（25ヶ月）', 18040, 0, '2023-04-01'),
(1, '重量税', 6600, 0, '2023-04-01'),
(1, '印紙代', 1800, 0, '2023-04-01');

-- 他の車両区分も同様に登録
```

## 7. セキュリティ要件

- 請求書の作成・編集・削除は認証されたユーザーのみ可能
- 管理者権限を持つユーザーのみ法定費用の変更が可能
- パスワードはBCryptでハッシュ化して保存
- ロール管理（Role=1: 一般ユーザー、Role=2: 管理者）

## 8. パフォーマンス要件

- 請求書一覧画面: 1000件表示で3秒以内
- Excel出力: 明細100行で5秒以内

## 9. 削除方式

全てのエンティティで物理削除を採用（IsActiveフラグによる論理削除は使用しない）

## 10. 共通仕様

### 10.1 一覧画面
- 全ての一覧画面はID順（昇順）でソート
- データがない場合は統一されたメッセージ形式で表示
- 通貨表示は「¥」記号を使用（Format="C0"ではなくカスタムフォーマット）

### 10.2 フォーム
- 新規登録と編集は同一のフォームコンポーネントを使用
- 削除確認ダイアログを表示

### 10.3 UI/UXガイドライン
- **ボタンテキスト**: 基本的に4文字で統一（新規登録、基本編集、明細追加、削除実行など）
- **ボタンスタイル**: Syncfusion.Blazor.Buttonsを一貫して使用
  - プライマリ: `e-primary`
  - 成功: `e-success`
  - 危険: `e-danger`
- **コーディング規約**:
  - コードビハインドパターン（.razor.csファイルにロジック記述）
  - インラインCSSは使用せず、共通CSSファイルに記述

## 11. API仕様

### 11.1 請求書API
- `GET /api/Invoices` - 請求書一覧取得
- `GET /api/Invoices/{id}` - 請求書詳細取得
- `POST /api/Invoices` - 請求書作成
- `PUT /api/Invoices/{id}` - 請求書更新
- `DELETE /api/Invoices/{id}` - 請求書削除（物理削除）
- `GET /api/Invoices/{id}/export` - Excel出力

### 11.2 請求明細API
- `POST /api/Invoices/{invoiceId}/details` - 明細追加
- `PUT /api/Invoices/{invoiceId}/details/{detailId}` - 明細更新
- `DELETE /api/Invoices/{invoiceId}/details/{detailId}` - 明細削除（物理削除）

### 11.3 部品API
- `GET /api/Parts` - 部品一覧取得（IsActive=trueのみ）
- `GET /api/Parts/{id}` - 部品詳細取得
- `POST /api/Parts` - 部品登録
- `PUT /api/Parts/{id}` - 部品更新
- `DELETE /api/Parts/{id}` - 部品削除（物理削除）

## 12. 変更履歴

### 2025/08/04
- マスター詳細パターンへの移行
- 部品マスタからの選択機能追加
- 明細入力を2段階フローに変更
- Excel出力のストリーム処理修正（DotNetStreamReference使用）
- Syncfusionライセンスキーをエンタープライズ版に更新
- UIボタンの4文字規則統一

## 13. 今後の拡張予定

- PDF出力機能
- 請求書のメール送信機能
- 売上集計レポート機能
- 在庫管理との連携
- 請求書の承認ワークフロー
- 定期請求書の自動生成