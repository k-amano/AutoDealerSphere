namespace AutoDealerSphere.Server.Services
{
    public interface IExcelExportService
    {
        Task<byte[]> ExportInvoiceToExcelAsync(int invoiceId);
    }
}