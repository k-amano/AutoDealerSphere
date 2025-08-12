using AutoDealerSphere.Shared.Models;
using Syncfusion.XlsIO;
using System.IO;
using Syncfusion.Drawing;

namespace AutoDealerSphere.Server.Services
{
    public class ExcelExportService : IExcelExportService
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IIssuerInfoService _issuerInfoService;

        public ExcelExportService(IInvoiceService invoiceService, IIssuerInfoService issuerInfoService)
        {
            _invoiceService = invoiceService;
            _issuerInfoService = issuerInfoService;
        }

        public async Task<byte[]> ExportInvoiceToExcelAsync(int invoiceId)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(invoiceId);
            if (invoice == null)
            {
                throw new InvalidOperationException($"Invoice with ID {invoiceId} not found.");
            }

            // 発行者情報を取得
            var issuerInfo = await _issuerInfoService.GetIssuerInfoAsync();

            using (ExcelEngine excelEngine = new ExcelEngine())
            {
                IApplication application = excelEngine.Excel;
                application.DefaultVersion = ExcelVersion.Excel2016;

                // 新しいワークブックを作成
                IWorkbook workbook = application.Workbooks.Create(1);
                IWorksheet worksheet = workbook.Worksheets[0];
                worksheet.Name = "請求書";

                // 用紙サイズをA4に設定
                worksheet.PageSetup.PaperSize = ExcelPaperSize.PaperA4;
                worksheet.PageSetup.Orientation = ExcelPageOrientation.Portrait;
                worksheet.PageSetup.TopMargin = 0.75;
                worksheet.PageSetup.BottomMargin = 0.75;
                worksheet.PageSetup.LeftMargin = 0.7;
                worksheet.PageSetup.RightMargin = 0.7;

                // 列幅設定
                worksheet.SetColumnWidth(1, 3);   // A列
                worksheet.SetColumnWidth(2, 25);  // B列（部品名称／項目）
                worksheet.SetColumnWidth(3, 15);  // C列（修理方法）
                worksheet.SetColumnWidth(4, 10);  // D列（部品単価）
                worksheet.SetColumnWidth(5, 8);   // E列（個数）
                worksheet.SetColumnWidth(6, 12);  // F列（部品価格）
                worksheet.SetColumnWidth(7, 12);  // G列（工賃）
                worksheet.SetColumnWidth(8, 5);   // H列
                worksheet.SetColumnWidth(9, 3);   // I列

                int row = 1;

                // タイトル
                worksheet.Range[$"B{row}:G{row}"].Merge();
                worksheet.Range[$"B{row}"].Text = "御　請　求　書";
                worksheet.Range[$"B{row}"].CellStyle.Font.Size = 20;
                worksheet.Range[$"B{row}"].CellStyle.Font.Bold = true;
                worksheet.Range[$"B{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range[$"B{row}"].CellStyle.Color = Color.FromArgb(146, 208, 80);
                worksheet.Range[$"B{row}"].RowHeight = 35;

                row += 2;

                // 発行者情報（右側）
                int issuerRow = row;
                if (issuerInfo != null)
                {
                    worksheet.Range[$"F{issuerRow}"].Text = "作成日";
                    worksheet.Range[$"G{issuerRow}"].Text = $"令和{DateTime.Now.Year - 2018}年{DateTime.Now.Month}月{DateTime.Now.Day}日";
                    worksheet.Range[$"G{issuerRow}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                    
                    issuerRow++;
                    worksheet.Range[$"F{issuerRow}:G{issuerRow}"].Merge();
                    worksheet.Range[$"F{issuerRow}"].Text = issuerInfo.PostalCode;
                    worksheet.Range[$"F{issuerRow}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                    
                    issuerRow++;
                    worksheet.Range[$"F{issuerRow}:G{issuerRow}"].Merge();
                    worksheet.Range[$"F{issuerRow}"].Text = issuerInfo.Address;
                    worksheet.Range[$"F{issuerRow}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                    
                    issuerRow++;
                    worksheet.Range[$"F{issuerRow}:G{issuerRow}"].Merge();
                    worksheet.Range[$"F{issuerRow}"].Text = issuerInfo.CompanyName;
                    worksheet.Range[$"F{issuerRow}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                    
                    issuerRow++;
                    worksheet.Range[$"F{issuerRow}:G{issuerRow}"].Merge();
                    worksheet.Range[$"F{issuerRow}"].Text = $"代表　{issuerInfo.Position}　{issuerInfo.Name}";
                    worksheet.Range[$"F{issuerRow}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                    
                    issuerRow++;
                    worksheet.Range[$"F{issuerRow}"].Text = $"TEL {issuerInfo.PhoneNumber}";
                    worksheet.Range[$"G{issuerRow}"].Text = $"FAX {issuerInfo.FaxNumber}";
                    worksheet.Range[$"F{issuerRow}:G{issuerRow}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                }

                // 請求先情報（左側）
                worksheet.Range[$"B{row}"].Text = "郵便番号";
                row++;
                worksheet.Range[$"B{row}"].Text = "住所";
                row++;
                worksheet.Range[$"B{row}"].Text = "氏名";
                worksheet.Range[$"C{row}:D{row}"].Merge();
                worksheet.Range[$"C{row}"].Text = $"{invoice.Client?.Name ?? ""} 様";
                worksheet.Range[$"C{row}"].CellStyle.Font.Size = 14;
                worksheet.Range[$"C{row}"].CellStyle.Font.Bold = true;

                row = Math.Max(row, issuerRow) + 2;

                // 合計金額
                worksheet.Range[$"B{row}"].Text = "合計金額";
                worksheet.Range[$"B{row}"].CellStyle.Font.Bold = true;
                worksheet.Range[$"C{row}:D{row}"].Merge();
                worksheet.Range[$"C{row}"].Text = $"¥{invoice.Total:N0}";
                worksheet.Range[$"C{row}"].CellStyle.Font.Size = 16;
                worksheet.Range[$"C{row}"].CellStyle.Font.Bold = true;
                worksheet.Range[$"C{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range[$"B{row}:D{row}"].BorderAround(ExcelLineStyle.Medium);

                row += 2;

                // 振込案内
                worksheet.Range[$"E{row}:G{row}"].Merge();
                worksheet.Range[$"E{row}"].Text = "銀行振込の場合は、下記口座までお振込みください。";
                worksheet.Range[$"E{row}"].CellStyle.Color = Color.FromArgb(146, 208, 80);
                worksheet.Range[$"E{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

                row++;
                
                // 口座情報
                if (issuerInfo != null && !string.IsNullOrEmpty(issuerInfo.Bank1Name))
                {
                    worksheet.Range[$"E{row}:G{row}"].Merge();
                    worksheet.Range[$"E{row}"].Text = $"{issuerInfo.Bank1Name} {issuerInfo.Bank1BranchName} 普通 {issuerInfo.Bank1AccountNumber} {issuerInfo.CompanyName}";
                    worksheet.Range[$"E{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                    row++;
                }
                
                if (issuerInfo != null && !string.IsNullOrEmpty(issuerInfo.Bank2Name))
                {
                    worksheet.Range[$"E{row}:G{row}"].Merge();
                    worksheet.Range[$"E{row}"].Text = $"{issuerInfo.Bank2Name} {issuerInfo.Bank2BranchName} 普通 {issuerInfo.Bank2AccountNumber} {issuerInfo.CompanyName} 代表者 {issuerInfo.Name}";
                    worksheet.Range[$"E{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                }

                row += 2;

                // 車両情報セクション
                worksheet.Range[$"B{row}"].Text = "車両番号";
                worksheet.Range[$"B{row}"].CellStyle.Color = Color.FromArgb(217, 217, 217);
                worksheet.Range[$"B{row}"].CellStyle.Font.Bold = true;
                worksheet.Range[$"C{row}"].Text = "車名";
                worksheet.Range[$"C{row}"].CellStyle.Color = Color.FromArgb(217, 217, 217);
                worksheet.Range[$"C{row}"].CellStyle.Font.Bold = true;
                worksheet.Range[$"D{row}"].Text = "車体番号";
                worksheet.Range[$"D{row}"].CellStyle.Color = Color.FromArgb(217, 217, 217);
                worksheet.Range[$"D{row}"].CellStyle.Font.Bold = true;
                worksheet.Range[$"E{row}"].Text = "初年度登録";
                worksheet.Range[$"E{row}"].CellStyle.Color = Color.FromArgb(217, 217, 217);
                worksheet.Range[$"E{row}"].CellStyle.Font.Bold = true;
                worksheet.Range[$"F{row}:G{row}"].Merge();
                worksheet.Range[$"F{row}"].Text = "走行距離";
                worksheet.Range[$"F{row}"].CellStyle.Color = Color.FromArgb(217, 217, 217);
                worksheet.Range[$"F{row}"].CellStyle.Font.Bold = true;
                worksheet.Range[$"F{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

                row++;

                // 車両情報データ
                string licensePlate = "";
                if (invoice.Vehicle != null)
                {
                    licensePlate = $"{invoice.Vehicle.LicensePlateLocation ?? ""} {invoice.Vehicle.LicensePlateClassification ?? ""} {invoice.Vehicle.LicensePlateHiragana ?? ""} {invoice.Vehicle.LicensePlateNumber ?? ""}".Trim();
                }
                worksheet.Range[$"B{row}"].Text = licensePlate;
                worksheet.Range[$"C{row}"].Text = invoice.Vehicle?.VehicleName ?? "";
                worksheet.Range[$"D{row}"].Text = invoice.Vehicle?.ChassisNumber ?? "";
                worksheet.Range[$"E{row}"].Text = invoice.Vehicle?.FirstRegistrationDate?.ToString("yyyy/MM/dd") ?? "";
                worksheet.Range[$"F{row}:G{row}"].Merge();
                worksheet.Range[$"F{row}"].Text = invoice.Mileage.HasValue ? $"{invoice.Mileage:N0} km" : "";
                worksheet.Range[$"F{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

                row += 2;

                // 明細ヘッダー（画像に合わせた項目名）
                worksheet.Range[$"B{row}"].Text = "部品名称／項目";
                worksheet.Range[$"C{row}"].Text = "修理方法";
                worksheet.Range[$"D{row}"].Text = "部品単価";
                worksheet.Range[$"E{row}"].Text = "個数";
                worksheet.Range[$"F{row}"].Text = "部品価格";
                worksheet.Range[$"G{row}"].Text = "工賃";

                // ヘッダーのスタイル設定
                var headerRange = worksheet.Range[$"B{row}:G{row}"];
                headerRange.CellStyle.Font.Bold = true;
                headerRange.CellStyle.Color = Color.FromArgb(217, 217, 217);
                headerRange.CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                headerRange.CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                headerRange.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

                row++;

                // 明細行（法定費用以外）
                int detailStartRow = row;
                var taxableDetails = invoice.InvoiceDetails
                    .Where(d => d.Type != "法定費用")
                    .OrderBy(d => d.DisplayOrder);
                    
                foreach (var detail in taxableDetails)
                {
                    worksheet.Range[$"B{row}"].Text = detail.ItemName;
                    worksheet.Range[$"C{row}"].Text = detail.RepairMethod ?? "";
                    worksheet.Range[$"D{row}"].Number = (double)detail.UnitPrice;
                    worksheet.Range[$"D{row}"].NumberFormat = "#,##0";
                    worksheet.Range[$"E{row}"].Number = (double)detail.Quantity;
                    worksheet.Range[$"E{row}"].NumberFormat = "#,##0.0";
                    worksheet.Range[$"F{row}"].Number = (double)(detail.Quantity * detail.UnitPrice);
                    worksheet.Range[$"F{row}"].NumberFormat = "#,##0";
                    worksheet.Range[$"G{row}"].Number = (double)detail.LaborCost;
                    worksheet.Range[$"G{row}"].NumberFormat = "#,##0";

                    // 数値列の右寄せ
                    worksheet.Range[$"D{row}:G{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;

                    row++;
                }

                // 明細の罫線
                if (row > detailStartRow)
                {
                    var detailRange = worksheet.Range[$"B{detailStartRow}:G{row - 1}"];
                    detailRange.BorderAround(ExcelLineStyle.Thin);
                    detailRange.BorderInside(ExcelLineStyle.Thin, ExcelKnownColors.Black);
                }

                // ページ小計
                row++;
                worksheet.Range[$"E{row}"].Text = "ページ小計";
                worksheet.Range[$"E{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range[$"F{row}"].Number = (double)invoice.TaxableSubTotal;
                worksheet.Range[$"F{row}"].NumberFormat = "#,##0";
                worksheet.Range[$"F{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range[$"F{row}"].BorderAround(ExcelLineStyle.Thin);

                row += 2;

                // 非課税項目（法定費用）
                worksheet.Range[$"B{row}"].Text = "非課税項目";
                worksheet.Range[$"B{row}"].CellStyle.Font.Bold = true;
                worksheet.Range[$"B{row}"].CellStyle.Color = Color.FromArgb(146, 208, 80);
                worksheet.Range[$"B{row}:C{row}"].Merge();

                row++;
                
                var nonTaxableItems = invoice.InvoiceDetails
                    .Where(d => d.Type == "法定費用")
                    .OrderBy(d => d.DisplayOrder);

                foreach (var item in nonTaxableItems)
                {
                    worksheet.Range[$"B{row}"].Text = item.ItemName;
                    worksheet.Range[$"C{row}"].Number = (double)item.UnitPrice;
                    worksheet.Range[$"C{row}"].NumberFormat = "#,##0";
                    worksheet.Range[$"C{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                    row++;
                }

                // 非課税項目計
                worksheet.Range[$"B{row}"].Text = "非課税項目計";
                worksheet.Range[$"B{row}"].CellStyle.Font.Bold = true;
                worksheet.Range[$"C{row}"].Number = (double)invoice.NonTaxableSubTotal;
                worksheet.Range[$"C{row}"].NumberFormat = "#,##0";
                worksheet.Range[$"C{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range[$"C{row}"].CellStyle.Font.Bold = true;

                // 小計・消費税・合計（右側）
                int summaryRow = row - 4;
                
                worksheet.Range[$"E{summaryRow}"].Text = "小計";
                worksheet.Range[$"F{summaryRow}"].Number = (double)invoice.TaxableSubTotal;
                worksheet.Range[$"F{summaryRow}"].NumberFormat = "#,##0";
                worksheet.Range[$"F{summaryRow}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range[$"G{summaryRow}"].Number = (double)invoice.TaxableSubTotal;
                worksheet.Range[$"G{summaryRow}"].NumberFormat = "#,##0";
                worksheet.Range[$"G{summaryRow}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;

                summaryRow++;
                worksheet.Range[$"E{summaryRow}"].Text = "課税額計";
                worksheet.Range[$"F{summaryRow}"].Number = (double)invoice.TaxableSubTotal;
                worksheet.Range[$"F{summaryRow}"].NumberFormat = "#,##0";
                worksheet.Range[$"F{summaryRow}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range[$"G{summaryRow}"].Number = (double)invoice.TaxableSubTotal;
                worksheet.Range[$"G{summaryRow}"].NumberFormat = "#,##0";
                worksheet.Range[$"G{summaryRow}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;

                summaryRow++;
                worksheet.Range[$"E{summaryRow}"].Text = $"消費税 {invoice.TaxRate}%";
                worksheet.Range[$"F{summaryRow}"].Number = (double)invoice.Tax;
                worksheet.Range[$"F{summaryRow}"].NumberFormat = "#,##0";
                worksheet.Range[$"F{summaryRow}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range[$"G{summaryRow}"].Number = (double)invoice.Tax;
                worksheet.Range[$"G{summaryRow}"].NumberFormat = "#,##0";
                worksheet.Range[$"G{summaryRow}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;

                summaryRow++;
                worksheet.Range[$"E{summaryRow}"].Text = "非課税額計";
                worksheet.Range[$"E{summaryRow}"].CellStyle.Color = Color.FromArgb(146, 208, 80);
                worksheet.Range[$"F{summaryRow}"].Number = (double)invoice.NonTaxableSubTotal;
                worksheet.Range[$"F{summaryRow}"].NumberFormat = "#,##0";
                worksheet.Range[$"F{summaryRow}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range[$"G{summaryRow}"].Number = (double)invoice.NonTaxableSubTotal;
                worksheet.Range[$"G{summaryRow}"].NumberFormat = "#,##0";
                worksheet.Range[$"G{summaryRow}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;

                summaryRow++;
                worksheet.Range[$"E{summaryRow}"].Text = "合計";
                worksheet.Range[$"E{summaryRow}"].CellStyle.Font.Bold = true;
                worksheet.Range[$"F{summaryRow}"].CellStyle.Font.Bold = true;
                worksheet.Range[$"G{summaryRow}"].Number = (double)invoice.Total;
                worksheet.Range[$"G{summaryRow}"].NumberFormat = "#,##0";
                worksheet.Range[$"G{summaryRow}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range[$"G{summaryRow}"].CellStyle.Font.Bold = true;

                // MemoryStreamに保存
                using (MemoryStream stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }
    }
}