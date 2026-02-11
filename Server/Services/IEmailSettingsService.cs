using AutoDealerSphere.Shared.Models;

namespace AutoDealerSphere.Server.Services
{
    public interface IEmailSettingsService
    {
        Task<EmailSettings?> GetSettingsAsync();
        Task<EmailSettings> CreateOrUpdateSettingsAsync(EmailSettings settings, string plainPassword);
        Task<string> DecryptPasswordAsync(string encryptedPassword);
        Task<bool> TestConnectionAsync(EmailSettings settings, string plainPassword);
    }
}
