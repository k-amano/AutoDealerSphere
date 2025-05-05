using AutoDealerSphere.Server.Services;
using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace AutoDealerSphere.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientController : ControllerBase
    {
        private readonly IClientService _clientService;

        public ClientController(IClientService clientService)
        {
            _clientService = clientService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AutoDealerSphere.Shared.Models.Client>>> Get()
        {
            var clients = await _clientService.GetAllAsync();
            return Ok(clients);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AutoDealerSphere.Shared.Models.Client>> GetClient(int id)
        {
            var client = await _clientService.GetByIdAsync(id);
            if (client == null)
            {
                return NotFound();
            }
            return Ok(client);
        }

        [Route("search")]
        [HttpPost]
        public async Task<ActionResult<IEnumerable<AutoDealerSphere.Shared.Models.Client>>> SearchClients(AutoDealerSphere.Shared.Models.ClientSearch search)
        {
            var clients = await _clientService.GetAllAsync();
            var filteredClients = clients.AsQueryable();

            if (search.Id > 0)
            {
                filteredClients = filteredClients.Where(c => c.Id == search.Id);
            }
            else
            {
                if (!string.IsNullOrEmpty(search.Name))
                {
                    filteredClients = filteredClients.Where(c => c.Name.Contains(search.Name));
                }
                if (!string.IsNullOrEmpty(search.Email))
                {
                    filteredClients = filteredClients.Where(c => c.Email.Contains(search.Email));
                }
            }

            return Ok(filteredClients.ToList());
        }

        [Route("add")]
        [HttpPost]
        public async Task<IActionResult> AddClient(AutoDealerSphere.Shared.Models.Client client)
        {
            if (client == null)
            {
                return BadRequest();
            }
            
            await _clientService.CreateAsync(client);
            return Ok("Saved Successfully!!");
        }

        [Route("update")]
        [HttpPost]
        public async Task<IActionResult> UpdateClient(AutoDealerSphere.Shared.Models.Client client)
        {
            if (client == null)
            {
                return BadRequest();
            }

            var existingClient = await _clientService.GetByIdAsync(client.Id);
            if (existingClient == null)
            {
                return NotFound();
            }

            await _clientService.UpdateAsync(client);
            return Ok("Updated Successfully!!");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClient(int id)
        {
            await _clientService.DeleteAsync(id);
            return NoContent();
        }
    }
}
