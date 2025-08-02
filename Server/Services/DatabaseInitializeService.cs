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

                // 初期データの作成
                CreateInitialAdminUser();
                CreateInitialVehicleCategories();
                CreateInitialStatutoryFees();
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
                            DisplayOrder INTEGER NOT NULL DEFAULT 0,
                            IsActive INTEGER NOT NULL DEFAULT 1
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
                            IsActive INTEGER NOT NULL DEFAULT 1,
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
                            IsActive INTEGER NOT NULL DEFAULT 1,
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
                // テーブルが存在するか確認
                var tableExists = _context.Database.ExecuteSqlRaw(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='VehicleCategories'") > 0;
                
                if (!tableExists) return;

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
                // テーブルが存在するか確認
                var tableExists = _context.Database.ExecuteSqlRaw(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='StatutoryFees'") > 0;
                
                if (!tableExists) return;

                var feesExist = _context.StatutoryFees.Any();
                
                if (!feesExist)
                {
                    var fees = new List<StatutoryFee>();
                    var effectiveDate = new DateTime(2023, 4, 1);

                    // 軽自動車
                    fees.Add(new StatutoryFee { VehicleCategoryId = 1, FeeType = "自賠責保険（24ヶ月）", Amount = 17540, EffectiveFrom = effectiveDate });
                    fees.Add(new StatutoryFee { VehicleCategoryId = 1, FeeType = "自賠責保険（25ヶ月）", Amount = 18040, EffectiveFrom = effectiveDate });
                    fees.Add(new StatutoryFee { VehicleCategoryId = 1, FeeType = "重量税", Amount = 6600, EffectiveFrom = effectiveDate });
                    fees.Add(new StatutoryFee { VehicleCategoryId = 1, FeeType = "印紙代", Amount = 1800, EffectiveFrom = effectiveDate });

                    // 小型車
                    fees.Add(new StatutoryFee { VehicleCategoryId = 2, FeeType = "自賠責保険（24ヶ月）", Amount = 17650, EffectiveFrom = effectiveDate });
                    fees.Add(new StatutoryFee { VehicleCategoryId = 2, FeeType = "自賠責保険（25ヶ月）", Amount = 18160, EffectiveFrom = effectiveDate });
                    fees.Add(new StatutoryFee { VehicleCategoryId = 2, FeeType = "重量税", Amount = 16400, EffectiveFrom = effectiveDate });
                    fees.Add(new StatutoryFee { VehicleCategoryId = 2, FeeType = "印紙代", Amount = 1800, EffectiveFrom = effectiveDate });

                    // 普通車
                    fees.Add(new StatutoryFee { VehicleCategoryId = 3, FeeType = "自賠責保険（24ヶ月）", Amount = 17650, EffectiveFrom = effectiveDate });
                    fees.Add(new StatutoryFee { VehicleCategoryId = 3, FeeType = "自賠責保険（25ヶ月）", Amount = 18160, EffectiveFrom = effectiveDate });
                    fees.Add(new StatutoryFee { VehicleCategoryId = 3, FeeType = "重量税", Amount = 24600, EffectiveFrom = effectiveDate });
                    fees.Add(new StatutoryFee { VehicleCategoryId = 3, FeeType = "印紙代", Amount = 1800, EffectiveFrom = effectiveDate });

                    _context.StatutoryFees.AddRange(fees);
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not create initial statutory fees: {ex.Message}");
            }
        }
    }
}