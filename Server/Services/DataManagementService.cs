using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AutoDealerSphere.Shared.Models;
using CsvHelper;
using CsvHelper.Configuration;

namespace AutoDealerSphere.Server.Services
{
    public class DataManagementService : IDataManagementService
    {
        private readonly IDbContextFactory<SQLDBContext> _contextFactory;

        public DataManagementService(IDbContextFactory<SQLDBContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<BackupResult> CreateBackupAsync()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Fetch all data from all tables
                var users = await context.Users.ToListAsync();
                var clients = await context.Clients.ToListAsync();
                var vehicles = await context.Vehicles.ToListAsync();
                var parts = await context.Parts.ToListAsync();
                var vehicleCategories = await context.VehicleCategories.ToListAsync();
                var statutoryFees = await context.StatutoryFees.ToListAsync();
                var invoices = await context.Invoices.ToListAsync();
                var invoiceDetails = await context.InvoiceDetails.ToListAsync();
                var issuerInfos = await context.IssuerInfos.ToListAsync();

                // Create ZIP archive in memory
                using var zipStream = new MemoryStream();
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                {
                    // Create meta.json
                    var meta = new
                    {
                        version = "2.0",
                        format = "csv-zip",
                        timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        database = "crm01",
                        statistics = new
                        {
                            users = users.Count,
                            clients = clients.Count,
                            vehicles = vehicles.Count,
                            parts = parts.Count,
                            categories = vehicleCategories.Count,
                            fees = statutoryFees.Count,
                            invoices = invoices.Count,
                            details = invoiceDetails.Count,
                            issuer = issuerInfos.Count
                        }
                    };

                    var metaEntry = archive.CreateEntry("meta.json");
                    using (var metaWriter = new StreamWriter(metaEntry.Open()))
                    {
                        await metaWriter.WriteAsync(JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true }));
                    }

                    // Create CSV configuration (UTF-8 with BOM)
                    var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        Encoding = new UTF8Encoding(true), // UTF-8 with BOM
                        NewLine = "\r\n"
                    };

