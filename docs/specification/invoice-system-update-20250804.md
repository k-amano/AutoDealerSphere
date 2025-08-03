# 請求書システム仕様書（更新版）
最終更新日: 2025/08/04

## 1. システム概要

請求書管理システムは、自動車販売店向けCRMシステム「AutoDealerSphere」の一部として、車両整備・修理に関する請求書の作成、管理、Excel出力を行う機能を提供します。

## 2. アーキテクチャ変更点

### 2.1 マスター詳細パターンの採用
- **旧設計**: 単一フォーム内で親子テーブルを同時編集
- **新設計**: マスター詳細パターンで親子データを分離して管理
  - 請求書基本情報と明細情報を別々のダイアログで編集
  - SfGrid内でのEditTemplateを廃止し、フォーム送信の競合を解消

### 2.2 削除方式
- **物理削除**を採用（IsActiveフラグによる論理削除は使用しない）
- 削除時は関連する明細データも含めて完全削除

## 3. データモデル

### 3.1 Invoice（請求書）
```csharp
public class Invoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; }  // 請求書番号（YY-MMDD-00001形式）
    public int ClientId { get; set; }          // 顧客ID
    public int? VehicleId { get; set; }        // 車両ID（任意）
    public DateTime InvoiceDate { get; set; }   // 請求日
    public DateTime? WorkCompletedDate { get; set; } // 作業完了日
    public DateTime? NextInspectionDate { get; set; } // 次回車検日
    public int? Mileage { get; set; }          // 走行距離
    public decimal TaxRate { get; set; }       // 消費税率（デフォルト10%）
    public string? Notes { get; set; }         // 備考
    public List<InvoiceDetail> InvoiceDetails { get; set; } // 明細リスト
    
    // ナビゲーションプロパティ
    public virtual Client? Client { get; set; }
    public virtual Vehicle? Vehicle { get; set; }
}
```

### 3.2 InvoiceDetail（請求明細）
```csharp
public class InvoiceDetail
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }         // 請求書ID
    public int? PartId { get; set; }           // 部品ID（部品マスタから選択）
    public string ItemName { get; set; }       // 項目名（部品名）
    public string? Type { get; set; }          // タイプ
    public string? RepairMethod { get; set; }  // 修理方法
    public decimal Quantity { get; set; }      // 数量
    public decimal UnitPrice { get; set; }     // 単価
    public decimal LaborCost { get; set; }     // 工賃
    public bool IsTaxable { get; set; }        // 課税対象
    public int DisplayOrder { get; set; }      // 表示順
    
    // 計算プロパティ
    public decimal SubTotal => (UnitPrice * Quantity) + LaborCost;
}
```

### 3.3 Part（部品マスタ）
```csharp
public class Part
{
    public int Id { get; set; }
    public string PartName { get; set; }       // 部品名
    public string? Type { get; set; }          // タイプ
    public decimal UnitPrice { get; set; }     // 単価
    public bool IsActive { get; set; }         // 有効フラグ
}
```

## 4. 画面構成

### 4.1 請求書一覧画面（/invoicelist）
- **機能**
  - 請求書の検索（請求書番号、顧客名、請求日範囲）
  - 新規作成ボタン（顧客データがない場合は無効化）
  - 請求書詳細へのリンク
  - Excel出力ボタン（各行に配置）

### 4.2 請求書詳細画面（/invoice/detail/{id}）
- **新規作成時（id=0）**
  - 基本情報登録ボタンのみ表示
  - 基本情報登録後に明細追加が可能

- **既存請求書表示時**
  - 基本情報の表示（読み取り専用）
  - 明細一覧の表示（グリッド形式）
  - 操作ボタン
    - 基本編集（4文字）
    - 明細追加（4文字）
    - Excel出力（5文字）
    - 削除実行（4文字）

### 4.3 基本情報編集ダイアログ
- **InvoiceBasicInfoDialog.razor**
- モーダルダイアログで表示
- 入力項目：
  - 顧客（ドロップダウン選択、必須）
  - 車両（選択した顧客の車両から選択）
  - 請求日（必須）
  - 作業完了日
  - 次回車検日
  - 走行距離
  - 消費税率
  - 備考

### 4.4 明細編集ダイアログ
- **InvoiceDetailDialog.razor / .razor.cs**
- 2段階の入力フロー：

