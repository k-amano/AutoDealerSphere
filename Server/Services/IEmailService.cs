namespace AutoDealerSphere.Server.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetCodeAsync(string toEmail, string code);
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
        Task<bool> SendTestEmailAsync(string toEmail);
    }
}
