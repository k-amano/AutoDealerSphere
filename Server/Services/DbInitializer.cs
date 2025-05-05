using AutoDealerSphere.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace AutoDealerSphere.Server.Services
{
    public static class DbInitializer
    {
        // この関数は使用しなくなりました - Program.csからEnsureCreatedを使用します
        public static void Initialize(SQLDBContext context)
        {
            try
            {
                // テーブルが存在するか確認（存在しなければ例外発生）
                context.Database.ExecuteSqlRaw("SELECT COUNT(*) FROM Clients");
                
                // テーブルが空の場合のみサンプルデータを追加
                if (!context.Clients.Any())
                {
                    AddSampleData(context);
                }
            }
            catch (Exception)
            {
                // Clientテーブルが存在しない場合、テーブルを作成
                string createTableSql = @"
                CREATE TABLE ""Clients"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Clients"" PRIMARY KEY AUTOINCREMENT,
                    ""Name"" TEXT NOT NULL,
                    ""Kana"" TEXT NULL,
                    ""Email"" TEXT NOT NULL,
                    ""Zip"" TEXT NOT NULL,
                    ""Prefecture"" INTEGER NOT NULL,
                    ""Address"" TEXT NOT NULL,
                    ""Building"" TEXT NULL,
                    ""Phone"" TEXT NULL
                );";
                
                context.Database.ExecuteSqlRaw(createTableSql);
                
                // サンプルデータを追加
                AddSampleData(context);
            }
        }
        
        // サンプルデータの初期化のみを行う関数
        public static void InitializeSampleData(SQLDBContext context)
        {
            // テーブルが空の場合のみサンプルデータを追加
            if (!context.Clients.Any())
            {
                AddSampleData(context);
            }
        }
        
        private static void AddSampleData(SQLDBContext context)
        {
            var clients = new[]
            {
                new AutoDealerSphere.Shared.Models.Client
                {
                    Name = "田中太郎",
                    Kana = "タナカタロウ",
                    Email = "tanaka@example.com",
                    Zip = "100-0001",
                    Prefecture = 13, // 東京都
                    Address = "千代田区千代田1-1",
                    Building = "千代田ビル101",
                    Phone = "03-1234-5678"
                },
                new AutoDealerSphere.Shared.Models.Client
                {
                    Name = "佐藤花子",
                    Kana = "サトウハナコ",
                    Email = "sato@example.com",
                    Zip = "530-0001",
                    Prefecture = 27, // 大阪府
                    Address = "大阪市北区梅田1-1",
                    Building = "大阪タワー201",
                    Phone = "06-1234-5678"
                },
                new AutoDealerSphere.Shared.Models.Client
                {
                    Name = "鈴木一郎",
                    Kana = "スズキイチロウ",
                    Email = "suzuki@example.com",
                    Zip = "450-0002",
                    Prefecture = 23, // 愛知県
                    Address = "名古屋市中村区名駅1-1",
                    Building = "名古屋ステーション301",
                    Phone = "052-123-4567"
                }
            };

            context.Clients.AddRange(clients);
            context.SaveChanges();
        }
    }
}