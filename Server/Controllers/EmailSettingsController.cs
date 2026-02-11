using AutoDealerSphere.Server.Services;
using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace AutoDealerSphere.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailSettingsController : ControllerBase
    {
        private readonly IEmailSettingsService _emailSettingsService;
        private readonly IEmailService _emailService;

        public EmailSettingsController(
            IEmailSettingsService emailSettingsService,
            IEmailService emailService)
        {
            _emailSettingsService = emailSettingsService;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<ActionResult<EmailSettings>> GetEmailSettings()
        {
            var settings = await _emailSettingsService.GetSettingsAsync();
            if (settings == null)
            {
                return Ok(new EmailSettings());
            }
            // パスワードは返さない（セキュリティのため）
            settings.EncryptedPassword = "";
            return Ok(settings);
        }

        [HttpPost]
        public async Task<ActionResult<EmailSettings>> CreateOrUpdateEmailSettings([FromBody] EmailSettingsRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _emailSettingsService.CreateOrUpdateSettingsAsync(
                request.Settings,
                request.PlainPassword);

            // パスワードは返さない
            result.EncryptedPassword = "";
            return Ok(result);
        }

        [HttpPost("test-connection")]
        public async Task<ActionResult<bool>> TestConnection([FromBody] EmailSettingsRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _emailSettingsService.TestConnectionAsync(
                request.Settings,
                request.PlainPassword);

            return Ok(result);
        }

        [HttpPost("send-test-email")]
        public async Task<ActionResult<bool>> SendTestEmail([FromBody] string toEmail)
        {
            try
            {
                var result = await _emailService.SendTestEmailAsync(toEmail);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class EmailSettingsRequest
    {
        public EmailSettings Settings { get; set; } = new EmailSettings();
        public string PlainPassword { get; set; } = "";
    }
}
