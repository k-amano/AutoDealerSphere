using AutoDealerSphere.Shared.Models;

namespace AutoDealerSphere.Server.Services
{
    public interface IPartService
    {
        Task<IEnumerable<Part>> GetAllPartsAsync();
        Task<Part?> GetPartByIdAsync(int id);
        Task<Part> CreatePartAsync(Part part);
        Task<bool> UpdatePartAsync(Part part);
        Task<bool> DeletePartAsync(int id);
    }
}