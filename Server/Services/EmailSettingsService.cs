using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using MailKit.Net.Smtp;

namespace AutoDealerSphere.Server.Services
{
    public class EmailSettingsService : IEmailSettingsService
    {
        private readonly SQLDBContext _context;
        private readonly IDataProtector _protector;
        private readonly ILogger<EmailSettingsService> _logger;

        public EmailSettingsService(
            SQLDBContext context,
            IDataProtectionProvider dataProtectionProvider,
            ILogger<EmailSettingsService> logger)
        {
            _context = context;
            _protector = dataProtectionProvider.CreateProtector("EmailSettings.Password");
            _logger = logger;
        }

        public async Task<EmailSettings?> GetSettingsAsync()
        {
            return await _context.EmailSettings.FirstOrDefaultAsync();
        }

        public async Task<EmailSettings> CreateOrUpdateSettingsAsync(EmailSettings settings, string plainPassword)
        {
            settings.EncryptedPassword = _protector.Protect(plainPassword);
            settings.UpdatedAt = DateTime.Now;

            var existing = await _context.EmailSettings.FirstOrDefaultAsync();

            if (existing == null)
            {
                _context.EmailSettings.Add(settings);
            }
            else
            {
                settings.Id = existing.Id;
                _context.Entry(existing).CurrentValues.SetValues(settings);
            }

            await _context.SaveChangesAsync();
            return settings;
        }

        public async Task<string> DecryptPasswordAsync(string encryptedPassword)
        {
            return await Task.FromResult(_protector.Unprotect(encryptedPassword));
        }

        public async Task<bool> TestConnectionAsync(EmailSettings settings, string plainPassword)
        {
            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, settings.EnableSsl);
                await client.AuthenticateAsync(settings.Username, plainPassword);
                await client.DisconnectAsync(true);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP接続テストに失敗しました");
                return false;
            }
        }
    }
}
