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

                // 列幅設定（テンプレートに合わせて調整）
                worksheet.SetColumnWidth(1, 8);    // A列
                worksheet.SetColumnWidth(2, 15);   // B列
                worksheet.SetColumnWidth(3, 12);   // C列
                worksheet.SetColumnWidth(4, 6);    // D列
                worksheet.SetColumnWidth(5, 9);    // E列
                worksheet.SetColumnWidth(6, 9);    // F列
                worksheet.SetColumnWidth(7, 9);    // G列
                worksheet.SetColumnWidth(8, 6);    // H列
                worksheet.SetColumnWidth(9, 6);    // I列
                worksheet.SetColumnWidth(10, 5);   // J列
                worksheet.SetColumnWidth(11, 10);  // K列
                worksheet.SetColumnWidth(12, 10);  // L列
                worksheet.SetColumnWidth(13, 10);  // M列
                worksheet.SetColumnWidth(14, 10);  // N列

                // 全体のフォントを游ゴシックに設定
                worksheet.UsedRange.CellStyle.Font.FontName = "游ゴシック";

                // タイトル（行1）
                worksheet.Range["C1:H1"].Merge();
                worksheet.Range["C1"].Text = "御　請　求　書";
                worksheet.Range["C1"].CellStyle.Font.Size = 20;
                worksheet.Range["C1"].CellStyle.Font.Bold = true;
                worksheet.Range["C1"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["C1"].CellStyle.VerticalAlignment = ExcelVAlign.VAlignCenter;
                worksheet.Range["C1"].CellStyle.Color = Color.FromArgb(146, 208, 80);
                worksheet.Range["C1"].RowHeight = 35;

                // 作成日（行3）
                worksheet.Range["J3"].Text = "作成日";
                worksheet.Range["K3:L3"].Merge();
                worksheet.Range["K3"].Text = $"令和{DateTime.Now.Year - 2018}年{DateTime.Now.Month}月{DateTime.Now.Day}日";

                // 発行者情報（行4-8の右側）
                if (issuerInfo != null)
                {
                    // 郵便番号（行4）
                    worksheet.Range["J4:L4"].Merge();
                    worksheet.Range["J4"].Text = issuerInfo.PostalCode;
                    
                    // 住所（行5）
                    worksheet.Range["J5:N5"].Merge();
                    worksheet.Range["J5"].Text = issuerInfo.Address;
                    
                    // 会社名（行6）
                    worksheet.Range["J6:N6"].Merge();
                    worksheet.Range["J6"].Text = issuerInfo.CompanyName;
                    
                    // 代表者（行7）
                    worksheet.Range["J7:N7"].Merge();
                    worksheet.Range["J7"].Text = $"代表　{issuerInfo.Position}　{issuerInfo.Name}";
                    
                    // 電話番号とFAX（行8）
                    worksheet.Range["J8:K8"].Merge();
                    worksheet.Range["J8"].Text = $"TEL {issuerInfo.PhoneNumber}";
                    worksheet.Range["L8:N8"].Merge();
                    worksheet.Range["L8"].Text = $"FAX {issuerInfo.FaxNumber}";
                }

                // 請求先情報（行4-6の左側）
                worksheet.Range["B4"].Text = "郵便番号";
                worksheet.Range["B5"].Text = "住所";
                worksheet.Range["B6"].Text = "氏名";
                worksheet.Range["D6"].Text = "様";

                // 合計金額（行9）
                worksheet.Range["B9"].Text = "合計金額";
                worksheet.Range["B9"].CellStyle.Font.Bold = true;
                
                // 合計金額セル（行11）
                worksheet.Range["C11:E11"].Merge();
                worksheet.Range["C11"].Text = $"¥{invoice.Total:N0}";
                worksheet.Range["C11"].CellStyle.Font.Size = 20;
                worksheet.Range["C11"].CellStyle.Font.Bold = true;
                worksheet.Range["C11"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["C11"].CellStyle.VerticalAlignment = ExcelVAlign.VAlignCenter;
                worksheet.Range["C11"].RowHeight = 30;
                worksheet.Range["C11:E11"].BorderAround(ExcelLineStyle.Medium);

                // 振込案内（行9の右側）
                worksheet.Range["H9:N9"].Merge();
                worksheet.Range["H9"].Text = "銀行振込の場合は、下記口座までお振込みください。";
                worksheet.Range["H9"].CellStyle.Color = Color.FromArgb(146, 208, 80);
                worksheet.Range["H9"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["H9"].CellStyle.Font.Bold = true;

                // 口座情報（行10-11）
                if (issuerInfo != null && !string.IsNullOrEmpty(issuerInfo.Bank1Name))
                {
                    worksheet.Range["H10:N10"].Merge();
                    worksheet.Range["H10"].Text = $"{issuerInfo.Bank1Name} {issuerInfo.Bank1BranchName} {issuerInfo.Bank1AccountType} {issuerInfo.Bank1AccountNumber} {issuerInfo.Bank1AccountHolder}";
                    worksheet.Range["H10"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                    worksheet.Range["H10"].CellStyle.Color = Color.FromArgb(146, 208, 80);
                }
                
                if (issuerInfo != null && !string.IsNullOrEmpty(issuerInfo.Bank2Name))
                {
                    worksheet.Range["H11:N11"].Merge();
                    worksheet.Range["H11"].Text = $"{issuerInfo.Bank2Name} {issuerInfo.Bank2BranchName} {issuerInfo.Bank2AccountType} {issuerInfo.Bank2AccountNumber} {issuerInfo.Bank2AccountHolder}";
                    worksheet.Range["H11"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                    worksheet.Range["H11"].CellStyle.Color = Color.FromArgb(146, 208, 80);
                }

                // 車両情報ヘッダー（行13）
                worksheet.Range["B13"].Text = "車両番号";
                worksheet.Range["B13"].CellStyle.Color = Color.FromArgb(217, 217, 217);
                worksheet.Range["B13"].CellStyle.Font.Bold = true;
                worksheet.Range["B13"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                
                worksheet.Range["D13:E13"].Merge();
                worksheet.Range["D13"].Text = "車名";
                worksheet.Range["D13"].CellStyle.Color = Color.FromArgb(217, 217, 217);
                worksheet.Range["D13"].CellStyle.Font.Bold = true;
                worksheet.Range["D13"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                
                worksheet.Range["F13:G13"].Merge();
                worksheet.Range["F13"].Text = "車体番号";
                worksheet.Range["F13"].CellStyle.Color = Color.FromArgb(217, 217, 217);
                worksheet.Range["F13"].CellStyle.Font.Bold = true;
                worksheet.Range["F13"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                
                worksheet.Range["I13"].Text = "初年度登録";
                worksheet.Range["I13"].CellStyle.Color = Color.FromArgb(217, 217, 217);
                worksheet.Range["I13"].CellStyle.Font.Bold = true;
                worksheet.Range["I13"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                
                worksheet.Range["L13:M13"].Merge();
                worksheet.Range["L13"].Text = "走行距離";
                worksheet.Range["L13"].CellStyle.Color = Color.FromArgb(217, 217, 217);
                worksheet.Range["L13"].CellStyle.Font.Bold = true;
                worksheet.Range["L13"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

                // 車両情報データ（行15）
                string licensePlate = "";
                if (invoice.Vehicle != null)
                {
                    licensePlate = $"{invoice.Vehicle.LicensePlateLocation ?? ""} {invoice.Vehicle.LicensePlateClassification ?? ""} {invoice.Vehicle.LicensePlateHiragana ?? ""} {invoice.Vehicle.LicensePlateNumber ?? ""}".Trim();
                }
                worksheet.Range["B15"].Text = licensePlate;
                worksheet.Range["D15:E15"].Merge();
                worksheet.Range["D15"].Text = invoice.Vehicle?.VehicleName ?? "";
                worksheet.Range["F15:G15"].Merge();
                worksheet.Range["F15"].Text = invoice.Vehicle?.ChassisNumber ?? "";
                worksheet.Range["I15"].Text = invoice.Vehicle?.FirstRegistrationDate?.ToString("yyyy/MM/dd") ?? "";
                worksheet.Range["L15:M15"].Merge();
                worksheet.Range["L15"].Text = invoice.Mileage.HasValue ? $"{invoice.Mileage:N0} km" : "";
                worksheet.Range["L15"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;

                // 明細ヘッダー（行17）
                worksheet.Range["B17:C17"].Merge();
                worksheet.Range["B17"].Text = "部品名称／項目";
                worksheet.Range["B17"].CellStyle.Color = Color.FromArgb(217, 217, 217);
                worksheet.Range["B17"].CellStyle.Font.Bold = true;
                worksheet.Range["B17"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["B17"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                worksheet.Range["B17"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                worksheet.Range["B17"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                worksheet.Range["B17"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
                
                worksheet.Range["D17:E17"].Merge();
                worksheet.Range["D17"].Text = "修理方法";
                worksheet.Range["D17"].CellStyle.Color = Color.FromArgb(217, 217, 217);
                worksheet.Range["D17"].CellStyle.Font.Bold = true;
                worksheet.Range["D17"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["D17"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                worksheet.Range["D17"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                worksheet.Range["D17"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                worksheet.Range["D17"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
                
                worksheet.Range["F17"].Text = "部品単価";
                worksheet.Range["F17"].CellStyle.Color = Color.FromArgb(217, 217, 217);
                worksheet.Range["F17"].CellStyle.Font.Bold = true;
                worksheet.Range["F17"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["F17"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                worksheet.Range["F17"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                worksheet.Range["F17"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                worksheet.Range["F17"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
                
                worksheet.Range["G17"].Text = "個数";
                worksheet.Range["G17"].CellStyle.Color = Color.FromArgb(217, 217, 217);
                worksheet.Range["G17"].CellStyle.Font.Bold = true;
                worksheet.Range["G17"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["G17"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                worksheet.Range["G17"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                worksheet.Range["G17"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                worksheet.Range["G17"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
                
                worksheet.Range["H17"].Text = "部品価格";
                worksheet.Range["H17"].CellStyle.Color = Color.FromArgb(217, 217, 217);
                worksheet.Range["H17"].CellStyle.Font.Bold = true;
                worksheet.Range["H17"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["H17"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                worksheet.Range["H17"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                worksheet.Range["H17"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                worksheet.Range["H17"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
                
                worksheet.Range["J17"].Text = "工賃";
                worksheet.Range["J17"].CellStyle.Color = Color.FromArgb(217, 217, 217);
                worksheet.Range["J17"].CellStyle.Font.Bold = true;
                worksheet.Range["J17"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["J17"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                worksheet.Range["J17"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                worksheet.Range["J17"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                worksheet.Range["J17"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;

                // 明細行（行18から開始、法定費用以外）
                int row = 18;
                var taxableDetails = invoice.InvoiceDetails
                    .Where(d => d.Type != "法定費用")
                    .OrderBy(d => d.DisplayOrder);
                    
                foreach (var detail in taxableDetails)
                {
                    worksheet.Range[$"B{row}:C{row}"].Merge();
                    worksheet.Range[$"B{row}"].Text = detail.ItemName;
                    worksheet.Range[$"B{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"B{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"B{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"B{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
                    
                    worksheet.Range[$"D{row}:E{row}"].Merge();
                    worksheet.Range[$"D{row}"].Text = detail.RepairMethod ?? "";
                    worksheet.Range[$"D{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"D{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"D{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"D{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
                    
                    worksheet.Range[$"F{row}"].Number = (double)detail.UnitPrice;
                    worksheet.Range[$"F{row}"].NumberFormat = "#,##0";
                    worksheet.Range[$"F{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                    worksheet.Range[$"F{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"F{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"F{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"F{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
                    
                    worksheet.Range[$"G{row}"].Number = (double)detail.Quantity;
                    worksheet.Range[$"G{row}"].NumberFormat = "#,##0.0";
                    worksheet.Range[$"G{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                    worksheet.Range[$"G{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"G{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"G{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"G{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
                    
                    worksheet.Range[$"H{row}"].Number = (double)(detail.Quantity * detail.UnitPrice);
                    worksheet.Range[$"H{row}"].NumberFormat = "#,##0";
                    worksheet.Range[$"H{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                    worksheet.Range[$"H{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"H{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"H{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"H{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
                    
                    worksheet.Range[$"J{row}"].Number = (double)detail.LaborCost;
                    worksheet.Range[$"J{row}"].NumberFormat = "#,##0";
                    worksheet.Range[$"J{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                    worksheet.Range[$"J{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"J{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"J{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"J{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;

                    row++;
                }

                // 空行を行38まで埋める（固定位置のため）
                while (row < 38)
                {
                    worksheet.Range[$"B{row}:C{row}"].Merge();
                    worksheet.Range[$"B{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"B{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"B{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"B{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
                    
                    worksheet.Range[$"D{row}:E{row}"].Merge();
                    worksheet.Range[$"D{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"D{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"D{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"D{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
                    
                    worksheet.Range[$"F{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"F{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"F{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"F{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
                    
                    worksheet.Range[$"G{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"G{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"G{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"G{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
                    
                    worksheet.Range[$"H{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"H{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"H{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"H{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
                    
                    worksheet.Range[$"J{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"J{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"J{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                    worksheet.Range[$"J{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
                    
                    row++;
                }

                // ページ小計（行39）
                worksheet.Range["G39"].Text = "ページ小計";
                worksheet.Range["G39"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["H39"].Number = (double)invoice.TaxableSubTotal;
                worksheet.Range["H39"].NumberFormat = "#,##0";
                worksheet.Range["H39"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range["J39"].Number = (double)invoice.TaxableSubTotal;
                worksheet.Range["J39"].NumberFormat = "#,##0";
                worksheet.Range["J39"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;

                // 非課税項目（行40）
                worksheet.Range["B40:C40"].Merge();
                worksheet.Range["B40"].Text = "非課税項目";
                worksheet.Range["B40"].CellStyle.Font.Bold = true;
                worksheet.Range["B40"].CellStyle.Color = Color.FromArgb(146, 208, 80);

                // 小計（行41）
                worksheet.Range["G41"].Text = "小計";
                worksheet.Range["G41"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["H41"].Number = (double)invoice.TaxableSubTotal;
                worksheet.Range["H41"].NumberFormat = "#,##0";
                worksheet.Range["H41"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range["J41"].Number = (double)invoice.TaxableSubTotal;
                worksheet.Range["J41"].NumberFormat = "#,##0";
                worksheet.Range["J41"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;

                // 非課税項目の詳細（行41-45）
                row = 41;
                var nonTaxableItems = invoice.InvoiceDetails
                    .Where(d => d.Type == "法定費用")
                    .OrderBy(d => d.DisplayOrder);

                foreach (var item in nonTaxableItems)
                {
                    worksheet.Range[$"B{row}"].Text = item.ItemName;
                    worksheet.Range[$"D{row}"].Number = (double)item.UnitPrice;
                    worksheet.Range[$"D{row}"].NumberFormat = "#,##0";
                    worksheet.Range[$"D{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                    row++;
                    if (row > 45) break; // 最大行45まで
                }

                // 課税計（行42）
                worksheet.Range["G42"].Text = "課税計";
                worksheet.Range["G42"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["J42"].Number = (double)invoice.TaxableSubTotal;
                worksheet.Range["J42"].NumberFormat = "#,##0";
                worksheet.Range["J42"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;

                // 印紙代（行43）
                worksheet.Range["G43"].Text = "印紙代";
                worksheet.Range["G43"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["J43"].Number = 1700; // 固定値
                worksheet.Range["J43"].NumberFormat = "#,##0";
                worksheet.Range["J43"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;

                // 証紙管理費（行44）
                worksheet.Range["G44"].Text = "証紙管理費";
                worksheet.Range["G44"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["J44"].Number = 400; // 固定値
                worksheet.Range["J44"].NumberFormat = "#,##0";
                worksheet.Range["J44"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;

                // 消費税 10%（行43）
                worksheet.Range["E43"].Text = "消費税 10%";
                worksheet.Range["E43"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                
                // 非課税額計（行44）
                worksheet.Range["E44"].Text = "非課税額計";
                worksheet.Range["E44"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["E44"].CellStyle.Color = Color.FromArgb(146, 208, 80);
                worksheet.Range["F44"].Number = (double)invoice.NonTaxableSubTotal;
                worksheet.Range["F44"].NumberFormat = "#,##0";
                worksheet.Range["F44"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;

                // 合計（行45）
                worksheet.Range["E45"].Text = "合計";
                worksheet.Range["E45"].CellStyle.Font.Bold = true;
                worksheet.Range["E45"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["J45"].Number = (double)invoice.Total;
                worksheet.Range["J45"].NumberFormat = "#,##0";
                worksheet.Range["J45"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range["J45"].CellStyle.Font.Bold = true;

                // 非課税項目計（行46）
                worksheet.Range["B46"].Text = "非課税項目計";
                worksheet.Range["B46"].CellStyle.Font.Bold = true;
                worksheet.Range["D46"].Number = (double)invoice.NonTaxableSubTotal;
                worksheet.Range["D46"].NumberFormat = "#,##0";
                worksheet.Range["D46"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range["D46"].CellStyle.Font.Bold = true;

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