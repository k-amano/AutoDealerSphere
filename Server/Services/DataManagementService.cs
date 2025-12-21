using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoDealerSphere.Shared.Models;

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

                // Create backup object
                var backupData = new
                {
                    Version = "1.0",
                    Timestamp = DateTime.UtcNow,
                    Database = "crm01",
                    Tables = new
                    {
                        Users = users,
                        Clients = clients,
                        Vehicles = vehicles,
                        Parts = parts,
                        VehicleCategories = vehicleCategories,
                        StatutoryFees = statutoryFees,
                        Invoices = invoices,
                        InvoiceDetails = invoiceDetails,
                        IssuerInfos = issuerInfos
                    }
                };

                // Serialize to JSON
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var json = JsonSerializer.Serialize(backupData, options);

                return new BackupResult
                {
                    Success = true,
                    Message = "バックアップが完了しました。",
                    BackupJson = json,
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

        public async Task<RestoreResult> RestoreFromBackupAsync(Stream backupStream)
        {
            try
            {
                // Read and parse JSON
                using var reader = new StreamReader(backupStream);
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
                    if (backupData.Tables.Users != null && backupData.Tables.Users.Count > 0)
                    {
                        await context.Users.AddRangeAsync(backupData.Tables.Users);
                        await context.SaveChangesAsync();
                    }

                    if (backupData.Tables.VehicleCategories != null && backupData.Tables.VehicleCategories.Count > 0)
                    {
                        await context.VehicleCategories.AddRangeAsync(backupData.Tables.VehicleCategories);
                        await context.SaveChangesAsync();
                    }

                    if (backupData.Tables.Clients != null && backupData.Tables.Clients.Count > 0)
                    {
                        await context.Clients.AddRangeAsync(backupData.Tables.Clients);
                        await context.SaveChangesAsync();
                    }

                    if (backupData.Tables.Parts != null && backupData.Tables.Parts.Count > 0)
                    {
                        await context.Parts.AddRangeAsync(backupData.Tables.Parts);
                        await context.SaveChangesAsync();
                    }

                    if (backupData.Tables.Vehicles != null && backupData.Tables.Vehicles.Count > 0)
                    {
                        await context.Vehicles.AddRangeAsync(backupData.Tables.Vehicles);
                        await context.SaveChangesAsync();
                    }

                    if (backupData.Tables.StatutoryFees != null && backupData.Tables.StatutoryFees.Count > 0)
                    {
                        await context.StatutoryFees.AddRangeAsync(backupData.Tables.StatutoryFees);
                        await context.SaveChangesAsync();
                    }

                    if (backupData.Tables.Invoices != null && backupData.Tables.Invoices.Count > 0)
                    {
                        await context.Invoices.AddRangeAsync(backupData.Tables.Invoices);
                        await context.SaveChangesAsync();
                    }

                    if (backupData.Tables.InvoiceDetails != null && backupData.Tables.InvoiceDetails.Count > 0)
                    {
                        await context.InvoiceDetails.AddRangeAsync(backupData.Tables.InvoiceDetails);
                        await context.SaveChangesAsync();
                    }

                    if (backupData.Tables.IssuerInfos != null && backupData.Tables.IssuerInfos.Count > 0)
                    {
                        await context.IssuerInfos.AddRangeAsync(backupData.Tables.IssuerInfos);
                        await context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();

                    return new RestoreResult
                    {
                        Success = true,
                        Message = "レストアが完了しました。",
                        Statistics = new RestoreStatistics
                        {
                            UsersCount = backupData.Tables.Users?.Count ?? 0,
                            ClientsCount = backupData.Tables.Clients?.Count ?? 0,
                            VehiclesCount = backupData.Tables.Vehicles?.Count ?? 0,
                            PartsCount = backupData.Tables.Parts?.Count ?? 0,
                            VehicleCategoriesCount = backupData.Tables.VehicleCategories?.Count ?? 0,
                            StatutoryFeesCount = backupData.Tables.StatutoryFees?.Count ?? 0,
                            InvoicesCount = backupData.Tables.Invoices?.Count ?? 0,
                            InvoiceDetailsCount = backupData.Tables.InvoiceDetails?.Count ?? 0,
                            IssuerInfosCount = backupData.Tables.IssuerInfos?.Count ?? 0
                        }
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"レストア処理中にエラーが発生しました: {ex.Message}", ex);
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
    }

    // Helper classes for deserialization
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
