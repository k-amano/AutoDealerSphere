namespace AutoDealerSphere.Server.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string resetToken, string resetUrl);
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
        Task<bool> SendTestEmailAsync(string toEmail);
    }
}
