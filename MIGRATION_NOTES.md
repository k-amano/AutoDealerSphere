# データベース移行手順

## Usersテーブルの更新（PasswordとRole列の追加）

Usersテーブルに新しくPasswordとRole列を追加しました。既存のデータベースを使用している場合は、以下の手順で更新してください。

### 方法1: データベースファイルの削除（推奨）

1. アプリケーションを停止します
2. `Server/Data/crm01.db` ファイルを削除します
3. アプリケーションを再起動します（自動的に新しいデータベースが作成されます）

### 方法2: 手動でテーブルを更新

SQLiteツールを使用して以下のSQLを実行します：

```sql
-- Passwordカラムを追加
ALTER TABLE Users ADD COLUMN Password TEXT NOT NULL DEFAULT '';

-- Roleカラムを追加
ALTER TABLE Users ADD COLUMN Role INTEGER NOT NULL DEFAULT 1;
```

### サンプルユーザー

データベース再作成後、以下のサンプルユーザーが自動的に作成されます：

1. 管理者
   - メールアドレス: admin@example.com
   - パスワード: admin123
   - 権限: 管理者（Role=2）

2. 一般ユーザー
   - メールアドレス: user@example.com
   - パスワード: user123
   - 権限: 一般ユーザー（Role=1）

3. 山田太郎
   - メールアドレス: yamada@example.com
   - パスワード: yamada123
   - 権限: 一般ユーザー（Role=1）

注意: 実際の運用環境では、パスワードはハッシュ化して保存する必要があります。