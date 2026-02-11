using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;

namespace AutoDealerSphere.Server.Services
{
    public class EmailService : IEmailService
    {
        private readonly IEmailSettingsService _settingsService;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IEmailSettingsService settingsService, ILogger<EmailService> logger)
        {
            _settingsService = settingsService;
            _logger = logger;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken, string resetUrl)
        {
            var subject = "パスワードリセットのご依頼";
            var body = $@"
                <h2>パスワードリセットのご依頼</h2>
                <p>パスワードリセットのリクエストを受け付けました。</p>
                <p>以下のリンクをクリックして、新しいパスワードを設定してください：</p>
                <p><a href='{resetUrl}?token={resetToken}'>パスワードをリセット</a></p>
                <p>このリンクは1時間有効です。</p>
                <p>※このメールに心当たりがない場合は、無視してください。</p>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var settings = await _settingsService.GetSettingsAsync();
            if (settings == null)
            {
                throw new InvalidOperationException("メール設定が登録されていません。");
            }

            var password = await _settingsService.DecryptPasswordAsync(settings.EncryptedPassword);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(settings.SenderName, settings.SenderEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;
            message.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, settings.EnableSsl);
                await client.AuthenticateAsync(settings.Username, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                _logger.LogInformation($"メール送信成功: {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"メール送信失敗: {toEmail}");
                throw;
            }
        }

        public async Task<bool> SendTestEmailAsync(string toEmail)
        {
            try
            {
                var subject = "テストメール";
                var body = @"
                    <h2>テストメール</h2>
                    <p>これはAutoDealerSphereからのテストメールです。</p>
                    <p>メール設定が正しく動作しています。</p>
                ";
                await SendEmailAsync(toEmail, subject, body);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "テストメール送信に失敗しました");
                return false;
            }
        }
    }
}
