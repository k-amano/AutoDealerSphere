using Microsoft.AspNetCore.Mvc;
using AutoDealerSphere.Server.Services;

namespace AutoDealerSphere.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleImportController : ControllerBase
    {
        private readonly IVehicleImportService _importService;

        public VehicleImportController(IVehicleImportService importService)
        {
            _importService = importService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadAndImport(IFormFile file, [FromForm] bool replaceExisting = false)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "ファイルが選択されていません。" });

            if (!file.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) &&
                !file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "CSVまたはTXTファイルを選択してください。" });

            var tempPath = Path.GetTempFileName();
            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var result = await _importService.ImportFromCsvAsync(tempPath, replaceExisting);

            if (System.IO.File.Exists(tempPath))
                System.IO.File.Delete(tempPath);

            return Ok(new
            {
                success = true,
                clientsImported = result.clientsImported,
                vehiclesImported = result.vehiclesImported,
                errors = result.errors,
                message = $"インポートが完了しました。顧客: {result.clientsImported}件、車両: {result.vehiclesImported}件"
            });
        }
    }
}