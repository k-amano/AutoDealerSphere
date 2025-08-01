using Microsoft.AspNetCore.Mvc;
using AutoDealerSphere.Server.Services;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;

namespace AutoDealerSphere.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleImportController : ControllerBase
    {
        private readonly IVehicleImportService _importService;
        private readonly ILogger<VehicleImportController> _logger;

        public VehicleImportController(IVehicleImportService importService, ILogger<VehicleImportController> logger)
        {
            _importService = importService;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadAndImport(IFormFile file, [FromForm] bool replaceExisting = false)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "ファイルが選択されていません。" });
            }

            if (!file.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) && 
                !file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "CSVまたはTXTファイルを選択してください。" });
            }

            try
            {
                // 一時ファイルに保存
                var tempPath = Path.GetTempFileName();
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // インポート実行
                var result = await _importService.ImportFromCsvAsync(tempPath, replaceExisting);

                // 一時ファイルを削除
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }

                return Ok(new
                {
                    success = true,
                    clientsImported = result.clientsImported,
                    vehiclesImported = result.vehiclesImported,
                    errors = result.errors,
                    message = $"インポートが完了しました。顧客: {result.clientsImported}件、車両: {result.vehiclesImported}件"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイルインポート中にエラーが発生しました");
                return StatusCode(500, new { error = "インポート処理中にエラーが発生しました。", details = ex.Message });
            }
        }

    }
}