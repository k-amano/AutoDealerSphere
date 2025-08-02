using AutoDealerSphere.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoDealerSphere.Server.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IDbContextFactory<SQLDBContext> _contextFactory;

        public InvoiceService(IDbContextFactory<SQLDBContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<IEnumerable<Invoice>> GetAllInvoicesAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Invoices
                .Include(i => i.Client)
                .Include(i => i.Vehicle)
                .Include(i => i.InvoiceDetails)
                .OrderBy(i => i.Id)
                .ToListAsync();
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(int id)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Invoices
                .Include(i => i.Client)
                .Include(i => i.Vehicle)
                    .ThenInclude(v => v.VehicleCategory)
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(d => d.Part)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Invoice> CreateInvoiceAsync(Invoice invoice)
        {
            using var context = _contextFactory.CreateDbContext();
            invoice.InvoiceNumber = await GenerateInvoiceNumberAsync();
            invoice.CreatedAt = DateTime.Now;
            invoice.UpdatedAt = DateTime.Now;
            
            CalculateInvoiceTotals(invoice);
            
            context.Invoices.Add(invoice);
            await context.SaveChangesAsync();
            return invoice;
        }

        public async Task<bool> UpdateInvoiceAsync(Invoice invoice)
        {
            using var context = _contextFactory.CreateDbContext();
            var existingInvoice = await context.Invoices
                .Include(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(i => i.Id == invoice.Id);
                
            if (existingInvoice == null)
            {
                return false;
            }

            invoice.UpdatedAt = DateTime.Now;
            CalculateInvoiceTotals(invoice);

            // 既存の明細を削除
            context.InvoiceDetails.RemoveRange(existingInvoice.InvoiceDetails);

            // 新しい明細を追加
            foreach (var detail in invoice.InvoiceDetails)
            {
                detail.InvoiceId = invoice.Id;
                context.InvoiceDetails.Add(detail);
            }

            context.Entry(existingInvoice).CurrentValues.SetValues(invoice);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteInvoiceAsync(int id)
        {
            using var context = _contextFactory.CreateDbContext();
            var invoice = await context.Invoices.FindAsync(id);
            if (invoice == null)
            {
                return false;
            }

            context.Invoices.Remove(invoice);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<string> GenerateInvoiceNumberAsync()
        {
            var year = DateTime.Now.Year;
            var month = DateTime.Now.Month;
            var yearMonth = $"{year}{month:D2}";

            using var context = _contextFactory.CreateDbContext();
            var lastInvoice = await context.Invoices
                .Where(i => i.InvoiceNumber.StartsWith(yearMonth))
                .OrderByDescending(i => i.InvoiceNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastInvoice != null)
            {
                var lastNumber = lastInvoice.InvoiceNumber.Substring(6);
                if (int.TryParse(lastNumber, out int number))
                {
                    nextNumber = number + 1;
                }
            }

            return $"{yearMonth}{nextNumber:D4}";
        }

        public async Task<byte[]> ExportToExcelAsync(int invoiceId)
        {
            // ExcelExportServiceに委譲
            var excelExportService = new ExcelExportService(this);
            return await excelExportService.ExportInvoiceToExcelAsync(invoiceId);
        }

        private void CalculateInvoiceTotals(Invoice invoice)
        {
            decimal taxableSubTotal = 0;
            decimal nonTaxableSubTotal = 0;

            foreach (var detail in invoice.InvoiceDetails)
            {
                var subTotal = detail.SubTotal;
                if (detail.IsTaxable)
                {
                    taxableSubTotal += subTotal;
                }
                else
                {
                    nonTaxableSubTotal += subTotal;
                }
            }

            invoice.TaxableSubTotal = taxableSubTotal;
            invoice.NonTaxableSubTotal = nonTaxableSubTotal;
            invoice.Tax = Math.Floor(taxableSubTotal * invoice.TaxRate / 100);
            invoice.Total = taxableSubTotal + nonTaxableSubTotal + invoice.Tax;
        }
    }
}