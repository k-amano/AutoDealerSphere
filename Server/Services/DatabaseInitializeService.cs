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

                // Vehiclesテーブルが存在しない場合は作成
                CreateVehiclesTableIfNotExists();

                // Usersテーブルが存在しない場合は作成
                CreateUsersTableIfNotExists();

                // 初期管理者ユーザーを作成（存在しない場合のみ）
                CreateInitialAdminUser();
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
                var tableExists = _context.Database.ExecuteSqlRaw(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Clients'") > 0;

                if (!tableExists)
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
                var tableExists = _context.Database.ExecuteSqlRaw(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Vehicles'") > 0;

                if (!tableExists)
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
                            CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                            UpdatedAt TEXT,
                            FOREIGN KEY (ClientId) REFERENCES Clients(Id) ON DELETE CASCADE
                        )
                    ");
                }
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
                var usersTableExists = _context.Database.ExecuteSqlRaw(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Users'") > 0;

                if (!usersTableExists)
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
    }
}