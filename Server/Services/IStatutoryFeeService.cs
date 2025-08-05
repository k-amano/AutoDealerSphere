using AutoDealerSphere.Shared.Models;

namespace AutoDealerSphere.Server.Services
{
    public interface IStatutoryFeeService
    {
        Task<List<StatutoryFee>> GetAllAsync();
        Task<StatutoryFee?> GetByIdAsync(int id);
        Task<List<StatutoryFee>> GetByCategoryIdAsync(int categoryId);
        Task<StatutoryFee> CreateAsync(StatutoryFee statutoryFee);
        Task<StatutoryFee?> UpdateAsync(int id, StatutoryFee statutoryFee);
        Task<bool> DeleteAsync(int id);
    }
}