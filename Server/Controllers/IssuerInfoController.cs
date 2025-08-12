using AutoDealerSphere.Server.Services;
using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace AutoDealerSphere.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IssuerInfoController : ControllerBase
    {
        private readonly IIssuerInfoService _issuerInfoService;

        public IssuerInfoController(IIssuerInfoService issuerInfoService)
        {
            _issuerInfoService = issuerInfoService;
        }

        [HttpGet]
        public async Task<ActionResult<IssuerInfo>> GetIssuerInfo()
        {
            var issuerInfo = await _issuerInfoService.GetIssuerInfoAsync();
            if (issuerInfo == null)
            {
                return Ok(new IssuerInfo());
            }
            return Ok(issuerInfo);
        }

        [HttpPost]
        public async Task<ActionResult<IssuerInfo>> CreateOrUpdateIssuerInfo([FromBody] IssuerInfo issuerInfo)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _issuerInfoService.CreateOrUpdateIssuerInfoAsync(issuerInfo);
            return Ok(result);
        }
    }
}