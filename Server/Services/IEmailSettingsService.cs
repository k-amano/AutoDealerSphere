using AutoDealerSphere.Shared.Models;

namespace AutoDealerSphere.Server.Services
{
    public interface IEmailSettingsService
    {
        Task<EmailSettings?> GetSettingsAsync();
        Task<EmailSettings> CreateOrUpdateSettingsAsync(EmailSettings settings, string plainPassword);
        /// <summary>復号に失敗した場合（暗号化キー喪失など）は null を返す。</summary>
        Task<string?> DecryptPasswordAsync(string encryptedPassword);
        Task<(bool Success, string Message)> TestConnectionAsync(EmailSettings settings, string plainPassword);
    }
}
