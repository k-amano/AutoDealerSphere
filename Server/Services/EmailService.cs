using AutoDealerSphere.Server.Exceptions;
using MailKit.Net.Smtp;
using MailKit.Security;
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

        public async Task SendPasswordResetCodeAsync(string toEmail, string code)
        {
            var subject = "パスワードリセットの認証コード";
            var body = $@"
                <h2>パスワードリセットの認証コード</h2>
                <p>パスワードリセットのリクエストを受け付けました。</p>
                <p>アプリの「パスワードを忘れた場合」画面に、以下の認証コードを入力してください：</p>
                <p style='font-size: 28px; font-weight: bold; letter-spacing: 6px;'>{code}</p>
                <p>このコードは15分間有効です。</p>
                <p>※このメールに心当たりがない場合は、無視してください。</p>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var settings = await _settingsService.GetSettingsAsync();
            if (settings == null)
            {
                throw new BusinessException("メール設定が登録されていないため、メールを送信できません。発行者情報画面でメール設定を登録してください。");
            }

            var password = await _settingsService.DecryptPasswordAsync(settings.EncryptedPassword);
            if (password == null)
            {
                throw new BusinessException("メール設定のパスワードを復号できませんでした。発行者情報画面でメール設定のパスワードを再登録してください。");
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(settings.SenderName, settings.SenderEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;
            message.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

            using var client = new SmtpClient();
            try
            {
                var socketOptions = settings.SmtpPort == 465
                    ? SecureSocketOptions.SslOnConnect
                    : settings.EnableSsl
                        ? SecureSocketOptions.StartTls
                        : SecureSocketOptions.None;
                await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, socketOptions);
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
