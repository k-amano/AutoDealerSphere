using Microsoft.AspNetCore.Mvc;
using AutoDealerSphere.Server.Services;

namespace AutoDealerSphere.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataManagementController : ControllerBase
    {
        private readonly IDataManagementService _dataManagementService;

        public DataManagementController(IDataManagementService dataManagementService)
        {
            _dataManagementService = dataManagementService;
        }

        [HttpGet("backup")]
        public async Task<IActionResult> Backup()
        {
            var result = await _dataManagementService.CreateBackupAsync();
            return Ok(result);
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

            using var stream = file.OpenReadStream();
            var result = await _dataManagementService.RestoreFromBackupAsync(stream);
            return Ok(result);
        }
    }
}
