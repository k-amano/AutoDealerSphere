using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoDealerSphere.Server.Services;
using AutoDealerSphere.Shared.Models;

namespace AutoDealerSphere.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleCategoriesController : ControllerBase
    {
        private readonly IDbContextFactory<SQLDBContext> _contextFactory;

        public VehicleCategoriesController(IDbContextFactory<SQLDBContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehicleCategory>>> GetVehicleCategories()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.VehicleCategories
                .OrderBy(vc => vc.DisplayOrder)
                .ToListAsync();
        }
    }
}