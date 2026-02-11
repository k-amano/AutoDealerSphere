using AutoDealerSphere.Server.Services;
using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoDealerSphere.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PasswordResetController : ControllerBase
    {
        private readonly SQLDBContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<PasswordResetController> _logger;

        public PasswordResetController(
            SQLDBContext context,
            IEmailService emailService,
            ILogger<PasswordResetController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("request")]
        public async Task<ActionResult> RequestPasswordReset([FromBody] PasswordResetRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                // セキュリティのため、ユーザーが存在しない場合も同じメッセージを返す
                if (user != null)
                {
                    // 既存の未使用トークンを無効化
                    var existingTokens = await _context.PasswordResetTokens
                        .Where(t => t.UserId == user.Id && !t.IsUsed)
                        .ToListAsync();

                    foreach (var token in existingTokens)
                    {
                        token.IsUsed = true;
                    }

                    // 新しいトークンを作成
                    var resetToken = new PasswordResetToken
                    {
                        UserId = user.Id,
                        Token = Guid.NewGuid().ToString(),
                        CreatedAt = DateTime.Now,
                        ExpiresAt = DateTime.Now.AddHours(1),
                        IsUsed = false
                    };

                    _context.PasswordResetTokens.Add(resetToken);
                    await _context.SaveChangesAsync();

                    // メール送信
                    var resetUrl = $"{request.BaseUrl}/reset-password";
                    await _emailService.SendPasswordResetEmailAsync(
                        user.Email,
                        resetToken.Token,
                        resetUrl);

                    _logger.LogInformation($"パスワードリセットメールを送信しました: {user.Email}");
                }

                // 常に同じメッセージを返す（情報漏洩防止）
                return Ok(new { message = "メールアドレスが登録されている場合、パスワードリセットのメールを送信しました。" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "パスワードリセット要求処理中にエラーが発生しました");
                return StatusCode(500, new { error = "メール送信に失敗しました。メール設定を確認してください。" });
            }
        }

        [HttpGet("verify/{token}")]
        public async Task<ActionResult> VerifyResetToken(string token)
        {
            var resetToken = await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed);

            if (resetToken == null)
            {
                return BadRequest(new { error = "無効なトークンです。" });
            }

            if (resetToken.ExpiresAt < DateTime.Now)
            {
                return BadRequest(new { error = "トークンの有効期限が切れています。" });
            }

            return Ok(new { valid = true, email = resetToken.User?.Email });
        }

        [HttpPost("reset")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var resetToken = await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == request.Token && !t.IsUsed);

            if (resetToken == null)
            {
                return BadRequest(new { error = "無効なトークンです。" });
            }

            if (resetToken.ExpiresAt < DateTime.Now)
            {
                return BadRequest(new { error = "トークンの有効期限が切れています。" });
            }

            if (resetToken.User == null)
            {
                return BadRequest(new { error = "ユーザーが見つかりません。" });
            }

            // パスワードを更新
            resetToken.User.Password = PasswordHashService.HashPassword(request.NewPassword);
            resetToken.IsUsed = true;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"パスワードがリセットされました: {resetToken.User.Email}");

            return Ok(new { message = "パスワードが正常にリセットされました。" });
        }
    }

    public class PasswordResetRequest
    {
        public string Email { get; set; } = "";
        public string BaseUrl { get; set; } = "";
    }

    public class ResetPasswordRequest
    {
        public string Token { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }
}
