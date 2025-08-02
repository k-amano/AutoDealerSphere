using AutoDealerSphere.Shared.Models;
using Syncfusion.XlsIO;
using System.IO;
using Syncfusion.Drawing;

namespace AutoDealerSphere.Server.Services
{
    public class ExcelExportService : IExcelExportService
    {
        private readonly IInvoiceService _invoiceService;

        public ExcelExportService(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        public async Task<byte[]> ExportInvoiceToExcelAsync(int invoiceId)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(invoiceId);
            if (invoice == null)
            {
                throw new InvalidOperationException($"Invoice with ID {invoiceId} not found.");
            }

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
                worksheet.SetColumnWidth(2, 25);  // B列（項目名）
                worksheet.SetColumnWidth(3, 15);  // C列（型式）
                worksheet.SetColumnWidth(4, 15);  // D列（修理方法）
                worksheet.SetColumnWidth(5, 8);   // E列（数量）
                worksheet.SetColumnWidth(6, 12);  // F列（単価）
                worksheet.SetColumnWidth(7, 12);  // G列（金額）
                worksheet.SetColumnWidth(8, 12);  // H列（工賃）
                worksheet.SetColumnWidth(9, 3);   // I列

                int row = 1;

                // タイトル
                worksheet.Range[$"A{row}:I{row}"].Merge();
                worksheet.Range[$"A{row}"].Text = "御　請　求　書";
                worksheet.Range[$"A{row}"].CellStyle.Font.Size = 20;
                worksheet.Range[$"A{row}"].CellStyle.Font.Bold = true;
                worksheet.Range[$"A{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range[$"A{row}"].RowHeight = 30;

                row += 2;

                // 請求日
                worksheet.Range[$"G{row}"].Text = "請求日：";
                worksheet.Range[$"H{row}"].Text = invoice.InvoiceDate.ToString("yyyy年MM月dd日");
                worksheet.Range[$"H{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;

                row++;

                // 請求先情報
                worksheet.Range[$"B{row}:D{row}"].Merge();
                worksheet.Range[$"B{row}"].Text = $"{invoice.Client?.Name ?? ""} 様";
                worksheet.Range[$"B{row}"].CellStyle.Font.Size = 14;
                worksheet.Range[$"B{row}"].CellStyle.Font.Bold = true;
                worksheet.Range[$"B{row}"].RowHeight = 25;

                row += 2;

                // 車両情報
                worksheet.Range[$"B{row}"].Text = "車両：";
                worksheet.Range[$"C{row}:D{row}"].Merge();
                worksheet.Range[$"C{row}"].Text = invoice.Vehicle?.VehicleName ?? "";

                row++;
                worksheet.Range[$"B{row}"].Text = "ナンバー：";
                worksheet.Range[$"C{row}:D{row}"].Merge();
                string licensePlate = "";
                if (invoice.Vehicle != null)
                {
                    licensePlate = $"{invoice.Vehicle.LicensePlateLocation ?? ""} {invoice.Vehicle.LicensePlateClassification ?? ""} {invoice.Vehicle.LicensePlateHiragana ?? ""} {invoice.Vehicle.LicensePlateNumber ?? ""}".Trim();
                }
                worksheet.Range[$"C{row}"].Text = licensePlate;

                row++;
                worksheet.Range[$"B{row}"].Text = "走行距離：";
                worksheet.Range[$"C{row}"].Text = invoice.Mileage.HasValue ? $"{invoice.Mileage:N0} km" : "";

                row += 2;

                // 明細ヘッダー
                worksheet.Range[$"B{row}"].Text = "項目";
                worksheet.Range[$"C{row}"].Text = "型式";
                worksheet.Range[$"D{row}"].Text = "修理方法";
                worksheet.Range[$"E{row}"].Text = "数量";
                worksheet.Range[$"F{row}"].Text = "単価";
                worksheet.Range[$"G{row}"].Text = "金額";
                worksheet.Range[$"H{row}"].Text = "工賃";

                // ヘッダーのスタイル設定
                var headerRange = worksheet.Range[$"B{row}:H{row}"];
                headerRange.CellStyle.Font.Bold = true;
                headerRange.CellStyle.Color = Color.FromArgb(217, 217, 217);
                headerRange.CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                headerRange.CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                headerRange.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

                row++;

                // 明細行
                int detailStartRow = row;
                foreach (var detail in invoice.InvoiceDetails.OrderBy(d => d.DisplayOrder))
                {
                    worksheet.Range[$"B{row}"].Text = detail.ItemName;
                    worksheet.Range[$"C{row}"].Text = detail.Type ?? "";
                    worksheet.Range[$"D{row}"].Text = detail.RepairMethod ?? "";
                    worksheet.Range[$"E{row}"].Number = (double)detail.Quantity;
                    worksheet.Range[$"E{row}"].NumberFormat = "#,##0.0";
                    worksheet.Range[$"F{row}"].Number = (double)detail.UnitPrice;
                    worksheet.Range[$"F{row}"].NumberFormat = "¥#,##0";
                    worksheet.Range[$"G{row}"].Number = (double)(detail.Quantity * detail.UnitPrice);
                    worksheet.Range[$"G{row}"].NumberFormat = "¥#,##0";
                    worksheet.Range[$"H{row}"].Number = (double)detail.LaborCost;
                    worksheet.Range[$"H{row}"].NumberFormat = "¥#,##0";

                    // 数値列の右寄せ
                    worksheet.Range[$"E{row}:H{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;

                    row++;
                }

                // 明細の罫線
                if (row > detailStartRow)
                {
                    var detailRange = worksheet.Range[$"B{detailStartRow}:H{row - 1}"];
                    detailRange.BorderAround(ExcelLineStyle.Thin);
                    detailRange.BorderInside(ExcelLineStyle.Thin, ExcelKnownColors.Black);
                }

                row += 2;

                // 小計・消費税・合計
                int summaryStartCol = 6; // F列
                
                // 課税対象小計
                worksheet.Range[$"F{row}"].Text = "課税対象小計：";
                worksheet.Range[$"F{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range[$"G{row}"].Number = (double)invoice.TaxableSubTotal;
                worksheet.Range[$"G{row}"].NumberFormat = "¥#,##0";
                worksheet.Range[$"G{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;

                row++;

                // 非課税小計
                worksheet.Range[$"F{row}"].Text = "非課税小計：";
                worksheet.Range[$"F{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range[$"G{row}"].Number = (double)invoice.NonTaxableSubTotal;
                worksheet.Range[$"G{row}"].NumberFormat = "¥#,##0";
                worksheet.Range[$"G{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;

                row++;

                // 消費税
                worksheet.Range[$"F{row}"].Text = $"消費税（{invoice.TaxRate}%）：";
                worksheet.Range[$"F{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range[$"G{row}"].Number = (double)invoice.Tax;
                worksheet.Range[$"G{row}"].NumberFormat = "¥#,##0";
                worksheet.Range[$"G{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;

                row++;

                // 合計（太字・大きめのフォント）
                worksheet.Range[$"F{row}"].Text = "合計金額：";
                worksheet.Range[$"F{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range[$"F{row}"].CellStyle.Font.Bold = true;
                worksheet.Range[$"F{row}"].CellStyle.Font.Size = 12;
                worksheet.Range[$"G{row}"].Number = (double)invoice.Total;
                worksheet.Range[$"G{row}"].NumberFormat = "¥#,##0";
                worksheet.Range[$"G{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range[$"G{row}"].CellStyle.Font.Bold = true;
                worksheet.Range[$"G{row}"].CellStyle.Font.Size = 12;

                // 合計に罫線
                worksheet.Range[$"F{row}:G{row}"].BorderAround(ExcelLineStyle.Medium);

                row += 3;

                // 次回車検日
                if (invoice.NextInspectionDate.HasValue)
                {
                    worksheet.Range[$"B{row}"].Text = $"次回車検日：{invoice.NextInspectionDate.Value.ToString("yyyy年MM月dd日")}";
                }

                row += 2;

                // 備考
                if (!string.IsNullOrWhiteSpace(invoice.Notes))
                {
                    worksheet.Range[$"B{row}"].Text = "備考：";
                    row++;
                    worksheet.Range[$"B{row}:H{row}"].Merge();
                    worksheet.Range[$"B{row}"].Text = invoice.Notes;
                    worksheet.Range[$"B{row}"].WrapText = true;
                }

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