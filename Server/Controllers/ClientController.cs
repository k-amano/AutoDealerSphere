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

            // 名前またはカナの部分一致
            if (!string.IsNullOrEmpty(search.NameOrKana))
            {
                filteredClients = filteredClients.Where(c => 
                    c.Name.Contains(search.NameOrKana) || 
                    (c.Kana != null && c.Kana.Contains(search.NameOrKana))
                );
            }
            
            // メールアドレスの部分一致
            if (!string.IsNullOrEmpty(search.Email))
            {
                filteredClients = filteredClients.Where(c => c.Email.Contains(search.Email));
            }
            
            // 電話番号の部分一致
            if (!string.IsNullOrEmpty(search.Phone))
            {
                filteredClients = filteredClients.Where(c => c.Phone != null && c.Phone.Contains(search.Phone));
            }
            
            // 住所の部分一致（郵便番号、都道府県、住所）
            if (!string.IsNullOrEmpty(search.Address))
            {
                filteredClients = filteredClients.Where(c => 
                    c.Zip.Contains(search.Address) ||
                    Prefecture.GetName(c.Prefecture).Contains(search.Address) ||
                    c.Address.Contains(search.Address)
                );
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
