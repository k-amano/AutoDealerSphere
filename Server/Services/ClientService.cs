using AutoDealerSphere.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoDealerSphere.Server.Services
{
    public class ClientService : IClientService
    {
        private readonly SQLDBContext _context;

        public ClientService(SQLDBContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AutoDealerSphere.Shared.Models.Client>> GetAllAsync()
        {
            return await _context.Clients.ToListAsync();
        }

        public async Task<AutoDealerSphere.Shared.Models.Client?> GetByIdAsync(int id)
        {
            return await _context.Clients.FindAsync(id);
        }

        public async Task<AutoDealerSphere.Shared.Models.Client> CreateAsync(AutoDealerSphere.Shared.Models.Client client)
        {
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();
            return client;
        }

        public async Task<AutoDealerSphere.Shared.Models.Client?> UpdateAsync(AutoDealerSphere.Shared.Models.Client client)
        {
            var existingClient = await _context.Clients.FindAsync(client.Id);
            if (existingClient == null)
            {
                return null;
            }

            _context.Entry(existingClient).CurrentValues.SetValues(client);
            await _context.SaveChangesAsync();
            return existingClient;
        }

        public async Task DeleteAsync(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client != null)
            {
                _context.Clients.Remove(client);
                await _context.SaveChangesAsync();
            }
        }
    }
}