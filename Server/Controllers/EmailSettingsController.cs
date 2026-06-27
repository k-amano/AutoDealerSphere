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
        public async Task<ActionResult<EmailSettingsResponse>> GetEmailSettings()
        {
            var settings = await _emailSettingsService.GetSettingsAsync();
            if (settings == null)
            {
                return Ok(new EmailSettingsResponse());
            }
            var plainPassword = "";
            if (!string.IsNullOrEmpty(settings.EncryptedPassword))
            {
                plainPassword = await _emailSettingsService.DecryptPasswordAsync(settings.EncryptedPassword);
            }
            settings.EncryptedPassword = "";
            return Ok(new EmailSettingsResponse { Settings = settings, PlainPassword = plainPassword });
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
        public async Task<ActionResult<TestConnectionResult>> TestConnection([FromBody] EmailSettingsRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, message) = await _emailSettingsService.TestConnectionAsync(
                request.Settings,
                request.PlainPassword);

            return Ok(new TestConnectionResult { Success = success, Message = message });
        }

        [HttpPost("send-test-email")]
        public async Task<ActionResult<bool>> SendTestEmail([FromBody] string toEmail)
        {
            var result = await _emailService.SendTestEmailAsync(toEmail);
            return Ok(result);
        }
    }

    public class EmailSettingsRequest
    {
        public EmailSettings Settings { get; set; } = new EmailSettings();
        public string PlainPassword { get; set; } = "";
    }

    public class TestConnectionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }
}
