# 請求書システム仕様書

## 1. 概要

### 1.1 目的
AutoDealerSphereシステムに、車検・修理作業の請求書を作成し、Excel形式で出力する機能を追加する。

### 1.2 スコープ
- 部品マスタの管理
- 法定費用マスタの管理
- 請求書の作成・編集・削除
- Excel形式での請求書出力

## 2. データベース設計

### 2.1 新規テーブル

#### 2.1.1 車両区分マスタ（VehicleCategories）
| カラム名 | データ型 | 制約 | 説明 |
|---------|---------|------|------|
| Id | INTEGER | PRIMARY KEY, AUTOINCREMENT | 主キー |
| CategoryName | TEXT | NOT NULL | 区分名（軽自動車、小型車等） |
| Description | TEXT | | 説明 |
| DisplayOrder | INTEGER | NOT NULL DEFAULT 0 | 表示順 |
| IsActive | BOOLEAN | NOT NULL DEFAULT 1 | 有効フラグ |

#### 2.1.2 部品マスタ（Parts）
| カラム名 | データ型 | 制約 | 説明 |
|---------|---------|------|------|
| Id | INTEGER | PRIMARY KEY, AUTOINCREMENT | 主キー |
| PartName | TEXT | NOT NULL | 部品名 |
| Type | TEXT | | 型式 |
| UnitPrice | DECIMAL(10,2) | NOT NULL DEFAULT 0 | 標準単価 |
| IsActive | BOOLEAN | NOT NULL DEFAULT 1 | 有効フラグ |
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
| IsActive | BOOLEAN | NOT NULL DEFAULT 1 | 有効フラグ |
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
| PartId | INTEGER | FOREIGN KEY | 部品ID（NULL可） |
| ItemName | TEXT | NOT NULL | 項目名 |
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
  - 部品の一覧表示（部品名、型式、単価）
  - 一覧画面上部の検索エリアで絞り込み（部品名、型式）
  - 「新規作成」ボタン
  - 各行に「編集」「削除」ボタン
  - ページング（SfGridの標準機能）

#### 3.1.2 部品登録・編集画面
- **新規登録パス**: `/part/0`
- **編集パス**: `/part/{id}`
- **コンポーネント**: `EditPart.razor`（新規・編集共通）
- **入力項目**:
  - 部品名（必須、最大100文字）
  - 型式（任意、最大50文字）
  - 単価（必須、0以上）
  - 有効フラグ

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
  - 請求書の一覧表示
  - 一覧画面上部の検索エリアで絞り込み（請求書番号、顧客名、期間）
  - 「新規作成」ボタン
  - 各行に「編集」「削除」「複製」「Excel出力」ボタン

#### 3.3.2 請求書作成・編集画面
- **新規作成パス**: `/invoice/0`
- **編集パス**: `/invoice/{id}`
- **コンポーネント**: `EditInvoice.razor`（新規・編集共通）
- **入力項目**:
  1. **基本情報**
     - 顧客選択（必須）
     - 車両選択（必須）
     - 請求日（必須、デフォルト：当日）
     - 作業完了日（必須）
     - 走行距離（任意）

  2. **明細入力**
     - 部品選択（オートコンプリート）
     - カスタム項目入力
     - 数量、単価、工賃の入力
     - 修理方法の入力
     - 課税/非課税の選択

  3. **法定費用**
     - 自賠責保険期間選択（24ヶ月/25ヶ月）
     - 法定費用の自動表示
     - 手動調整可能

  4. **計算**
     - 小計の自動計算
     - 消費税の自動計算
     - 合計金額の表示

### 3.4 Excel出力機能

#### 3.4.1 出力仕様
- **ファイル形式**: Excel 2007以降（.xlsx）
- **用紙サイズ**: A4
- **レイアウト**: 提供された請求書画像に準拠

#### 3.4.2 出力内容
- ヘッダー部（御請求書、日付、請求先情報）
- 明細部（部品、修理内容、数量、金額）
- 法定費用部
- 合計部（小計、消費税、合計）

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
    D1 --> D2[請求書作成/invoice/0]
    D1 --> D3[請求書編集/invoice/{id}]
    D2 --> D4[Excel出力]
    D3 --> D4
```

## 5. バリデーション

### 5.1 部品登録
- 部品名: 必須、最大100文字
- 単価: 必須、0以上の数値

### 5.2 請求書作成
- 顧客: 必須選択
- 車両: 必須選択
- 請求日: 必須、未来日付不可
- 作業完了日: 必須
- 明細: 最低1件必須

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

## 8. パフォーマンス要件

- 請求書一覧画面: 1000件表示で3秒以内
- Excel出力: 明細100行で5秒以内

## 9. 今後の拡張予定

- PDF出力機能
- 請求書のメール送信機能
- 売上集計レポート機能
- 在庫管理との連携