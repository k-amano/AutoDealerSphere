using AutoDealerSphere.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoDealerSphere.Server.Services
{
    public class PartService : IPartService
    {
        private readonly IDbContextFactory<SQLDBContext> _contextFactory;

        public PartService(IDbContextFactory<SQLDBContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<IEnumerable<Part>> GetAllPartsAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Parts
                .Where(p => p.IsActive)
                .OrderBy(p => p.PartName)
                .ToListAsync();
        }

        public async Task<Part?> GetPartByIdAsync(int id)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Parts.FindAsync(id);
        }

        public async Task<Part> CreatePartAsync(Part part)
        {
            using var context = _contextFactory.CreateDbContext();
            part.CreatedAt = DateTime.Now;
            part.UpdatedAt = DateTime.Now;
            context.Parts.Add(part);
            await context.SaveChangesAsync();
            return part;
        }

        public async Task<bool> UpdatePartAsync(Part part)
        {
            using var context = _contextFactory.CreateDbContext();
            var existingPart = await context.Parts.FindAsync(part.Id);
            if (existingPart == null)
            {
                return false;
            }

            part.UpdatedAt = DateTime.Now;
            context.Entry(existingPart).CurrentValues.SetValues(part);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeletePartAsync(int id)
        {
            using var context = _contextFactory.CreateDbContext();
            var part = await context.Parts.FindAsync(id);
            if (part == null)
            {
                return false;
            }

            // 論理削除
            part.IsActive = false;
            part.UpdatedAt = DateTime.Now;
            await context.SaveChangesAsync();
            return true;
        }
    }
}