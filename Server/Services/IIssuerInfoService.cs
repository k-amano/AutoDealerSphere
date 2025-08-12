using AutoDealerSphere.Shared.Models;

namespace AutoDealerSphere.Server.Services
{
    public interface IIssuerInfoService
    {
        Task<IssuerInfo> GetIssuerInfoAsync();
        Task<IssuerInfo> CreateOrUpdateIssuerInfoAsync(IssuerInfo issuerInfo);
    }
}