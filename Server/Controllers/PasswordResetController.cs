using AutoDealerSphere.Server.Exceptions;
using AutoDealerSphere.Server.Services;
using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace AutoDealerSphere.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PasswordResetController : ControllerBase
    {
        private const int MaxFailedAttempts = 5;

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

        /// <summary>パスワードリセット画面のユーザー選択用（名前のみ返す）</summary>
        [HttpGet("users")]
        public async Task<ActionResult> GetUsers()
        {
            var users = await _context.Users
                .OrderBy(u => u.Id)
                .Select(u => new { u.Id, u.Name })
                .ToListAsync();
            return Ok(users);
        }

        [HttpPost("request")]
        public async Task<ActionResult> RequestPasswordReset([FromBody] PasswordResetRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId);

            if (user == null)
            {
                throw new BusinessException("ユーザーが見つかりません。");
            }

            var existingTokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.Id && !t.IsUsed)
                .ToListAsync();

            foreach (var token in existingTokens)
            {
                token.IsUsed = true;
            }

            var code = RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6");
            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Token = code,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddMinutes(15),
                IsUsed = false
            };

            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            // 送り先は本人の登録メールアドレス固定（画面から指定はできない）
            await _emailService.SendPasswordResetCodeAsync(user.Email, code);

            _logger.LogInformation("パスワードリセット用認証コードを送信しました: {Email}", user.Email);

            return Ok(new { maskedEmail = MaskEmail(user.Email) });
        }

        [HttpPost("reset")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (request.NewPassword.Length < 6)
            {
                throw new BusinessException("パスワードは6文字以上で入力してください。");
            }

            var resetToken = await _context.PasswordResetTokens
                .Include(t => t.User)
                .Where(t => t.UserId == request.UserId && !t.IsUsed)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync();

            if (resetToken == null)
            {
                throw new BusinessException("有効な認証コードがありません。「認証コードを送信」からやり直してください。");
            }

            if (resetToken.ExpiresAt < DateTime.Now)
            {
                throw new BusinessException("認証コードの有効期限が切れています。もう一度「認証コードを送信」からやり直してください。");
            }

            if (resetToken.Token != request.Code)
            {
                resetToken.FailedCount++;
                var invalidated = resetToken.FailedCount >= MaxFailedAttempts;
                if (invalidated)
                {
                    resetToken.IsUsed = true;
                }
                await _context.SaveChangesAsync();

                throw new BusinessException(invalidated
                    ? "認証コードの誤入力が続いたため、コードを無効にしました。もう一度「認証コードを送信」からやり直してください。"
                    : "認証コードが正しくありません。");
            }

            if (resetToken.User == null)
            {
                throw new BusinessException("ユーザーが見つかりません。");
            }

            // パスワードを更新
            resetToken.User.Password = PasswordHashService.HashPassword(request.NewPassword);
            resetToken.IsUsed = true;

            await _context.SaveChangesAsync();

            _logger.LogInformation("パスワードがリセットされました: {Email}", resetToken.User.Email);

            return Ok(new { message = "パスワードが正常にリセットされました。" });
        }

        private static string MaskEmail(string email)
        {
            var at = email.IndexOf('@');
            if (at <= 1)
            {
                return at < 0 ? "***" : "***" + email[at..];
            }
            return email[0] + "***" + email[at..];
        }
    }

    public class PasswordResetRequest
    {
        public int UserId { get; set; }
    }

    public class ResetPasswordRequest
    {
        public int UserId { get; set; }
        public string Code { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }
}
