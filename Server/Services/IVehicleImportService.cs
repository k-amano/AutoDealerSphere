using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoDealerSphere.Server.Services
{
    public interface IVehicleImportService
    {
        Task<(int clientsImported, int vehiclesImported, List<string> errors)> ImportFromCsvAsync(string filePath);
    }
}