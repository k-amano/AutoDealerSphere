using AutoDealerSphere.Shared.Models;
using AutoDealerSphere.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoDealerSphere.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatutoryFeesController : ControllerBase
    {
        private readonly IStatutoryFeeService _statutoryFeeService;

        public StatutoryFeesController(IStatutoryFeeService statutoryFeeService)
        {
            _statutoryFeeService = statutoryFeeService;
        }

        [HttpGet]
        public async Task<ActionResult<List<StatutoryFee>>> GetAll()
        {
            var fees = await _statutoryFeeService.GetAllAsync();
            return Ok(fees);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StatutoryFee>> GetById(int id)
        {
            var fee = await _statutoryFeeService.GetByIdAsync(id);
            if (fee == null)
                return NotFound();
            
            return Ok(fee);
        }

        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<List<StatutoryFee>>> GetByCategoryId(int categoryId)
        {
            var fees = await _statutoryFeeService.GetByCategoryIdAsync(categoryId);
            return Ok(fees);
        }

        [HttpPost]
        public async Task<ActionResult<StatutoryFee>> Create(StatutoryFee statutoryFee)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _statutoryFeeService.CreateAsync(statutoryFee);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<StatutoryFee>> Update(int id, StatutoryFee statutoryFee)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _statutoryFeeService.UpdateAsync(id, statutoryFee);
            if (updated == null)
                return NotFound();

            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var deleted = await _statutoryFeeService.DeleteAsync(id);
            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}