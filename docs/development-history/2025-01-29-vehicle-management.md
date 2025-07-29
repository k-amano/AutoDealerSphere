# 車両管理機能 開発履歴

## 日付: 2025年1月29日

## 概要
顧客管理機能と同様の構成で車両管理機能を新規開発

## 開発内容

### 1. 車両一覧ページ（VehicleList.razor）
**機能**:
- 車両データの一覧表示
- 検索機能（車名、ナンバー、所有者名）
- 新規追加ボタン
- 各行に編集ボタン

**表示項目**:
- ID
- 車名
- 型式
- ナンバー（複数フィールドを結合表示）
- 所有者（顧客名を表示）
- 初度登録
- 車検満了日
- 走行距離

### 2. 車両編集ページ（EditVehicle.razor）
**機能**:
- 新規追加（VehicleId=0）
- 既存車両の編集
- 削除機能

**特徴**:
- 顧客リストをドロップダウンで選択可能
- パンくずリストでナビゲーション

### 3. 車両フォームコンポーネント（VehicleForm.razor）
**セクション構成**:

#### 基本情報
- 所有者（必須・ドロップダウン）
- 車名
- 型式

#### ナンバープレート情報
- 登録地域
- 分類番号
- ひらがな
- ナンバー

#### 車両詳細
- 初度登録年月
- 走行距離
- 車台番号
- 型式指定番号
- 類別区分番号

#### 車検情報
- 車検満了日
- 次回車検日
- 車検証番号

#### その他の情報
- 用途
- 自家用・事業用
- 車体の形状
- 燃料の種類

**スタイル**:
- ClientFormと同様のform-rowレイアウト
- セクションごとに見出し（h4）で区切り
- 専用CSS（VehicleForm.razor.css）

### 4. 車両APIコントローラー（VehiclesController.cs）
**エンドポイント**:
- `GET /api/vehicles` - 一覧取得（Client情報を含む）
- `GET /api/vehicles/{id}` - 個別取得
- `GET /api/vehicles/search` - 検索（車名、ナンバー、所有者名）
- `POST /api/vehicles` - 新規作成
- `PUT /api/vehicles/{id}` - 更新
- `DELETE /api/vehicles/{id}` - 削除

**特徴**:
- Entity Framework Coreを使用
- ClientとのリレーションをInclude
- 更新時はClientプロパティを更新対象外に設定

### 5. メニュー追加
MainLayout.razorのメニューに「車両管理」を追加
- アイコン: e-icons e-list
- URL: /vehiclelist

## 技術的なポイント

### 1. エンドポイントの修正
- 最初`api/clients`としていたが、実際は`api/client`だったため修正
- ClientControllerのルーティングに合わせた

### 2. データベース構成
- SQLDBContextに既にVehiclesテーブルが定義済み
- ClientとのForeignKeyリレーションが設定済み
- カスケード削除が有効

### 3. Syncfusionコンポーネントの使用
- SfGrid: 一覧表示
- SfDropDownList: 所有者選択
- SfDatePicker: 日付入力
- SfNumericTextBox: 数値入力
- SfButton: 各種ボタン

## ファイル一覧
1. `/Client/Pages/VehicleList.razor` - 車両一覧画面
2. `/Client/Pages/VehicleList.razor.cs` - 車両一覧コードビハインド
3. `/Client/Pages/EditVehicle.razor` - 車両編集画面
4. `/Client/Pages/EditVehicle.razor.cs` - 車両編集コードビハインド
5. `/Client/Shared/VehicleForm.razor` - 車両フォームコンポーネント
6. `/Client/Shared/VehicleForm.razor.css` - 車両フォームスタイル
7. `/Server/Controllers/VehiclesController.cs` - 車両APIコントローラー
8. `/Client/Layout/MainLayout.razor` - メニュー項目追加

## コミット
- 7f4353b 車両管理機能を追加