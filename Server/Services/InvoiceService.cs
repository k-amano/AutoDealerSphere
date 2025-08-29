using AutoDealerSphere.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoDealerSphere.Server.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IDbContextFactory<SQLDBContext> _contextFactory;
        private readonly IIssuerInfoService _issuerInfoService;

        public InvoiceService(IDbContextFactory<SQLDBContext> contextFactory, IIssuerInfoService issuerInfoService)
        {
            _contextFactory = contextFactory;
            _issuerInfoService = issuerInfoService;
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
            invoice.InvoiceNumber = await GenerateInvoiceNumberAsync(invoice.ClientId);
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

        public async Task<string> GenerateInvoiceNumberAsync(int clientId)
        {
            var year = DateTime.Now.Year % 100;  // 2桁の年 (yy)
            var month = DateTime.Now.Month;      // 月 (mm)
            var yearMonth = $"{year:D2}{month:D2}";      // yymm形式
            var clientIdPart = $"{clientId:D4}";         // ClientId 4桁0埋め
            
            var prefix = $"{yearMonth}{clientIdPart}";   // {yymm}{ClientId}

            using var context = _contextFactory.CreateDbContext();
            var lastInvoice = await context.Invoices
                .Where(i => i.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(i => i.InvoiceNumber)
                .FirstOrDefaultAsync();

            int nextSequence = 1;
            if (lastInvoice != null)
            {
                // 最後の2桁（連番）を取得
                var lastSequence = lastInvoice.InvoiceNumber.Substring(10);
                if (int.TryParse(lastSequence, out int sequence))
                {
                    nextSequence = sequence + 1;
                }
            }

            // {yymm}{ClientId 4桁0埋め}{連番 2桁0埋め}
            return $"{prefix}{nextSequence:D2}";
        }

        public async Task<byte[]> ExportToExcelAsync(int invoiceId)
        {
            // ExcelExportServiceに委譲
            var excelExportService = new ExcelExportService(this, _issuerInfoService);
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

        // 明細CRUD操作
        public async Task<InvoiceDetail> CreateInvoiceDetailAsync(int invoiceId, InvoiceDetail detail)
        {
            using var context = _contextFactory.CreateDbContext();
            detail.InvoiceId = invoiceId;
            detail.CreatedAt = DateTime.Now;
            
            context.InvoiceDetails.Add(detail);
            await context.SaveChangesAsync();
            
            // 請求書の合計を再計算
            await RecalculateInvoiceTotalsAsync(invoiceId);
            
            return detail;
        }

        public async Task<InvoiceDetail?> UpdateInvoiceDetailAsync(int invoiceId, int detailId, InvoiceDetail detail)
        {
            using var context = _contextFactory.CreateDbContext();
            var existingDetail = await context.InvoiceDetails
                .FirstOrDefaultAsync(d => d.Id == detailId && d.InvoiceId == invoiceId);
            
            if (existingDetail == null)
                return null;
            
            existingDetail.ItemName = detail.ItemName;
            existingDetail.Type = detail.Type;
            existingDetail.RepairMethod = detail.RepairMethod;
            existingDetail.Quantity = detail.Quantity;
            existingDetail.UnitPrice = detail.UnitPrice;
            existingDetail.LaborCost = detail.LaborCost;
            existingDetail.IsTaxable = detail.IsTaxable;
            existingDetail.PartId = detail.PartId;
            
            await context.SaveChangesAsync();
            
            // 請求書の合計を再計算
            await RecalculateInvoiceTotalsAsync(invoiceId);
            
            return existingDetail;
        }

        public async Task<bool> DeleteInvoiceDetailAsync(int invoiceId, int detailId)
        {
            using var context = _contextFactory.CreateDbContext();
            var detail = await context.InvoiceDetails
                .FirstOrDefaultAsync(d => d.Id == detailId && d.InvoiceId == invoiceId);
            
            if (detail == null)
                return false;
            
            context.InvoiceDetails.Remove(detail);
            await context.SaveChangesAsync();
            
            // 請求書の合計を再計算
            await RecalculateInvoiceTotalsAsync(invoiceId);
            
            return true;
        }

        private async Task RecalculateInvoiceTotalsAsync(int invoiceId)
        {
            using var context = _contextFactory.CreateDbContext();
            var invoice = await context.Invoices
                .Include(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);
            
            if (invoice != null)
            {
                CalculateInvoiceTotals(invoice);
                invoice.UpdatedAt = DateTime.Now;
                await context.SaveChangesAsync();
            }
        }
    }
}