                    // Write each table to CSV
                    await WriteCsvToZip(archive, "users.csv", users, csvConfig);
                    await WriteCsvToZip(archive, "clients.csv", clients, csvConfig);
                    await WriteCsvToZip(archive, "vehicles.csv", vehicles, csvConfig);
                    await WriteCsvToZip(archive, "parts.csv", parts, csvConfig);
                    await WriteCsvToZip(archive, "categories.csv", vehicleCategories, csvConfig);
                    await WriteCsvToZip(archive, "fees.csv", statutoryFees, csvConfig);
                    await WriteCsvToZip(archive, "invoices.csv", invoices, csvConfig);
                    await WriteCsvToZip(archive, "details.csv", invoiceDetails, csvConfig);
                    await WriteCsvToZip(archive, "issuer.csv", issuerInfos, csvConfig);
                }

                // Convert ZIP to Base64 for download
                zipStream.Position = 0;
                var zipBytes = zipStream.ToArray();
                var base64Zip = Convert.ToBase64String(zipBytes);

                return new BackupResult
                {
                    Success = true,
                    Message = "バックアップが完了しました。",
                    BackupData = base64Zip,
                    Statistics = new BackupStatistics
                    {
                        UsersCount = users.Count,
                        ClientsCount = clients.Count,
                        VehiclesCount = vehicles.Count,
                        PartsCount = parts.Count,
                        VehicleCategoriesCount = vehicleCategories.Count,
                        StatutoryFeesCount = statutoryFees.Count,
                        InvoicesCount = invoices.Count,
                        InvoiceDetailsCount = invoiceDetails.Count,
                        IssuerInfosCount = issuerInfos.Count
                    }
                };
            }
            catch (Exception ex)
            {
                return new BackupResult
                {
                    Success = false,
                    Message = "バックアップに失敗しました。",
                    Error = ex.Message
                };
            }
        }

        private async Task WriteCsvToZip<T>(ZipArchive archive, string fileName, List<T> data, CsvConfiguration config)
        {
            var entry = archive.CreateEntry(fileName);
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream, config.Encoding);
            using var csv = new CsvWriter(writer, config);
            await csv.WriteRecordsAsync(data);
        }

        public async Task<RestoreResult> RestoreFromBackupAsync(Stream backupStream)
        {
            try
            {
                // Check if it's a ZIP file (new format) or JSON (legacy)
                backupStream.Position = 0;
                var buffer = new byte[4];
                await backupStream.ReadAsync(buffer, 0, 4);
                backupStream.Position = 0;

                // ZIP file starts with PK (0x504B)
                bool isZipFile = buffer[0] == 0x50 && buffer[1] == 0x4B;

                if (isZipFile)
                {
                    return await RestoreFromCsvZipAsync(backupStream);
                }
                else
                {
                    return await RestoreFromJsonAsync(backupStream);
                }
            }
            catch (Exception ex)
            {
                return new RestoreResult
                {
                    Success = false,
                    Message = "レストアに失敗しました。",
                    Error = ex.Message
                };
            }
        }

        private async Task<RestoreResult> RestoreFromCsvZipAsync(Stream zipStream)
        {
            try
            {
                using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

                // Read meta.json to verify format
                var metaEntry = archive.GetEntry("meta.json");
                if (metaEntry == null)
                {
                    return new RestoreResult
                    {
                        Success = false,
                        Message = "無効なバックアップファイルです。",
                        Error = "meta.jsonが見つかりません。"
                    };
                }

                // CSV configuration for reading
                var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Encoding = new UTF8Encoding(true),
                    BadDataFound = null // Ignore bad data
                };

                // Read CSV files
                var users = await ReadCsvFromZip<User>(archive, "users.csv", csvConfig);
                var clients = await ReadCsvFromZip<AutoDealerSphere.Shared.Models.Client>(archive, "clients.csv", csvConfig);
                var vehicles = await ReadCsvFromZip<Vehicle>(archive, "vehicles.csv", csvConfig);
                var parts = await ReadCsvFromZip<Part>(archive, "parts.csv", csvConfig);
                var categories = await ReadCsvFromZip<VehicleCategory>(archive, "categories.csv", csvConfig);
                var fees = await ReadCsvFromZip<StatutoryFee>(archive, "fees.csv", csvConfig);
                var invoices = await ReadCsvFromZip<Invoice>(archive, "invoices.csv", csvConfig);
                var details = await ReadCsvFromZip<InvoiceDetail>(archive, "details.csv", csvConfig);
                var issuerInfos = await ReadCsvFromZip<IssuerInfo>(archive, "issuer.csv", csvConfig);

                // Restore data to database
                return await RestoreToDatabase(users, clients, vehicles, parts, categories, fees, invoices, details, issuerInfos);
            }
            catch (Exception ex)
            {
                return new RestoreResult
                {
                    Success = false,
                    Message = "CSV形式のレストアに失敗しました。",
                    Error = ex.Message
                };
            }
        }

        private async Task<List<T>> ReadCsvFromZip<T>(ZipArchive archive, string fileName, CsvConfiguration config)
        {
            var entry = archive.GetEntry(fileName);
            if (entry == null) return new List<T>();

            using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream, config.Encoding);
            using var csv = new CsvReader(reader, config);
            var records = csv.GetRecords<T>();
            return records.ToList();
        }

        private async Task<RestoreResult> RestoreFromJsonAsync(Stream jsonStream)
        {
            try
            {
                // Read and parse JSON (legacy format)
                using var reader = new StreamReader(jsonStream);
                var json = await reader.ReadToEndAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var backupData = JsonSerializer.Deserialize<BackupData>(json, options);

                if (backupData?.Tables == null)
                {
                    return new RestoreResult
                    {
                        Success = false,
                        Message = "無効なバックアップファイルです。",
                        Error = "バックアップデータの形式が正しくありません。"
                    };
                }

                return await RestoreToDatabase(
                    backupData.Tables.Users ?? new List<User>(),
                    backupData.Tables.Clients ?? new List<AutoDealerSphere.Shared.Models.Client>(),
                    backupData.Tables.Vehicles ?? new List<Vehicle>(),
                    backupData.Tables.Parts ?? new List<Part>(),
                    backupData.Tables.VehicleCategories ?? new List<VehicleCategory>(),
                    backupData.Tables.StatutoryFees ?? new List<StatutoryFee>(),
                    backupData.Tables.Invoices ?? new List<Invoice>(),
                    backupData.Tables.InvoiceDetails ?? new List<InvoiceDetail>(),
                    backupData.Tables.IssuerInfos ?? new List<IssuerInfo>()
                );
            }
            catch (Exception ex)
            {
                return new RestoreResult
                {
                    Success = false,
                    Message = "JSON形式のレストアに失敗しました。",
                    Error = ex.Message
                };
            }
        }

        private async Task<RestoreResult> RestoreToDatabase(
            List<User> users,
            List<AutoDealerSphere.Shared.Models.Client> clients,
            List<Vehicle> vehicles,
            List<Part> parts,
            List<VehicleCategory> categories,
            List<StatutoryFee> fees,
            List<Invoice> invoices,
            List<InvoiceDetail> details,
            List<IssuerInfo> issuerInfos)
        {
            using var context = _contextFactory.CreateDbContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // Delete all existing data in correct order (respecting foreign keys)
                // Delete child tables first
                await context.InvoiceDetails.ExecuteDeleteAsync();
                await context.Invoices.ExecuteDeleteAsync();
                await context.StatutoryFees.ExecuteDeleteAsync();
                await context.Vehicles.ExecuteDeleteAsync();
                await context.Clients.ExecuteDeleteAsync();
                await context.Users.ExecuteDeleteAsync();
                await context.Parts.ExecuteDeleteAsync();
                await context.VehicleCategories.ExecuteDeleteAsync();
                await context.IssuerInfos.ExecuteDeleteAsync();

                // Reset auto-increment counters for SQLite
                await context.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence");

                // Insert data in correct order (parent tables first)
                if (users.Count > 0)
                {
                    await context.Users.AddRangeAsync(users);
                    await context.SaveChangesAsync();
                }

                if (categories.Count > 0)
                {
                    await context.VehicleCategories.AddRangeAsync(categories);
                    await context.SaveChangesAsync();
                }

                if (clients.Count > 0)
                {
                    await context.Clients.AddRangeAsync(clients);
                    await context.SaveChangesAsync();
                }

                if (parts.Count > 0)
                {
                    await context.Parts.AddRangeAsync(parts);
                    await context.SaveChangesAsync();
                }

                if (vehicles.Count > 0)
                {
                    await context.Vehicles.AddRangeAsync(vehicles);
                    await context.SaveChangesAsync();
                }

                if (fees.Count > 0)
                {
                    await context.StatutoryFees.AddRangeAsync(fees);
                    await context.SaveChangesAsync();
                }

                if (invoices.Count > 0)
                {
                    await context.Invoices.AddRangeAsync(invoices);
                    await context.SaveChangesAsync();
                }

                if (details.Count > 0)
                {
                    await context.InvoiceDetails.AddRangeAsync(details);
                    await context.SaveChangesAsync();
                }

                if (issuerInfos.Count > 0)
                {
                    await context.IssuerInfos.AddRangeAsync(issuerInfos);
                    await context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return new RestoreResult
                {
                    Success = true,
                    Message = "レストアが完了しました。",
                    Statistics = new RestoreStatistics
                    {
                        UsersCount = users.Count,
                        ClientsCount = clients.Count,
                        VehiclesCount = vehicles.Count,
                        PartsCount = parts.Count,
                        VehicleCategoriesCount = categories.Count,
                        StatutoryFeesCount = fees.Count,
                        InvoicesCount = invoices.Count,
                        InvoiceDetailsCount = details.Count,
                        IssuerInfosCount = issuerInfos.Count
                    }
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"レストア処理中にエラーが発生しました: {ex.Message}", ex);
            }
        }
    }

    // Helper classes for deserialization (legacy JSON format)
    public class BackupData
    {
        public string? Version { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Database { get; set; }
        public BackupTables? Tables { get; set; }
    }

    public class BackupTables
    {
        public List<User>? Users { get; set; }
        public List<AutoDealerSphere.Shared.Models.Client>? Clients { get; set; }
        public List<Vehicle>? Vehicles { get; set; }
        public List<Part>? Parts { get; set; }
        public List<VehicleCategory>? VehicleCategories { get; set; }
        public List<StatutoryFee>? StatutoryFees { get; set; }
        public List<Invoice>? Invoices { get; set; }
        public List<InvoiceDetail>? InvoiceDetails { get; set; }
        public List<IssuerInfo>? IssuerInfos { get; set; }
    }
}
