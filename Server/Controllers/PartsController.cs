using Microsoft.AspNetCore.Mvc;
using AutoDealerSphere.Server.Services;
using AutoDealerSphere.Shared.Models;

namespace AutoDealerSphere.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PartsController : ControllerBase
    {
        private readonly IPartService _partService;

        public PartsController(IPartService partService)
        {
            _partService = partService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Part>>> GetParts()
        {
            var parts = await _partService.GetAllPartsAsync();
            return Ok(parts);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Part>> GetPart(int id)
        {
            var part = await _partService.GetPartByIdAsync(id);
            if (part == null)
            {
                return NotFound();
            }
            return Ok(part);
        }

        [HttpPost]
        public async Task<ActionResult<Part>> CreatePart(Part part)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdPart = await _partService.CreatePartAsync(part);
            return CreatedAtAction(nameof(GetPart), new { id = createdPart.Id }, createdPart);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePart(int id, Part part)
        {
            if (id != part.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updated = await _partService.UpdatePartAsync(part);
            if (!updated)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePart(int id)
        {
            var deleted = await _partService.DeletePartAsync(id);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}