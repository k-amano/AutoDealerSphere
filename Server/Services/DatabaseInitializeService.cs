using AutoDealerSphere.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoDealerSphere.Server.Services
{
    public class DatabaseInitializeService
    {
        private readonly SQLDBContext _context;

        public DatabaseInitializeService(SQLDBContext context)
        {
            _context = context;
        }

        public void Initialize()
        {
            try
            {
                // データベースが存在しない場合は作成
                _context.Database.EnsureCreated();

                // Clientsテーブルが存在しない場合は作成
                CreateClientsTableIfNotExists();

                // Usersテーブルが存在しない場合は作成
                CreateUsersTableIfNotExists();

                // 請求書システム用テーブルの作成（Vehiclesより先に作成）
                CreateVehicleCategoriesTableIfNotExists();
                CreatePartsTableIfNotExists();

                // Vehiclesテーブルが存在しない場合は作成（VehicleCategoriesの後）
                CreateVehiclesTableIfNotExists();

                // 残りのテーブルを作成
                CreateStatutoryFeesTableIfNotExists();
                CreateInvoicesTableIfNotExists();
                CreateInvoiceDetailsTableIfNotExists();
                CreateIssuerInfoTableIfNotExists();

                // 初期データの作成
                CreateInitialAdminUser();
                CreateInitialVehicleCategories();
                CreateInitialStatutoryFees();
                CreateInitialParts();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database initialization error: {ex.Message}");
                throw;
            }
        }

        private void CreateClientsTableIfNotExists()
        {
            try
            {
                _context.Database.ExecuteSqlRaw(@"
                        CREATE TABLE IF NOT EXISTS Clients (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL,
                            Kana TEXT,
                            Email TEXT NOT NULL,
                            Zip TEXT NOT NULL,
                            Prefecture INTEGER NOT NULL,
                            Address TEXT NOT NULL,
                            Building TEXT,
                            Phone TEXT
                        )
                    ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not create Clients table: {ex.Message}");
            }
        }

        private void CreateVehiclesTableIfNotExists()
        {
            try
            {
                _context.Database.ExecuteSqlRaw(@"
                        CREATE TABLE IF NOT EXISTS Vehicles (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            ClientId INTEGER NOT NULL,
                            LicensePlateLocation TEXT,
                            LicensePlateClassification TEXT,
                            LicensePlateHiragana TEXT,
                            LicensePlateNumber TEXT,
                            KeyNumber TEXT,
                            ChassisNumber TEXT,
                            TypeCertificationNumber TEXT,
                            CategoryNumber TEXT,
                            VehicleName TEXT,
                            VehicleModel TEXT,
                            Mileage DECIMAL(18,2),
                            FirstRegistrationDate TEXT,
                            Purpose TEXT,
                            PersonalBusinessUse TEXT,
                            BodyShape TEXT,
                            SeatingCapacity INTEGER,
                            MaxLoadCapacity INTEGER,
                            VehicleWeight INTEGER,
                            VehicleTotalWeight INTEGER,
                            VehicleLength INTEGER,
                            VehicleWidth INTEGER,
                            VehicleHeight INTEGER,
                            FrontOverhang INTEGER,
                            RearOverhang INTEGER,
                            ModelCode TEXT,
                            EngineModel TEXT,
                            Displacement DECIMAL(18,2),
                            FuelType TEXT,
                            InspectionExpiryDate TEXT,
                            NextInspectionDate TEXT,
                            InspectionCertificateNumber TEXT,
                            UserNameOrCompany TEXT,
                            UserAddress TEXT,
                            BaseLocation TEXT,
                            VehicleCategoryId INTEGER,
                            CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                            UpdatedAt TEXT,
                            FOREIGN KEY (ClientId) REFERENCES Clients(Id) ON DELETE CASCADE,
                            FOREIGN KEY (VehicleCategoryId) REFERENCES VehicleCategories(Id)
                        )
                    ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not create Vehicles table: {ex.Message}");
            }
        }

        private void CreateUsersTableIfNotExists()
        {
            try
            {
                _context.Database.ExecuteSqlRaw(@"
                        CREATE TABLE IF NOT EXISTS Users (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL,
                            Email TEXT NOT NULL,
                            Password TEXT NOT NULL,
                            Role INTEGER NOT NULL DEFAULT 1
                        )
                    ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not create Users table: {ex.Message}");
            }
        }

        private void CreateInitialAdminUser()
        {
            try
            {
                // 管理者ユーザーが存在しない場合のみ作成
                var adminExists = _context.Users.Any(u => u.Role == 2);
                
                if (!adminExists)
                {
                    var adminUser = new User
                    {
                        Name = "管理者",
                        Email = "admin@example.com",
                        Password = PasswordHashService.HashPassword("admin123"),
                        Role = 2 // 管理者
                    };

                    _context.Users.Add(adminUser);
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not create admin user: {ex.Message}");
            }
        }

        private void CreateVehicleCategoriesTableIfNotExists()
        {
            try
            {
                _context.Database.ExecuteSqlRaw(@"
                        CREATE TABLE IF NOT EXISTS VehicleCategories (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            CategoryName TEXT NOT NULL,
                            Description TEXT,
                            DisplayOrder INTEGER NOT NULL DEFAULT 0
                        )
                    ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not create VehicleCategories table: {ex.Message}");
            }
        }

        private void CreatePartsTableIfNotExists()
        {
            try
            {
                _context.Database.ExecuteSqlRaw(@"
                        CREATE TABLE IF NOT EXISTS Parts (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            PartName TEXT NOT NULL,
                            Type TEXT,
                            UnitPrice DECIMAL(10,2) NOT NULL DEFAULT 0,
                            CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                            UpdatedAt TEXT NOT NULL DEFAULT (datetime('now'))
                        )
                    ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not create Parts table: {ex.Message}");
            }
        }

        private void CreateStatutoryFeesTableIfNotExists()
        {
            try
            {
                _context.Database.ExecuteSqlRaw(@"
                        CREATE TABLE IF NOT EXISTS StatutoryFees (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            VehicleCategoryId INTEGER NOT NULL,
                            FeeType TEXT NOT NULL,
                            Amount DECIMAL(10,2) NOT NULL,
                            IsTaxable INTEGER NOT NULL DEFAULT 0,
                            EffectiveFrom TEXT NOT NULL,
                            EffectiveTo TEXT,
                            CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                            UpdatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                            FOREIGN KEY (VehicleCategoryId) REFERENCES VehicleCategories(Id)
                        )
                    ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not create StatutoryFees table: {ex.Message}");
            }
        }

        private void CreateInvoicesTableIfNotExists()
        {
            try
            {
                _context.Database.ExecuteSqlRaw(@"
                        CREATE TABLE IF NOT EXISTS Invoices (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            InvoiceNumber TEXT NOT NULL UNIQUE,
                            ClientId INTEGER NOT NULL,
                            VehicleId INTEGER NOT NULL,
                            InvoiceDate TEXT NOT NULL,
                            WorkCompletedDate TEXT NOT NULL,
                            NextInspectionDate TEXT,
                            Mileage INTEGER,
                            TaxableSubTotal DECIMAL(10,2) NOT NULL DEFAULT 0,
                            NonTaxableSubTotal DECIMAL(10,2) NOT NULL DEFAULT 0,
                            TaxRate DECIMAL(5,2) NOT NULL DEFAULT 10.0,
                            Tax DECIMAL(10,2) NOT NULL DEFAULT 0,
                            Total DECIMAL(10,2) NOT NULL DEFAULT 0,
                            Notes TEXT,
                            CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                            UpdatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                            FOREIGN KEY (ClientId) REFERENCES Clients(Id),
                            FOREIGN KEY (VehicleId) REFERENCES Vehicles(Id)
                        )
                    ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not create Invoices table: {ex.Message}");
            }
        }

        private void CreateInvoiceDetailsTableIfNotExists()
        {
            try
            {
                _context.Database.ExecuteSqlRaw(@"
                        CREATE TABLE IF NOT EXISTS InvoiceDetails (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            InvoiceId INTEGER NOT NULL,
                            PartId INTEGER,
                            ItemName TEXT NOT NULL,
                            Type TEXT,
                            RepairMethod TEXT,
                            Quantity DECIMAL(10,2) NOT NULL DEFAULT 1,
                            UnitPrice DECIMAL(10,2) NOT NULL DEFAULT 0,
                            LaborCost DECIMAL(10,2) NOT NULL DEFAULT 0,
                            IsTaxable INTEGER NOT NULL DEFAULT 1,
                            DisplayOrder INTEGER NOT NULL DEFAULT 0,
                            CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                            FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id) ON DELETE CASCADE,
                            FOREIGN KEY (PartId) REFERENCES Parts(Id)
                        )
                    ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not create InvoiceDetails table: {ex.Message}");
            }
        }

        private void CreateInitialVehicleCategories()
        {
            try
            {
                var categoriesExist = _context.VehicleCategories.Any();
                
                if (!categoriesExist)
                {
                    var categories = new List<VehicleCategory>
                    {
                        new VehicleCategory { CategoryName = "軽自動車", Description = "軽自動車全般", DisplayOrder = 1 },
                        new VehicleCategory { CategoryName = "小型車", Description = "車両重量1.0t以下", DisplayOrder = 2 },
                        new VehicleCategory { CategoryName = "普通車", Description = "車両重量1.5t以下", DisplayOrder = 3 },
                        new VehicleCategory { CategoryName = "中型車", Description = "車両重量2.0t以下", DisplayOrder = 4 },
                        new VehicleCategory { CategoryName = "大型車", Description = "車両重量2.0t超", DisplayOrder = 5 }
                    };

                    _context.VehicleCategories.AddRange(categories);
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not create initial vehicle categories: {ex.Message}");
            }
        }

        private void CreateInitialStatutoryFees()
        {
            try
            {
                var feesExist = _context.StatutoryFees.Any();
                
                if (!feesExist)
                {
                    var fees = new List<StatutoryFee>();
                    var effectiveDate = new DateTime(2023, 4, 1);

                    // 軽自動車
                    fees.Add(new StatutoryFee { VehicleCategoryId = 1, FeeType = "自賠責保険（24ヶ月）", Amount = 17540, EffectiveFrom = effectiveDate });
                    fees.Add(new StatutoryFee { VehicleCategoryId = 1, FeeType = "自賠責保険（25ヶ月）", Amount = 18040, EffectiveFrom = effectiveDate });
                    fees.Add(new StatutoryFee { VehicleCategoryId = 1, FeeType = "重量税", Amount = 8200, EffectiveFrom = effectiveDate });
                    fees.Add(new StatutoryFee { VehicleCategoryId = 1, FeeType = "印紙代", Amount = 1800, EffectiveFrom = effectiveDate });

                    // 小型車
                    fees.Add(new StatutoryFee { VehicleCategoryId = 2, FeeType = "自賠責保険（24ヶ月）", Amount = 17650, EffectiveFrom = effectiveDate });
                    fees.Add(new StatutoryFee { VehicleCategoryId = 2, FeeType = "自賠責保険（25ヶ月）", Amount = 18160, EffectiveFrom = effectiveDate });
                    fees.Add(new StatutoryFee { VehicleCategoryId = 2, FeeType = "重量税", Amount = 16400, EffectiveFrom = effectiveDate });
                    fees.Add(new StatutoryFee { VehicleCategoryId = 2, FeeType = "印紙代", Amount = 1800, EffectiveFrom = effectiveDate });

                    // 普通車
                    fees.Add(new StatutoryFee { VehicleCategoryId = 3, FeeType = "自賠責保険（24ヶ月）", Amount = 20010, EffectiveFrom = effectiveDate });
                    fees.Add(new StatutoryFee { VehicleCategoryId = 3, FeeType = "自賠責保険（25ヶ月）", Amount = 20610, EffectiveFrom = effectiveDate });
                    fees.Add(new StatutoryFee { VehicleCategoryId = 3, FeeType = "重量税", Amount = 34200, EffectiveFrom = effectiveDate });
                    fees.Add(new StatutoryFee { VehicleCategoryId = 3, FeeType = "印紙代", Amount = 1700, EffectiveFrom = effectiveDate });

                    _context.StatutoryFees.AddRange(fees);
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not create initial statutory fees: {ex.Message}");
            }
        }

        private void CreateInitialParts()
        {
            try
            {
                var partsExist = _context.Parts.Any();
                
                if (!partsExist)
                {
                    var parts = new List<Part>
                    {
                        // オイル関連
                        new Part { PartName = "エンジンオイル（4L）", Type = "オイル", UnitPrice = 3500 },
                        new Part { PartName = "エンジンオイル（3L）", Type = "オイル", UnitPrice = 2700 },
                        new Part { PartName = "オイルフィルター", Type = "フィルター", UnitPrice = 1500 },
                        new Part { PartName = "ATFオイル", Type = "オイル", UnitPrice = 2000 },
                        new Part { PartName = "ブレーキオイル", Type = "オイル", UnitPrice = 1500 },
                        
                        // フィルター類
                        new Part { PartName = "エアフィルター", Type = "フィルター", UnitPrice = 2500 },
                        new Part { PartName = "エアコンフィルター", Type = "フィルター", UnitPrice = 2000 },
                        
                        // タイヤ関連
                        new Part { PartName = "タイヤ（155/65R14）", Type = "タイヤ", UnitPrice = 6000 },
                        new Part { PartName = "タイヤ（175/65R14）", Type = "タイヤ", UnitPrice = 7000 },
                        new Part { PartName = "タイヤ（195/65R15）", Type = "タイヤ", UnitPrice = 8500 },
                        new Part { PartName = "タイヤ（205/60R16）", Type = "タイヤ", UnitPrice = 10000 },
                        
                        // ブレーキ関連
                        new Part { PartName = "ブレーキパッド（フロント）", Type = "ブレーキ", UnitPrice = 8000 },
                        new Part { PartName = "ブレーキパッド（リア）", Type = "ブレーキ", UnitPrice = 6000 },
                        new Part { PartName = "ブレーキディスク（フロント）", Type = "ブレーキ", UnitPrice = 12000 },
                        new Part { PartName = "ブレーキディスク（リア）", Type = "ブレーキ", UnitPrice = 10000 },
                        
                        // バッテリー
                        new Part { PartName = "バッテリー（40B19L）", Type = "バッテリー", UnitPrice = 8000 },
                        new Part { PartName = "バッテリー（60B24L）", Type = "バッテリー", UnitPrice = 12000 },
                        new Part { PartName = "バッテリー（80D23L）", Type = "バッテリー", UnitPrice = 15000 },
                        
                        // ワイパー
                        new Part { PartName = "ワイパーブレード（運転席）", Type = "ワイパー", UnitPrice = 1500 },
                        new Part { PartName = "ワイパーブレード（助手席）", Type = "ワイパー", UnitPrice = 1200 },
                        new Part { PartName = "ワイパーブレード（リア）", Type = "ワイパー", UnitPrice = 1000 },
                        
                        // ランプ類
                        new Part { PartName = "ヘッドライトバルブ（H4）", Type = "ランプ", UnitPrice = 1500 },
                        new Part { PartName = "ヘッドライトバルブ（HID）", Type = "ランプ", UnitPrice = 8000 },
                        new Part { PartName = "ブレーキランプバルブ", Type = "ランプ", UnitPrice = 500 },
                        new Part { PartName = "ウインカーバルブ", Type = "ランプ", UnitPrice = 500 },
                        
                        // その他消耗品
                        new Part { PartName = "スパークプラグ", Type = "点火系", UnitPrice = 800 },
                        new Part { PartName = "クーラント", Type = "冷却系", UnitPrice = 1500 },
                        new Part { PartName = "ウォッシャー液", Type = "その他", UnitPrice = 500 },
                        
                        // 工賃項目（部品ではないが、請求書で使用）
                        new Part { PartName = "一般整備工賃", Type = "工賃", UnitPrice = 0 },
                        
                        // 車検関連（軽自動車）
                        new Part { PartName = "車検整備費用（軽）", Type = "車検", UnitPrice = 16000 },
                        new Part { PartName = "24か月点検（軽）", Type = "点検", UnitPrice = 4700 },
                        new Part { PartName = "下廻り（スチーム洗車）", Type = "車検", UnitPrice = 2000 },
                        new Part { PartName = "下廻り（錆止め塗装）（軽）", Type = "車検", UnitPrice = 3000 },
                        new Part { PartName = "車検ライン通し（軽）", Type = "車検", UnitPrice = 5000 },
                        new Part { PartName = "車検代行料（軽）", Type = "車検", UnitPrice = 8500 },
                        new Part { PartName = "スーパークーラント交換 5L", Type = "冷却系", UnitPrice = 4000 },
                        new Part { PartName = "ブレーキフルード交換 1L", Type = "ブレーキ", UnitPrice = 2000 },
                        new Part { PartName = "フロント・リアブレーキ O/H サービス", Type = "ブレーキ", UnitPrice = 0 },
                        
                        // 車検関連（普通車）
                        new Part { PartName = "車検整備費用（普通車）", Type = "車検", UnitPrice = 20000 },
                        new Part { PartName = "24か月点検（普通車）", Type = "点検", UnitPrice = 6000 },
                        new Part { PartName = "下廻り（錆止め塗装）（普通車）", Type = "車検", UnitPrice = 4000 },
                        new Part { PartName = "車検ライン通し（普通車）", Type = "車検", UnitPrice = 5000 },
                        new Part { PartName = "車検代行料（普通車）", Type = "車検", UnitPrice = 8500 },
                        new Part { PartName = "スーパークーラント交換 6L", Type = "冷却系", UnitPrice = 4800 },
                        
                        // タイヤ（詳細）
                        new Part { PartName = "タイヤ 155/65R14 ネクストリー", Type = "タイヤ", UnitPrice = 8500 },
                        new Part { PartName = "タイヤ 155/65R14 エコピア NH200", Type = "タイヤ", UnitPrice = 11400 },
                        new Part { PartName = "タイヤ 145/80R12 貨物 K370", Type = "タイヤ", UnitPrice = 6000 },
                        new Part { PartName = "タイヤ入替・バランス", Type = "工賃", UnitPrice = 1500 },
                        new Part { PartName = "エアーバルブ", Type = "タイヤ", UnitPrice = 500 },
                        new Part { PartName = "廃タイヤ処分", Type = "その他", UnitPrice = 500 },
                        
                        // 足回り工賃（軽自動車）
                        new Part { PartName = "タイロッド工賃（軽）", Type = "工賃", UnitPrice = 3000 },
                        new Part { PartName = "ロアーム工賃（軽）", Type = "工賃", UnitPrice = 3000 },
                        
                        // 足回り工賃（普通車）
                        new Part { PartName = "タイロッド工賃（普通車）", Type = "工賃", UnitPrice = 4000 },
                        new Part { PartName = "ロアーム工賃（普通車）", Type = "工賃", UnitPrice = 4000 },
                        
                        // 管理費
                        new Part { PartName = "証紙管理費", Type = "手数料", UnitPrice = 400 }
                    };

                    _context.Parts.AddRange(parts);
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not create initial parts: {ex.Message}");
            }
        }

        private void CreateIssuerInfoTableIfNotExists()
        {
            try
            {
                _context.Database.ExecuteSqlRaw(@"
                        CREATE TABLE IF NOT EXISTS IssuerInfos (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            PostalCode TEXT NOT NULL,
                            Address TEXT NOT NULL,
                            CompanyName TEXT NOT NULL,
                            Position TEXT,
                            Name TEXT NOT NULL,
                            PhoneNumber TEXT NOT NULL,
                            FaxNumber TEXT,
                            Bank1Name TEXT,
                            Bank1BranchName TEXT,
                            Bank1AccountType TEXT,
                            Bank1AccountNumber TEXT,
                            Bank1AccountHolder TEXT,
                            Bank2Name TEXT,
                            Bank2BranchName TEXT,
                            Bank2AccountType TEXT,
                            Bank2AccountNumber TEXT,
                            Bank2AccountHolder TEXT
                        )
                    ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not create IssuerInfos table: {ex.Message}");
            }
        }
    }
}