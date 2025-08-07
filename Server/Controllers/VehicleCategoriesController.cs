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
<<<<<<< HEAD
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
=======
        private readonly SQLDBContext _context;

        public VehicleCategoriesController(SQLDBContext context)
        {
            _context = context;
        }

        // GET: api/VehicleCategories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehicleCategory>>> GetVehicleCategories()
        {
            return await _context.VehicleCategories
                .OrderBy(vc => vc.DisplayOrder)
                .ToListAsync();
        }

        // GET: api/VehicleCategories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<VehicleCategory>> GetVehicleCategory(int id)
        {
            var vehicleCategory = await _context.VehicleCategories.FindAsync(id);

            if (vehicleCategory == null)
            {
                return NotFound();
            }

            return vehicleCategory;
        }
>>>>>>> 3ede5e8 (feat: 車両管理編集画面に排気量と車種の入力欄を追加)
    }
}