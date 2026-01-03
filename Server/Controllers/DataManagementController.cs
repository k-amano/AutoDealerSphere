using Microsoft.AspNetCore.Mvc;
using AutoDealerSphere.Server.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AutoDealerSphere.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataManagementController : ControllerBase
    {
        private readonly IDataManagementService _dataManagementService;
        private readonly ILogger<DataManagementController> _logger;

        public DataManagementController(
            IDataManagementService dataManagementService,
            ILogger<DataManagementController> logger)
        {
            _dataManagementService = dataManagementService;
            _logger = logger;
        }

        [HttpGet("backup")]
        public async Task<IActionResult> Backup()
        {
            try
            {
                var result = await _dataManagementService.CreateBackupAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "バックアップ処理中にエラーが発生しました");
                return StatusCode(500, new { error = "バックアップ処理中にエラーが発生しました。", details = ex.Message });
            }
        }

        [HttpPost("restore")]
        public async Task<IActionResult> Restore(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "ファイルが選択されていません。" });
            }

            if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) &&
                !file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "バックアップファイル（.jsonまたは.zip）を選択してください。" });
            }

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _dataManagementService.RestoreFromBackupAsync(stream);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "レストア処理中にエラーが発生しました");
                return StatusCode(500, new { error = "レストア処理中にエラーが発生しました。", details = ex.Message });
            }
        }
    }
}
