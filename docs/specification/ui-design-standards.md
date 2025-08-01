# UIデザイン標準仕様書

## 概要
AutoDealerSphereのUI/UXデザインの統一性を保つための標準仕様。

## 1. 共通レイアウト

### 1.1 ページ構成
```
┌─────────────────────────────────────────┐
│         ヘッダー（システム名）            │
├─────────────┬───────────────────────────┤
│             │                           │
│   サイド     │      メインコンテンツ      │
│   メニュー   │                           │
│             │                           │
└─────────────┴───────────────────────────┘
```

### 1.2 パンくずリスト
全ての管理画面で以下の形式で表示：
```html
<SfBreadcrumb>
    <BreadcrumbItems>
        <BreadcrumbItem IconCss="e-icons e-home" Url="/" />
        <BreadcrumbItem Text="[画面名]" Url="/" />
    </BreadcrumbItems>
</SfBreadcrumb>
```

## 2. リスト画面デザイン

### 2.1 2段表示グリッド
顧客一覧、ユーザー一覧、車両一覧で採用している表示形式。

#### 構成要素
- **1行目**: 主要情報（ID、名前、補足情報）
- **2行目**: 詳細情報（住所、連絡先など）

#### CSSクラス
- `.two-line-grid`: グリッド全体
- `.client-row`, `.user-row`, `.vehicle-row`: 各行のコンテナ
- `.row-line1`, `.row-line2`: 1行目、2行目

### 2.2 検索フォーム
```html
<div class="search-form">
    <EditForm Model="@Search" OnValidSubmit="() => OnSearch(Search)">
        <DataAnnotationsValidator />
        <div class="form">
            <!-- 検索項目 -->
            <div class="form-buttons">
                <SfButton Type="ButtonType.Submit">検索</SfButton>
            </div>
        </div>
    </EditForm>
</div>
```

### 2.3 ツールバー
```html
<div class="toolbar">
    <SfButton OnClick="AddItem" CssClass="e-primary">新規追加</SfButton>
</div>
```

## 3. フォームデザイン

### 3.1 基本構造
```html
<div class="[entity]-form">
    <EditForm Model="@Item" OnValidSubmit="OnValidated">
        <DataAnnotationsValidator />
        <div class="form">
            <!-- フォーム項目 -->
            <div class="form-buttons">
                <SfButton OnClick="OnRegister" IsPrimary="true">登録</SfButton>
                <SfButton OnClick="OnOpenDialogue" CssClass="e-danger">削除</SfButton>
                <SfButton OnClick="OnCancel">キャンセル</SfButton>
            </div>
        </div>
    </EditForm>
</div>
```

### 3.2 フォーム行
```html
<div class="form-row">
    <label class="required">項目名</label>
    <SfTextBox @bind-Value="Item.Property" Placeholder="プレースホルダー"></SfTextBox>
    <ValidationMessage For="@(() => Item.Property)" />
</div>
```

### 3.3 必須項目
必須項目のラベルには`required`クラスを付与。CSSで「必須」バッジを自動表示。

## 4. 色とスタイル

### 4.1 カラーパレット
- **プライマリ**: #7db4e6（青）
- **危険/削除**: #dc3545（赤）
- **背景**: #f8f8f8（薄いグレー）
- **ボーダー**: #ddd, #e0e0e0
- **テキスト**: #333（メイン）, #666（サブ）, #999（補助）

### 4.2 フォント
```css
font-family: Arial, Helvetica, 'メイリオ', Meiryo, 
             'ヒラギノ角ゴ Pro W3', 'Hiragino Kaku Gothic Pro',
             'ＭＳ Ｐゴシック', sans-serif;
```

### 4.3 フォントサイズ
- 本文: 12px
- h2: 18px
- h3: 16px
- グリッド内: 14px

## 5. インタラクション

### 5.1 リンク
- グリッド内のリンク: `.grid-link`クラス
- ホバー時の色変更: #0066cc → #0052a3

### 5.2 削除確認ダイアログ
```html
<SfDialog Width="250px" ShowCloseIcon="true" IsModal="true" @bind-Visible="@IsVisible">
    <DialogTemplates>
        <Header> 確認 </Header>
        <Content> 削除してもよろしいですか? </Content>
    </DialogTemplates>
    <DialogButtons>
        <DialogButton Content="OK" IsPrimary="true" OnClick="@OnDelete" />
        <DialogButton Content="Cancel" OnClick="@OnCloseDialogue" />
    </DialogButtons>
</SfDialog>
```

## 6. レスポンシブデザイン

### 6.1 フォーム幅
- 入力フィールドの最大幅: 400px
- フォーム全体の推奨幅: 50%

### 6.2 グリッド
- ページング有効
- ソート機能有効
- 複数列ソート有効

## 7. アイコン

Syncfusion標準アイコンを使用：
- ホーム: `e-icons e-home`
- ユーザー: `e-icons e-user`
- 人々: `e-icons e-people`
- リスト: `e-icons e-list`
- アップロード: `e-icons e-upload`
- サインアウト: `e-icons e-sign-out`

## 8. エラー表示

### 8.1 バリデーションエラー
- 色: #dc3545
- フォントサイズ: 0.9em
- 表示位置: 入力フィールドの下

### 8.2 エラーメッセージ
```html
<p class="error-message">エラーメッセージ</p>
```

## 9. 特殊なスタイル

### 9.1 権限表示（ユーザー一覧）
```css
.user-role {
    padding: 2px 8px;
    border: 1px solid #ddd;
    border-radius: 4px;
    font-size: 12px;
}
```

管理者の場合は青色の背景で強調表示。