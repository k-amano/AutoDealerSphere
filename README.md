# AutoDealerSphere

自動車販売店向け顧客関係管理（CRM）システム

## 概要

AutoDealerSphereは、自動車販売店の業務効率化を目的とした包括的なCRMシステムです。顧客情報、車両管理、部品在庫、請求書発行など、販売店運営に必要な機能を統合的に提供します。

## 主な機能

- **顧客管理**: 顧客情報の登録・編集・検索、詳細な顧客プロファイル管理
- **車両管理**: 車両情報の登録・編集・検索、車検証データの管理
- **部品管理**: 部品在庫の追跡、価格設定、在庫レベル管理
- **請求書管理**: 請求書の作成・編集・印刷、Excel形式でのエクスポート
- **ユーザー管理**: システムユーザーの権限管理、認証機能

## 技術スタック

- **.NET 8.0**: 最新の.NETフレームワーク
- **Blazor WebAssembly**: クライアントサイドのSPAフレームワーク
- **ASP.NET Core**: サーバーサイドAPI
- **Entity Framework Core 8.0.4**: ORM（Object-Relational Mapping）
- **SQLite**: 軽量で高速なデータベース
- **Syncfusion Blazor Components**: 高機能UIコンポーネントライブラリ

## システム要件

- .NET 8.0 SDK以上
- Visual Studio 2022またはVisual Studio Code
- Windows、macOS、またはLinux

## プロジェクト構成

```
AutoDealerSphere/
├── Client/          # Blazor WebAssemblyクライアント
├── Server/          # ASP.NET Core API
├── Shared/          # 共有モデルとインターフェース
└── docs/           # ドキュメント
```

## インストール手順

1. リポジトリをクローン
```bash
git clone https://github.com/k-amano/AutoDealerSphere.git
cd AutoDealerSphere
```

2. 依存関係の復元
```bash
dotnet restore
```

3. ソリューションのビルド
```bash
dotnet build
```

4. アプリケーションの実行
```bash
cd Server
dotnet run
```

5. ブラウザでアクセス
- HTTP: http://localhost:5259
- HTTPS: https://localhost:7187

## 初期設定

### 管理者アカウント

初回起動時に以下の管理者アカウントが自動的に作成されます：
- **メールアドレス**: admin@example.com
- **パスワード**: admin123

セキュリティのため、初回ログイン後にパスワードを変更することを推奨します。

### データベース

アプリケーション起動時に自動的にデータベースが初期化されます。データベースファイルは `Server/Data/crm01.db` に作成されます。

## 開発

### ビルドコマンド

```bash
# ソリューション全体のビルド
dotnet build

# クライアントプロジェクトのビルド
dotnet build Client/AutoDealerSphere.Client.csproj

# サーバープロジェクトのビルド
dotnet build Server/AutoDealerSphere.Server.csproj
```

### デバッグ実行

Visual Studio 2022を使用する場合は、ソリューションファイル（AutoDealerSphere.sln）を開いてF5キーでデバッグ実行できます。

## ライセンス

### Syncfusionライセンス

本プロジェクトはSyncfusion Blazorコンポーネントを使用しています。商用利用の場合は、適切なSyncfusionライセンスが必要です。

- コミュニティライセンス（売上高$100万未満の企業または個人開発者向け）: https://www.syncfusion.com/products/communitylicense
- 商用ライセンス: https://www.syncfusion.com/sales/products

## 貢献

プルリクエストや問題報告を歓迎します。貢献する前に、以下のドキュメントをご確認ください：

- [CLAUDE.md](./CLAUDE.md) - AIアシスタント向けプロジェクトガイドライン
- [docs/](./docs/) - 詳細な技術ドキュメント

## サポート

問題や質問がある場合は、[Issues](https://github.com/k-amano/AutoDealerSphere/issues)ページで報告してください。

## 著者

Kiyoshi Amano

## 更新履歴

詳細な更新履歴は以下のドキュメントを参照してください：
- [UPDATES_20250801.md](./UPDATES_20250801.md)
- [docs/development-history/](./docs/development-history/)