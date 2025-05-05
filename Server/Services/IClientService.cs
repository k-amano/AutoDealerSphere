using AutoDealerSphere.Shared.Models;
using System.Threading.Tasks;

namespace AutoDealerSphere.Server.Services
{
    public interface IClientService
    {
        Task<IEnumerable<AutoDealerSphere.Shared.Models.Client>> GetAllAsync();
        Task<AutoDealerSphere.Shared.Models.Client?> GetByIdAsync(int id);
        Task<AutoDealerSphere.Shared.Models.Client> CreateAsync(AutoDealerSphere.Shared.Models.Client client);
        Task<AutoDealerSphere.Shared.Models.Client?> UpdateAsync(AutoDealerSphere.Shared.Models.Client client);
        Task DeleteAsync(int id);
    }
}
