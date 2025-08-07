using AutoDealerSphere.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoDealerSphere.Server.Services
{
    public class StatutoryFeeService : IStatutoryFeeService
    {
        private readonly IDbContextFactory<SQLDBContext> _dbContextFactory;

        public StatutoryFeeService(IDbContextFactory<SQLDBContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<List<StatutoryFee>> GetAllAsync()
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            return await context.StatutoryFees
                .Include(sf => sf.VehicleCategory)
                .OrderBy(sf => sf.VehicleCategoryId)
                .ThenBy(sf => sf.FeeType)
                .ToListAsync();
        }

        public async Task<StatutoryFee?> GetByIdAsync(int id)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            return await context.StatutoryFees
                .Include(sf => sf.VehicleCategory)
                .FirstOrDefaultAsync(sf => sf.Id == id);
        }

        public async Task<List<StatutoryFee>> GetByCategoryIdAsync(int categoryId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var currentDate = DateTime.Today;
            
            return await context.StatutoryFees
                .Include(sf => sf.VehicleCategory)
                .Where(sf => sf.VehicleCategoryId == categoryId)
                .Where(sf => sf.EffectiveFrom <= currentDate)
                .Where(sf => sf.EffectiveTo == null || sf.EffectiveTo >= currentDate)
                .OrderBy(sf => sf.FeeType)
                .ToListAsync();
        }

        public async Task<StatutoryFee> CreateAsync(StatutoryFee statutoryFee)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            context.StatutoryFees.Add(statutoryFee);
            await context.SaveChangesAsync();
            return statutoryFee;
        }

        public async Task<StatutoryFee?> UpdateAsync(int id, StatutoryFee statutoryFee)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var existing = await context.StatutoryFees.FindAsync(id);
            
            if (existing == null)
                return null;

            existing.VehicleCategoryId = statutoryFee.VehicleCategoryId;
            existing.FeeType = statutoryFee.FeeType;
            existing.Amount = statutoryFee.Amount;
            existing.IsTaxable = statutoryFee.IsTaxable;
            existing.EffectiveFrom = statutoryFee.EffectiveFrom;
            existing.EffectiveTo = statutoryFee.EffectiveTo;
            existing.UpdatedAt = DateTime.Now;

            await context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var statutoryFee = await context.StatutoryFees.FindAsync(id);
            
            if (statutoryFee == null)
                return false;

            context.StatutoryFees.Remove(statutoryFee);
            await context.SaveChangesAsync();
            return true;
        }
    }
}