using System.IO;
using System.Threading.Tasks;

namespace AutoDealerSphere.Server.Services
{
    public interface IDataManagementService
    {
        Task<BackupResult> CreateBackupAsync();
        Task<RestoreResult> RestoreFromBackupAsync(Stream backupStream);
    }

    public class BackupResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? BackupJson { get; set; }
        public BackupStatistics? Statistics { get; set; }
        public string? Error { get; set; }
    }

    public class BackupStatistics
    {
        public int UsersCount { get; set; }
        public int ClientsCount { get; set; }
        public int VehiclesCount { get; set; }
        public int PartsCount { get; set; }
        public int InvoicesCount { get; set; }
        public int VehicleCategoriesCount { get; set; }
        public int StatutoryFeesCount { get; set; }
        public int InvoiceDetailsCount { get; set; }
        public int IssuerInfosCount { get; set; }
    }

    public class RestoreResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public RestoreStatistics? Statistics { get; set; }
        public string? Error { get; set; }
    }

    public class RestoreStatistics
    {
        public int UsersCount { get; set; }
        public int ClientsCount { get; set; }
        public int VehiclesCount { get; set; }
        public int PartsCount { get; set; }
        public int InvoicesCount { get; set; }
        public int VehicleCategoriesCount { get; set; }
        public int StatutoryFeesCount { get; set; }
        public int InvoiceDetailsCount { get; set; }
        public int IssuerInfosCount { get; set; }
    }
}
