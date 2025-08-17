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

                // 列幅設定（指定された値に変更）
                worksheet.SetColumnWidth(1, 7.88);    // A列
                worksheet.SetColumnWidth(2, 8.38);    // B列
                worksheet.SetColumnWidth(3, 6.38);    // C列
                worksheet.SetColumnWidth(4, 2.63);    // D列
                worksheet.SetColumnWidth(5, 8.38);    // E列
                worksheet.SetColumnWidth(6, 8.38);    // F列
                worksheet.SetColumnWidth(7, 0.77);    // G列
                worksheet.SetColumnWidth(8, 8.38);    // H列
                worksheet.SetColumnWidth(9, 3.13);    // I列
                worksheet.SetColumnWidth(10, 8.13);   // J列
                worksheet.SetColumnWidth(11, 8.38);   // K列
                worksheet.SetColumnWidth(12, 3.13);   // L列
                worksheet.SetColumnWidth(13, 8.38);   // M列
                worksheet.SetColumnWidth(14, 3.13);   // N列

                // 行の高さ設定
                worksheet.SetRowHeight(6, 6);
                worksheet.SetRowHeight(12, 6);
                worksheet.SetRowHeight(14, 6);
                worksheet.SetRowHeight(16, 6);

                // タイトル（行1）
                worksheet.Range["C1:K1"].Merge();
                worksheet.Range["C1"].Text = "御　請　求　書";
                worksheet.Range["C1"].CellStyle.Font.Size = 20;
                worksheet.Range["C1"].CellStyle.Font.Bold = true;
                worksheet.Range["C1"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["C1"].CellStyle.VerticalAlignment = ExcelVAlign.VAlignCenter;
                worksheet.Range["C1"].CellStyle.Color = Color.FromArgb(169, 208, 142); // #A9D08E
                worksheet.Range["C1"].RowHeight = 35;

                // 作成日（行2）
                worksheet.Range["K2"].Text = "作成日";
                worksheet.Range["K2"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range["L2:M2"].Merge();
                worksheet.Range["L2"].Text = $"令和{DateTime.Now.Year - 2018}年{DateTime.Now.Month}月{DateTime.Now.Day}日";

                // 発行者情報（行3-8の右側）
                if (issuerInfo != null)
                {
                    // 郵便番号（行3）
                    worksheet.Range["J3"].Text = issuerInfo.PostalCode;
                    
                    // 住所（行4）
                    worksheet.Range["J4"].Text = issuerInfo.Address;
                    
                    // 社名（行5）
                    worksheet.Range["J5"].Text = issuerInfo.CompanyName;
                    
                    // 役職・氏名（行6、全角空白でつなげる）
                    worksheet.Range["J6"].Text = $"{issuerInfo.Position}　{issuerInfo.Name}";
                    
                    // 電話番号（行8）
                    worksheet.Range["J8"].Text = $"TEL {issuerInfo.PhoneNumber}";
                    
                    // FAX番号（行8）
                    worksheet.Range["L8"].Text = $"FAX {issuerInfo.FaxNumber}";
                }

                // 請求先情報（行4-6の左側）
                worksheet.Range["A4"].Text = "郵便番号";
                worksheet.Range["B4"].Text = invoice.Client?.Zip ?? "";
                
                worksheet.Range["A5"].Text = "住所";
                worksheet.Range["B5"].Text = invoice.Client?.Address ?? "";
                
                worksheet.Range["A7"].Text = "氏名";
                worksheet.Range["B7"].Text = invoice.Client?.Name ?? "";
                worksheet.Range["F7"].Text = "様";

                // 合計金額（行9）
                worksheet.Range["A9"].Text = "合計金額";
                worksheet.Range["A9"].CellStyle.Font.Bold = true;
                
                // 合計金額セル（B10-E11セル結合）
                worksheet.Range["B10:E11"].Merge();
                worksheet.Range["B10"].Text = $"¥{invoice.Total:N0}";
                worksheet.Range["B10"].CellStyle.Font.Size = 20;
                worksheet.Range["B10"].CellStyle.Font.Bold = true;
                worksheet.Range["B10"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["B10"].CellStyle.VerticalAlignment = ExcelVAlign.VAlignBottom;
                worksheet.Range["B10"].RowHeight = 30;
                // 下線を二重線に変更
                worksheet.Range["B11:E11"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Double;

                // 振込案内（行9の右側）
                worksheet.Range["H9:N9"].Merge();
                worksheet.Range["H9"].Text = "銀行振込の場合は、下記口座までお振込みください。";
                worksheet.Range["H9"].CellStyle.Color = Color.FromArgb(169, 208, 142); // #A9D08E
                worksheet.Range["H9"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["H9"].CellStyle.Font.Bold = true;
                // H9:N9に極細罫線
                worksheet.Range["H9"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["H9"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["H9"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["I9"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["I9"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["J9"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["J9"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["K9"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["K9"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["L9"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["L9"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["M9"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["M9"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["N9"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["N9"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["N9"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;

                // 口座情報（行10-11）
                if (issuerInfo != null && !string.IsNullOrEmpty(issuerInfo.Bank1Name))
                {
                    worksheet.Range["H10:N10"].Merge();
                    worksheet.Range["H10"].Text = $"{issuerInfo.Bank1Name} {issuerInfo.Bank1BranchName} {issuerInfo.Bank1AccountType} {issuerInfo.Bank1AccountNumber} {issuerInfo.Bank1AccountHolder}";
                    worksheet.Range["H10"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                    worksheet.Range["H10"].CellStyle.Color = Color.FromArgb(226, 239, 218); // #E2EFDA
                    // H10:N10に極細罫線
                    worksheet.Range["H10"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["H10"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["H10"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["I10"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["I10"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["J10"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["J10"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["K10"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["K10"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["L10"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["L10"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["M10"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["M10"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["N10"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["N10"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["N10"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                }
                
                if (issuerInfo != null && !string.IsNullOrEmpty(issuerInfo.Bank2Name))
                {
                    worksheet.Range["H11:N11"].Merge();
                    worksheet.Range["H11"].Text = $"{issuerInfo.Bank2Name} {issuerInfo.Bank2BranchName} {issuerInfo.Bank2AccountType} {issuerInfo.Bank2AccountNumber} {issuerInfo.Bank2AccountHolder}";
                    worksheet.Range["H11"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                    worksheet.Range["H11"].CellStyle.Color = Color.FromArgb(226, 239, 218); // #E2EFDA
                    // H11:N11に極細罫線
                    worksheet.Range["H11"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["H11"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["H11"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["I11"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["I11"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["J11"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["J11"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["K11"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["K11"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["L11"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["L11"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["M11"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["M11"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["N11"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["N11"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range["N11"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                }

                // 車両情報ヘッダー（行13）
                worksheet.Range["A13"].Text = "車両番号";
                worksheet.Range["A13"].CellStyle.Color = Color.FromArgb(169, 208, 142); // #A9D08E
                worksheet.Range["A13"].CellStyle.Font.Bold = true;
                worksheet.Range["A13"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                
                worksheet.Range["D13"].Text = "車名";
                worksheet.Range["D13"].CellStyle.Color = Color.FromArgb(169, 208, 142); // #A9D08E
                worksheet.Range["D13"].CellStyle.Font.Bold = true;
                worksheet.Range["D13"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                
                worksheet.Range["F13"].Text = "車体番号";
                worksheet.Range["F13"].CellStyle.Color = Color.FromArgb(169, 208, 142); // #A9D08E
                worksheet.Range["F13"].CellStyle.Font.Bold = true;
                worksheet.Range["F13"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                
                worksheet.Range["K13"].Text = "初年度登録";
                worksheet.Range["K13"].CellStyle.Color = Color.FromArgb(169, 208, 142); // #A9D08E
                worksheet.Range["K13"].CellStyle.Font.Bold = true;
                worksheet.Range["K13"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

                // 車両番号 データ B13(C13までセル結合)
                worksheet.Range["B13:C13"].Merge();
                string licensePlate = "";
                if (invoice.Vehicle != null)
                {
                    licensePlate = $"{invoice.Vehicle.LicensePlateLocation ?? ""} {invoice.Vehicle.LicensePlateClassification ?? ""} {invoice.Vehicle.LicensePlateHiragana ?? ""} {invoice.Vehicle.LicensePlateNumber ?? ""}".Trim();
                }
                worksheet.Range["B13"].Text = licensePlate;
                worksheet.Range["B13"].CellStyle.Color = Color.FromArgb(226, 239, 218); // #E2EFDA
                
                // 車名 データ E13
                worksheet.Range["E13"].Text = invoice.Vehicle?.VehicleName ?? "";
                worksheet.Range["E13"].CellStyle.Color = Color.FromArgb(226, 239, 218); // #E2EFDA
                
                // 車体番号 データ G13(J13までセル結合)
                worksheet.Range["G13:J13"].Merge();
                worksheet.Range["G13"].Text = invoice.Vehicle?.ChassisNumber ?? "";
                worksheet.Range["G13"].CellStyle.Color = Color.FromArgb(226, 239, 218); // #E2EFDA
                
                // 初年度登録 データ L13(N13までセル結合)
                worksheet.Range["L13:N13"].Merge();
                worksheet.Range["L13"].Text = invoice.Vehicle?.FirstRegistrationDate?.ToString("yyyy/MM/dd") ?? "";
                worksheet.Range["L13"].CellStyle.Color = Color.FromArgb(226, 239, 218); // #E2EFDA
                
                // 車検満了日 ラベル A15 データ B15(E15までセル結合)
                worksheet.Range["A15"].Text = "車検満了日";
                worksheet.Range["A15"].CellStyle.Color = Color.FromArgb(169, 208, 142); // #A9D08E
                worksheet.Range["A15"].CellStyle.Font.Bold = true;
                worksheet.Range["A15"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                
                worksheet.Range["B15:E15"].Merge();
                worksheet.Range["B15"].Text = invoice.Vehicle?.InspectionExpiryDate?.ToString("yyyy/MM/dd") ?? "";
                worksheet.Range["B15"].CellStyle.Color = Color.FromArgb(226, 239, 218); // #E2EFDA
                
                // 形式 ラベル F15 データ G15(J15までセル結合)
                worksheet.Range["F15"].Text = "形式";
                worksheet.Range["F15"].CellStyle.Color = Color.FromArgb(169, 208, 142); // #A9D08E
                worksheet.Range["F15"].CellStyle.Font.Bold = true;
                worksheet.Range["F15"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                
                worksheet.Range["G15:J15"].Merge();
                worksheet.Range["G15"].Text = invoice.Vehicle?.VehicleModel ?? "";
                worksheet.Range["G15"].CellStyle.Color = Color.FromArgb(226, 239, 218); // #E2EFDA
                
                // 走行距離 ラベル K15 データ L15(N15までセル結合)
                worksheet.Range["K15"].Text = "走行距離";
                worksheet.Range["K15"].CellStyle.Color = Color.FromArgb(169, 208, 142); // #A9D08E
                worksheet.Range["K15"].CellStyle.Font.Bold = true;
                worksheet.Range["K15"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                
                worksheet.Range["L15:N15"].Merge();
                worksheet.Range["L15"].Text = invoice.Mileage.HasValue ? $"{invoice.Mileage:N0} km" : "";
                worksheet.Range["L15"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range["L15"].CellStyle.Color = Color.FromArgb(226, 239, 218); // #E2EFDA

                // 明細ヘッダー（行17）
                // 部品名称／項目 A17-E17
                worksheet.Range["A17:E17"].Merge();
                worksheet.Range["A17"].Text = "部品名称／項目";
                worksheet.Range["A17"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["A17"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["A17"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["A17"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["B17"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["B17"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["C17"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["C17"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["D17"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["D17"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["E17"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["E17"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["E17"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                
                // 修理方法 F17-G17
                worksheet.Range["F17:G17"].Merge();
                worksheet.Range["F17"].Text = "修理方法";
                worksheet.Range["F17"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["F17"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["F17"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["F17"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["G17"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["G17"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["G17"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                
                // 部品単価 H17-I17
                worksheet.Range["H17:I17"].Merge();
                worksheet.Range["H17"].Text = "部品単価";
                worksheet.Range["H17"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["H17"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["H17"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["H17"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["I17"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["I17"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["I17"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                
                // 個数 J17
                worksheet.Range["J17"].Text = "個数";
                worksheet.Range["J17"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["J17"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["J17"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["J17"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["J17"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                
                // 部品価格 K17-L17
                worksheet.Range["K17:L17"].Merge();
                worksheet.Range["K17"].Text = "部品価格";
                worksheet.Range["K17"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["K17"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["K17"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["K17"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["L17"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["L17"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["L17"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                
                // 工賃 M17-N17
                worksheet.Range["M17:N17"].Merge();
                worksheet.Range["M17"].Text = "工賃";
                worksheet.Range["M17"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["M17"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["M17"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["M17"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["N17"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["N17"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["N17"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;

                // 明細行（行18から開始、法定費用以外）
                int row = 18;
                var taxableDetails = invoice.InvoiceDetails
                    .Where(d => d.Type != "法定費用")
                    .OrderBy(d => d.DisplayOrder);
                
                // 部品価格と工賃の小計を計算
                decimal partsSubTotal = 0;
                decimal laborSubTotal = 0;
                    
                foreach (var detail in taxableDetails)
                {
                    // 部品名称 A-E
                    worksheet.Range[$"A{row}:E{row}"].Merge();
                    worksheet.Range[$"A{row}"].Text = detail.ItemName;
                    worksheet.Range[$"A{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignLeft;
                    worksheet.Range[$"A{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"A{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"A{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"B{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"B{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"C{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"C{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"D{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"D{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"E{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"E{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"E{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                    
                    // 修理方法 F-G
                    worksheet.Range[$"F{row}:G{row}"].Merge();
                    worksheet.Range[$"F{row}"].Text = detail.RepairMethod ?? "";
                    worksheet.Range[$"F{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                    worksheet.Range[$"F{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"F{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"F{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"G{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"G{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"G{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                    
                    // 部品単価 H-I
                    worksheet.Range[$"H{row}:I{row}"].Merge();
                    worksheet.Range[$"H{row}"].Number = (double)detail.UnitPrice;
                    worksheet.Range[$"H{row}"].NumberFormat = "#,##0";
                    worksheet.Range[$"H{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                    worksheet.Range[$"H{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"H{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"H{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"I{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"I{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"I{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                    
                    // 個数 J
                    worksheet.Range[$"J{row}"].Number = (double)detail.Quantity;
                    worksheet.Range[$"J{row}"].NumberFormat = "#,##0.0";
                    worksheet.Range[$"J{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                    worksheet.Range[$"J{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"J{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"J{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"J{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                    
                    // 部品価格 K-L
                    worksheet.Range[$"K{row}:L{row}"].Merge();
                    worksheet.Range[$"K{row}"].Number = (double)(detail.Quantity * detail.UnitPrice);
                    worksheet.Range[$"K{row}"].NumberFormat = "#,##0";
                    worksheet.Range[$"K{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                    worksheet.Range[$"K{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"K{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"K{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"L{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"L{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"L{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                    
                    // 工賃 M-N
                    worksheet.Range[$"M{row}:N{row}"].Merge();
                    worksheet.Range[$"M{row}"].Number = (double)detail.LaborCost;
                    worksheet.Range[$"M{row}"].NumberFormat = "#,##0";
                    worksheet.Range[$"M{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                    worksheet.Range[$"M{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"M{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"M{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"N{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"N{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"N{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                    
                    // 小計に加算
                    partsSubTotal += detail.Quantity * detail.UnitPrice;
                    laborSubTotal += detail.LaborCost;

                    row++;
                }

                // 空行を行38まで埋める（固定位置のため）
                while (row <= 38)
                {
                    // 部品名称 A-E
                    worksheet.Range[$"A{row}:E{row}"].Merge();
                    worksheet.Range[$"A{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"A{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"A{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"B{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"B{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"C{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"C{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"D{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"D{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"E{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"E{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"E{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                    
                    // 修理方法 F-G
                    worksheet.Range[$"F{row}:G{row}"].Merge();
                    worksheet.Range[$"F{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"F{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"F{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"G{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"G{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"G{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                    
                    // 部品単価 H-I
                    worksheet.Range[$"H{row}:I{row}"].Merge();
                    worksheet.Range[$"H{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"H{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"H{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"I{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"I{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"I{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                    
                    // 個数 J
                    worksheet.Range[$"J{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"J{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"J{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"J{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                    
                    // 部品価格 K-L
                    worksheet.Range[$"K{row}:L{row}"].Merge();
                    worksheet.Range[$"K{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"K{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"K{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"L{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"L{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"L{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                    
                    // 工賃 M-N
                    worksheet.Range[$"M{row}:N{row}"].Merge();
                    worksheet.Range[$"M{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"M{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"M{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"N{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"N{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"N{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                    
                    row++;
                }

                // ページ小計（行39）
                // ページ小計ラベル H39:J39
                worksheet.Range["H39:J39"].Merge();
                worksheet.Range["H39"].Text = "ページ小計";
                worksheet.Range["H39"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["H39"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["H39"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["H39"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["I39"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["I39"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["J39"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["J39"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["J39"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                
                // 部品価格小計 K39:L39
                worksheet.Range["K39:L39"].Merge();
                worksheet.Range["K39"].Number = (double)partsSubTotal;
                worksheet.Range["K39"].NumberFormat = "#,##0";
                worksheet.Range["K39"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range["K39"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["K39"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["K39"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["L39"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["L39"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["L39"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                
                // 工賃小計 M39:N39
                worksheet.Range["M39:N39"].Merge();
                worksheet.Range["M39"].Number = (double)laborSubTotal;
                worksheet.Range["M39"].NumberFormat = "#,##0";
                worksheet.Range["M39"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range["M39"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["M39"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["M39"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["N39"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["N39"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["N39"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;

                // 非課税項目（行40）
                worksheet.Range["A40:F40"].Merge();
                worksheet.Range["A40"].Text = "非課税項目";
                worksheet.Range["A40"].CellStyle.Font.Bold = true;
                worksheet.Range["A40"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["A40"].CellStyle.Color = Color.FromArgb(169, 208, 142); // #A9D08E
                worksheet.Range["A40"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["A40"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["A40"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["B40"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["B40"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["C40"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["C40"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["D40"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["D40"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["E40"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["E40"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["F40"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["F40"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["F40"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;

                // 小計（行41）
                worksheet.Range["H41:J41"].Merge();
                worksheet.Range["H41"].Text = "小計";
                worksheet.Range["H41"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["H41"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["H41"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["I41"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["I41"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["J41"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["J41"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["J41"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                
                // 部品価格の小計 K41:L41
                worksheet.Range["K41:L41"].Merge();
                worksheet.Range["K41"].Number = (double)partsSubTotal;
                worksheet.Range["K41"].NumberFormat = "#,##0";
                worksheet.Range["K41"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range["K41"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["K41"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["K41"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["L41"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["L41"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["L41"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                
                // 工賃の小計 M41:N41
                worksheet.Range["M41:N41"].Merge();
                worksheet.Range["M41"].Number = (double)laborSubTotal;
                worksheet.Range["M41"].NumberFormat = "#,##0";
                worksheet.Range["M41"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range["M41"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["M41"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["M41"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["N41"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["N41"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["N41"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;

                // 非課税項目の詳細（行41-45）
                row = 41;
                var nonTaxableItems = invoice.InvoiceDetails
                    .Where(d => d.Type == "法定費用")
                    .OrderBy(d => d.DisplayOrder);
                
                decimal nonTaxableTotal = 0;

                foreach (var item in nonTaxableItems)
                {
                    if (row > 45) break; // 最大行45まで
                    
                    // 項目名 A-E (セル結合)
                    worksheet.Range[$"A{row}:E{row}"].Merge();
                    worksheet.Range[$"A{row}"].Text = item.ItemName;
                    worksheet.Range[$"A{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"A{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"A{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"B{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"B{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"C{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"C{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"D{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"D{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"E{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"E{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"E{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                    
                    // 金額 F
                    worksheet.Range[$"F{row}"].Number = (double)item.UnitPrice;
                    worksheet.Range[$"F{row}"].NumberFormat = "#,##0";
                    worksheet.Range[$"F{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                    worksheet.Range[$"F{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"F{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"F{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"F{row}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                    
                    nonTaxableTotal += item.UnitPrice;
                    row++;
                }
                
                // 残りの行を空白で埋める（行45まで）
                for (int i = row; i <= 45; i++)
                {
                    worksheet.Range[$"A{i}:E{i}"].Merge();
                    worksheet.Range[$"A{i}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"A{i}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"A{i}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"B{i}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"B{i}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"C{i}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"C{i}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"D{i}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"D{i}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"E{i}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"E{i}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"E{i}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                    
                    worksheet.Range[$"F{i}"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"F{i}"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"F{i}"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                    worksheet.Range[$"F{i}"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                }

                // 課税額計（行42）
                worksheet.Range["H42:J42"].Merge();
                worksheet.Range["H42"].Text = "課税額計";
                worksheet.Range["H42"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["H42"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["H42"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["I42"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["I42"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["J42"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["J42"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["J42"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                
                // K42 + M42
                decimal taxableTotal = partsSubTotal + laborSubTotal;
                worksheet.Range["K42:N42"].Merge();
                worksheet.Range["K42"].Number = (double)taxableTotal;
                worksheet.Range["K42"].NumberFormat = "#,##0";
                worksheet.Range["K42"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range["K42"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["K42"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["K42"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["L42"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["L42"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["M42"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["M42"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["N42"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["N42"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["N42"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;

                // 消費税 10%（行43）
                worksheet.Range["H43:J43"].Merge();
                worksheet.Range["H43"].Text = "消費税 10%";
                worksheet.Range["H43"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["H43"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["H43"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["I43"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["I43"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["J43"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["J43"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["J43"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                
                // K42 * 0.1 小数点以下切り捨て
                int tax = (int)(taxableTotal * 0.1m);
                worksheet.Range["K43:N43"].Merge();
                worksheet.Range["K43"].Number = (double)tax;
                worksheet.Range["K43"].NumberFormat = "#,##0";
                worksheet.Range["K43"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range["K43"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["K43"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["K43"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["L43"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["L43"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["M43"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["M43"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["N43"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["N43"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["N43"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;

                // 非課税額計（行44）
                worksheet.Range["H44:J44"].Merge();
                worksheet.Range["H44"].Text = "非課税額計";
                worksheet.Range["H44"].CellStyle.Color = Color.FromArgb(169, 208, 142); // #A9D08E
                worksheet.Range["H44"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["H44"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["H44"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["I44"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["I44"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["J44"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["J44"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["J44"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                
                // =F46
                worksheet.Range["K44:N44"].Merge();
                worksheet.Range["K44"].Formula = "=F46";
                worksheet.Range["K44"].NumberFormat = "#,##0";
                worksheet.Range["K44"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range["K44"].CellStyle.Color = Color.FromArgb(226, 239, 218); // #E2EFDA
                worksheet.Range["K44"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["K44"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["K44"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["L44"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["L44"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["M44"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["M44"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["N44"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["N44"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["N44"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;

                // 合計（行45）
                worksheet.Range["H45:J45"].Merge();
                worksheet.Range["H45"].Text = "合計";
                worksheet.Range["H45"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["H45"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["H45"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["I45"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["I45"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["J45"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["J45"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["J45"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                
                // K42 + K43 + K44
                worksheet.Range["K45:N45"].Merge();
                worksheet.Range["K45"].Formula = "=K42+K43+K44";
                worksheet.Range["K45"].NumberFormat = "#,##0";
                worksheet.Range["K45"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range["K45"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["K45"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["K45"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["L45"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["L45"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["M45"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["M45"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["N45"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["N45"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["N45"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;

                // 非課税額計（行46）
                worksheet.Range["A46:E46"].Merge();
                worksheet.Range["A46"].Text = "非課税額計";
                worksheet.Range["A46"].CellStyle.Color = Color.FromArgb(169, 208, 142); // #A9D08E
                worksheet.Range["A46"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["A46"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["A46"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["B46"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["B46"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["C46"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["C46"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["D46"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["D46"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["E46"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["E46"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["E46"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
                
                worksheet.Range["F46"].Number = (double)nonTaxableTotal;
                worksheet.Range["F46"].NumberFormat = "#,##0";
                worksheet.Range["F46"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range["F46"].CellStyle.Color = Color.FromArgb(226, 239, 218); // #E2EFDA
                worksheet.Range["F46"].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["F46"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["F46"].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
                worksheet.Range["F46"].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;

                // 全体のフォントを游ゴシックに設定（すべてのコンテンツ書き込み後）
                if (worksheet.UsedRange != null)
                {
                    worksheet.UsedRange.CellStyle.Font.FontName = "游ゴシック";
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