using AutoDealerSphere.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoDealerSphere.Server.Services
{
    public class IssuerInfoService : IIssuerInfoService
    {
        private readonly IDbContextFactory<SQLDBContext> _contextFactory;

        public IssuerInfoService(IDbContextFactory<SQLDBContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<IssuerInfo> GetIssuerInfoAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.IssuerInfos.FirstOrDefaultAsync();
        }

        public async Task<IssuerInfo> CreateOrUpdateIssuerInfoAsync(IssuerInfo issuerInfo)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var existingInfo = await context.IssuerInfos.FirstOrDefaultAsync();
            
            if (existingInfo == null)
            {
                context.IssuerInfos.Add(issuerInfo);
            }
            else
            {
                existingInfo.PostalCode = issuerInfo.PostalCode;
                existingInfo.Address = issuerInfo.Address;
                existingInfo.CompanyName = issuerInfo.CompanyName;
                existingInfo.Position = issuerInfo.Position;
                existingInfo.Name = issuerInfo.Name;
                existingInfo.PhoneNumber = issuerInfo.PhoneNumber;
                existingInfo.FaxNumber = issuerInfo.FaxNumber;
                existingInfo.Bank1Name = issuerInfo.Bank1Name;
                existingInfo.Bank1BranchName = issuerInfo.Bank1BranchName;
                existingInfo.Bank1AccountType = issuerInfo.Bank1AccountType;
                existingInfo.Bank1AccountNumber = issuerInfo.Bank1AccountNumber;
                existingInfo.Bank2Name = issuerInfo.Bank2Name;
                existingInfo.Bank2BranchName = issuerInfo.Bank2BranchName;
                existingInfo.Bank2AccountType = issuerInfo.Bank2AccountType;
                existingInfo.Bank2AccountNumber = issuerInfo.Bank2AccountNumber;
                
                context.IssuerInfos.Update(existingInfo);
            }
            
            await context.SaveChangesAsync();
            
            return existingInfo ?? issuerInfo;
        }
    }
}