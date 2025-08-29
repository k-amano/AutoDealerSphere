using AutoDealerSphere.Shared.Models;

namespace AutoDealerSphere.Server.Services
{
    public interface IInvoiceService
    {
        Task<IEnumerable<Invoice>> GetAllInvoicesAsync();
        Task<Invoice?> GetInvoiceByIdAsync(int id);
        Task<Invoice> CreateInvoiceAsync(Invoice invoice);
        Task<bool> UpdateInvoiceAsync(Invoice invoice);
        Task<bool> DeleteInvoiceAsync(int id);
        Task<string> GenerateInvoiceNumberAsync(int clientId);
        Task<byte[]> ExportToExcelAsync(int invoiceId);
        
        // 明細CRUD操作
        Task<InvoiceDetail> CreateInvoiceDetailAsync(int invoiceId, InvoiceDetail detail);
        Task<InvoiceDetail?> UpdateInvoiceDetailAsync(int invoiceId, int detailId, InvoiceDetail detail);
        Task<bool> DeleteInvoiceDetailAsync(int invoiceId, int detailId);
    }
}