#### ステップ1: 部品選択
- 部品マスタから部品を選択
- 検索機能（部品名で絞り込み）
- タイプでのフィルタリング
- ラジオボタンで選択
- ページネーション（10件/ページ）

#### ステップ2: 詳細入力
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

## 5. API エンドポイント

### 5.1 請求書API
- `GET /api/Invoices` - 請求書一覧取得
- `GET /api/Invoices/{id}` - 請求書詳細取得
- `POST /api/Invoices` - 請求書作成
- `PUT /api/Invoices/{id}` - 請求書更新
- `DELETE /api/Invoices/{id}` - 請求書削除
- `GET /api/Invoices/{id}/export` - Excel出力

### 5.2 請求明細API
- `POST /api/Invoices/{invoiceId}/details` - 明細追加
- `PUT /api/Invoices/{invoiceId}/details/{detailId}` - 明細更新
- `DELETE /api/Invoices/{invoiceId}/details/{detailId}` - 明細削除

### 5.3 部品API
- `GET /api/Parts` - 部品一覧取得（アクティブな部品のみ）

## 6. ビジネスロジック

### 6.1 請求書番号の自動採番
- 形式: `YY-MMDD-00001`
- YY: 西暦年の下2桁
- MMDD: 月日
- 00001: 日ごとの連番（5桁）

### 6.2 合計金額の計算
```csharp
// 課税対象小計
var taxableSubTotal = details.Where(d => d.IsTaxable)
                             .Sum(d => d.SubTotal);

// 非課税小計
var nonTaxableSubTotal = details.Where(d => !d.IsTaxable)
                                .Sum(d => d.SubTotal);

// 消費税
var tax = Math.Floor(taxableSubTotal * (taxRate / 100));

// 合計
var total = taxableSubTotal + nonTaxableSubTotal + tax;
```

### 6.3 バリデーション
- 請求書作成時に顧客の存在確認
- 車両選択時の顧客との関連チェック
- 明細の必須項目チェック（項目名、数量、単価）

## 7. Excel出力機能

### 7.1 使用ライブラリ
- Syncfusion.XlsIO.Net.Core (v30.1.41)
- エンタープライズライセンスキーを使用

### 7.2 出力形式
- ファイル名: `請求書_{請求書番号}_{YYYYMMDD}.xlsx`
- シート構成：
  1. ヘッダー部（会社情報、顧客情報）
  2. 明細部（項目、数量、単価、小計）
  3. 合計部（課税/非課税小計、消費税、総合計）

### 7.3 JavaScript連携
```javascript
// DotNetStreamReferenceを使用したファイルダウンロード
window.downloadFileFromStream = async (fileName, contentStreamReference) => {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName;
    anchorElement.click();
    anchorElement.remove();
    URL.revokeObjectURL(url);
};
```

## 8. UI/UXガイドライン

### 8.1 ボタンテキスト規則
- 基本的に4文字で統一
- 例：新規登録、基本編集、明細追加、削除実行、一覧へ戻る

### 8.2 CSSクラス
- プライマリボタン: `e-primary`
- 成功ボタン: `e-success`
- 危険ボタン: `e-danger`
- 小サイズボタン: `e-btn-sm`

### 8.3 コーディング規約
- コードビハインドパターンの採用
  - .razor: ビューのみ
  - .razor.cs: すべてのロジック
- インラインCSSは使用せず、共通CSSファイルに記述
- Syncfusionコンポーネントの一貫した使用

## 9. セキュリティ考慮事項

### 9.1 データ検証
- サーバー側での入力値検証
- SQLインジェクション対策（Entity Framework使用）
- XSS対策（Blazorの自動エスケープ）

### 9.2 アクセス制御
- 請求書の作成・編集・削除は認証済みユーザーのみ
- 他の顧客の請求書へのアクセス防止

## 10. 今後の拡張予定

### 10.1 機能追加
- 請求書のPDF出力
- メール送信機能
- 入金管理機能
- 請求書テンプレート機能

### 10.2 改善項目
- 部品マスタの在庫管理連携
- 請求書の承認ワークフロー
- 定期請求書の自動生成
- 売上分析レポート

## 変更履歴

### 2025/08/04
- マスター詳細パターンへの移行
- 部品マスタからの選択機能追加
- 明細入力を2段階フローに変更
- Excel出力のストリーム処理修正
- Syncfusionライセンスキーの更新