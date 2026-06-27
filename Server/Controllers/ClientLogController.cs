using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace AutoDealerSphere.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientLogController : ControllerBase
{
    private readonly ILogger<ClientLogController> _logger;

    public ClientLogController(ILogger<ClientLogController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public IActionResult Log([FromBody] ClientLogRequest request)
    {
        _logger.LogError(
            "クライアントエラー: {Message}{NewLine}URL: {Url}{NewLine}StackTrace: {StackTrace}",
            request.Message,
            Environment.NewLine,
            request.Url ?? "(不明)",
            Environment.NewLine,
            request.StackTrace ?? "(なし)");

        return Ok();
    }
}
