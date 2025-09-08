using AutoDealerSphere.Shared.Models;
using Syncfusion.XlsIO;
using System.IO;
using Syncfusion.Drawing;

namespace AutoDealerSphere.Server.Services
{
    public class ExcelExportService : IExcelExportService
    {
        // 色定数
        private static readonly Color HeaderColor = Color.FromArgb(169, 208, 142); // #A9D08E
        private static readonly Color DataColor = Color.FromArgb(226, 239, 218); // #E2EFDA
        private readonly IInvoiceService _invoiceService;
        private readonly IIssuerInfoService _issuerInfoService;

        public ExcelExportService(IInvoiceService invoiceService, IIssuerInfoService issuerInfoService)
        {
            _invoiceService = invoiceService;
            _issuerInfoService = issuerInfoService;
        }

        // 複数のセルにテキストを一括設定
        private void SetCellTexts(IWorksheet worksheet, Dictionary<string, string> cellValues)
        {
            foreach (var kvp in cellValues)
            {
                worksheet.Range[kvp.Key].Text = kvp.Value;
            }
        }

        // 複数のセルに数値を一括設定
        private void SetCellNumbers(IWorksheet worksheet, Dictionary<string, double> cellValues, string numberFormat = null)
        {
            foreach (var kvp in cellValues)
            {
                worksheet.Range[kvp.Key].Number = kvp.Value;
                if (numberFormat != null)
                {
                    worksheet.Range[kvp.Key].NumberFormat = numberFormat;
                }
            }
        }

        // セルを結合してテキストと書式を設定
        private void MergeAndSetCell(IWorksheet worksheet, string range, string text, Action<IStyle> styleAction = null)
        {
            worksheet.Range[range].Merge();
            worksheet.Range[range.Split(':')[0]].Text = text;
            if (styleAction != null)
            {
                styleAction(worksheet.Range[range.Split(':')[0]].CellStyle);
            }
        }

        // セルを結合して数値を設定（書式と罫線も含む）
        private void MergeAndSetNumber(IWorksheet worksheet, string range, double value, 
            string numberFormat = "#,##0", ExcelHAlign align = ExcelHAlign.HAlignRight, 
            ExcelLineStyle? borderStyle = null, Color? bgColor = null)
        {
            worksheet.Range[range].Merge();
            var firstCell = range.Split(':')[0];
            worksheet.Range[firstCell].Number = value;
            worksheet.Range[firstCell].NumberFormat = numberFormat;
            worksheet.Range[firstCell].CellStyle.HorizontalAlignment = align;
            
            if (bgColor.HasValue)
            {
                worksheet.Range[firstCell].CellStyle.Color = bgColor.Value;
            }
            
            if (borderStyle.HasValue)
            {
                SetRangeBorders(worksheet, range, borderStyle.Value);
            }
        }

        // 複数の数値セルを一括設定（マージ、書式、罫線含む）
        private void SetMergedNumberCells(IWorksheet worksheet, params MergedNumberCell[] cells)
        {
            foreach (var cell in cells)
            {
                MergeAndSetNumber(worksheet, cell.Range, cell.Value, 
                    cell.NumberFormat ?? "#,##0", 
                    cell.Align ?? ExcelHAlign.HAlignRight, 
                    cell.BorderStyle, 
                    cell.BgColor);
            }
        }

        // マージされた数値セルの定義
        private class MergedNumberCell
        {
            public string Range { get; set; }
            public double Value { get; set; }
            public string NumberFormat { get; set; }
            public ExcelHAlign? Align { get; set; }
            public ExcelLineStyle? BorderStyle { get; set; }
            public Color? BgColor { get; set; }
        }

        // セル範囲に罫線を設定
        private void SetRangeBorders(IWorksheet worksheet, string range, ExcelLineStyle lineStyle)
        {
            var cells = range.Contains(":") ? range.Split(':') : new[] { range, range };
            var startCell = cells[0];
            var endCell = cells[1];
            
            // 範囲内の全セルを取得
            var rangeObj = worksheet.Range[range];
            var startCol = GetColumnIndex(startCell);
            var endCol = GetColumnIndex(endCell);
            var row = GetRowNumber(startCell);
            
            for (int col = startCol; col <= endCol; col++)
            {
                var cellAddress = GetCellAddress(col, row);
                var cell = worksheet.Range[cellAddress];
                
                // 上下の罫線は全セルに設定
                cell.CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = lineStyle;
                cell.CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = lineStyle;
                
                // 左罫線は最初のセルのみ
                if (col == startCol)
                {
                    cell.CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = lineStyle;
                }
                
                // 右罫線は最後のセルのみ
                if (col == endCol)
                {
                    cell.CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = lineStyle;
                }
            }
        }

        // 複数のセル範囲に罫線を一括設定
        private void SetMultipleRangeBorders(IWorksheet worksheet, string[] ranges, ExcelLineStyle lineStyle)
        {
            foreach (var range in ranges)
            {
                SetRangeBorders(worksheet, range, lineStyle);
            }
        }

        // セルの列インデックスを取得
        private int GetColumnIndex(string cellAddress)
        {
            string columnPart = "";
            foreach (char c in cellAddress)
            {
                if (char.IsLetter(c))
                    columnPart += c;
                else
                    break;
            }
            
            int index = 0;
            for (int i = 0; i < columnPart.Length; i++)
            {
                index = index * 26 + (columnPart[i] - 'A' + 1);
            }
            return index;
        }

        // セルの行番号を取得
        private int GetRowNumber(string cellAddress)
        {
            string rowPart = "";
            foreach (char c in cellAddress)
            {
                if (char.IsDigit(c))
                    rowPart += c;
            }
            return int.Parse(rowPart);
        }

        // 列インデックスからセルアドレスを生成
        private string GetCellAddress(int columnIndex, int row)
        {
            string columnLetter = "";
            while (columnIndex > 0)
            {
                columnIndex--;
                columnLetter = (char)('A' + columnIndex % 26) + columnLetter;
                columnIndex /= 26;
            }
            return columnLetter + row;
        }

        public async Task<byte[]> ExportInvoiceToExcelAsync(int invoiceId)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(invoiceId);
            if (invoice == null)
            {
                throw new InvalidOperationException($"Invoice with ID {invoiceId} not found.");
            }

            // 同一請求書番号の全ての請求書を取得
            var relatedInvoices = await _invoiceService.GetInvoicesByInvoiceNumberAsync(invoice.InvoiceNumber);
            
            // 発行者情報を取得
            var issuerInfo = await _issuerInfoService.GetIssuerInfoAsync();

            using (ExcelEngine excelEngine = new ExcelEngine())
            {
                IApplication application = excelEngine.Excel;
                application.DefaultVersion = ExcelVersion.Excel2016;

                // テンプレートファイルを開く
                var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "invoice-template.xlsx");
                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException($"Template file not found: {templatePath}");
                }

                IWorkbook workbook = application.Workbooks.Open(templatePath);
                
                // 複数の請求書がある場合は、追加のシートを作成
                for (int i = 0; i < relatedInvoices.Count; i++)
                {
                    var currentInvoice = relatedInvoices[i];
                    IWorksheet worksheet;
                    if (i == 0)
                    {
                        // 最初のシートは既存のものを使用
                        worksheet = workbook.Worksheets[0];
                    }
                    else
                    {
                        // 2枚目以降は最後のシートの後にコピー
                        worksheet = workbook.Worksheets.AddCopyAfter(workbook.Worksheets[0], workbook.Worksheets[workbook.Worksheets.Count - 1]);
                    }

                    // シート名を{InvoiceNumber}-{Subnumber}形式に設定
                    var sheetName = currentInvoice.Subnumber > 1 
                        ? $"{currentInvoice.InvoiceNumber}-{currentInvoice.Subnumber}" 
                        : currentInvoice.InvoiceNumber;
                    worksheet.Name = sheetName;

                    // テンプレートにデータを投入
                    await PopulateTemplateWithDataAsync(worksheet, currentInvoice, issuerInfo, i == 0);
                }

                // MemoryStreamに保存
                using (MemoryStream stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        // ライセンスプレートをフォーマット
        private string FormatLicensePlate(Vehicle vehicle)
        {
            if (vehicle == null) return "";
            return $"{vehicle.LicensePlateLocation ?? ""} {vehicle.LicensePlateClassification ?? ""} {vehicle.LicensePlateHiragana ?? ""} {vehicle.LicensePlateNumber ?? ""}".Trim();
        }






        
        // テンプレートにデータを投入
        private async Task PopulateTemplateWithDataAsync(IWorksheet worksheet, Invoice invoice, IssuerInfo issuerInfo, bool isFirstSheet = true)
        {
            // 請求書情報（L2:請求書番号、L3:請求書発行日、L4:インボイス番号）
            PopulateInvoiceInfo(worksheet, invoice, issuerInfo);
            
            // 発行者情報
            PopulateIssuerInfo(worksheet, issuerInfo);
            
            // 請求先情報
            PopulateClientInfo(worksheet, invoice.Client);
            
            // 合計金額（最初のシートのみ表示）
            if (isFirstSheet)
            {
                // 同一請求書番号の全ての合計を計算
                var relatedInvoices = await _invoiceService.GetInvoicesByInvoiceNumberAsync(invoice.InvoiceNumber);
                var totalAmount = relatedInvoices.Sum(i => i.Total);
                PopulateTotalAmount(worksheet, totalAmount);
            }
            else
            {
                // 2枚目以降は合計を表示しない
                worksheet.Range["B12"].Text = "";
            }
            
            // 振込先口座情報
            PopulateBankAccountInfo(worksheet, issuerInfo);
            
            // 車両情報
            PopulateVehicleInfo(worksheet, invoice.Vehicle, invoice.Mileage);
            
            // 請求明細
            PopulateInvoiceDetails(worksheet, invoice);
            
            // 非課税項目
            PopulateNonTaxableItems(worksheet, invoice);
            
            // 合計計算（最初のシートのみ）
            if (isFirstSheet)
            {
                // 同一請求書番号の全ての請求書の合計を計算
                var relatedInvoices = await _invoiceService.GetInvoicesByInvoiceNumberAsync(invoice.InvoiceNumber);
                PopulateAggregatedTotals(worksheet, relatedInvoices);
            }
            else
            {
                PopulateTotals(worksheet, invoice);
            }
        }

        // 請求書情報を設定（L2:請求書番号、L3:請求書発行日、L4:インボイス番号）
        private void PopulateInvoiceInfo(IWorksheet worksheet, Invoice invoice, IssuerInfo issuerInfo)
        {
            // L2: 請求書番号（複数車両の場合は{InvoiceNumber}-{Subnumber}の形式）
            var displayInvoiceNumber = invoice.Subnumber > 1 ? $"{invoice.InvoiceNumber}-{invoice.Subnumber}" : invoice.InvoiceNumber;
            worksheet.Range["L2"].Text = displayInvoiceNumber ?? "";
            
            // L3: 請求書発行日
            worksheet.Range["L3"].Text = invoice.InvoiceDate.ToString("yyyy/MM/dd");
            
            // L4: インボイス番号
            worksheet.Range["L4"].Text = issuerInfo?.InvoiceNumber ?? "";
        }

        // 発行者情報を設定（J3からの行が2行下にずれる）
        private void PopulateIssuerInfo(IWorksheet worksheet, IssuerInfo issuerInfo)
        {
            if (issuerInfo == null) return;
            
            worksheet.Range["J5"].Text = issuerInfo.PostalCode ?? "";
            worksheet.Range["J6"].Text = issuerInfo.Address ?? "";
            worksheet.Range["J7"].Text = issuerInfo.CompanyName ?? "";
            worksheet.Range["J8"].Text = $"{issuerInfo.Position ?? ""}　{issuerInfo.Name ?? ""}";
            worksheet.Range["J10"].Text = $"TEL {issuerInfo.PhoneNumber ?? ""}";
            worksheet.Range["L10"].Text = $"FAX {issuerInfo.FaxNumber ?? ""}";
        }

        // 請求先情報を設定（2行下にずれる）
        private void PopulateClientInfo(IWorksheet worksheet, AutoDealerSphere.Shared.Models.Client client)
        {
            if (client == null) return;
            
            worksheet.Range["B6"].Text = client.Zip ?? "";
            worksheet.Range["B7"].Text = client.Address ?? "";
            worksheet.Range["B9"].Text = client.Name ?? "";
        }

        // 合計金額を設定（2行下にずれる）
        private void PopulateTotalAmount(IWorksheet worksheet, decimal total)
        {
            worksheet.Range["B12"].Text = $"¥{total:N0}";
        }

        // 振込先口座情報を設定（2行下にずれる）
        private void PopulateBankAccountInfo(IWorksheet worksheet, IssuerInfo issuerInfo)
        {
            if (issuerInfo == null) return;
            
            // 第1銀行口座 (H12)
            if (!string.IsNullOrEmpty(issuerInfo.Bank1Name))
            {
                var bank1Info = $"{issuerInfo.Bank1Name ?? ""} {issuerInfo.Bank1BranchName ?? ""} {issuerInfo.Bank1AccountType ?? ""} {issuerInfo.Bank1AccountNumber ?? ""} {issuerInfo.Bank1AccountHolder ?? ""}".Trim();
                worksheet.Range["H12"].Text = bank1Info;
            }
            
            // 第2銀行口座 (H13)
            if (!string.IsNullOrEmpty(issuerInfo.Bank2Name))
            {
                var bank2Info = $"{issuerInfo.Bank2Name ?? ""} {issuerInfo.Bank2BranchName ?? ""} {issuerInfo.Bank2AccountType ?? ""} {issuerInfo.Bank2AccountNumber ?? ""} {issuerInfo.Bank2AccountHolder ?? ""}".Trim();
                worksheet.Range["H13"].Text = bank2Info;
            }
        }

        // 車両情報を設定（2行下にずれる）
        private void PopulateVehicleInfo(IWorksheet worksheet, Vehicle vehicle, decimal? mileage)
        {
            if (vehicle == null) return;
            
            // 車両番号
            string licensePlate = FormatLicensePlate(vehicle);
            worksheet.Range["B15"].Text = licensePlate;
            
            // 車名
            worksheet.Range["E15"].Text = vehicle.VehicleName ?? "";
            
            // 車体番号
            worksheet.Range["G15"].Text = vehicle.ChassisNumber ?? "";
            
            // 初年度登録
            worksheet.Range["L15"].Text = vehicle.FirstRegistrationDate?.ToString("yyyy/MM/dd") ?? "";
            
            // 車検満了日
            worksheet.Range["B17"].Text = vehicle.InspectionExpiryDate?.ToString("yyyy/MM/dd") ?? "";
            
            // 形式
            worksheet.Range["G17"].Text = vehicle.VehicleModel ?? "";
            
            // 走行距離
            worksheet.Range["L17"].Text = mileage.HasValue ? $"{mileage:N0} km" : "";
        }

        // 請求明細を設定（明細行は2行下に移動、1行減らす）
        private void PopulateInvoiceDetails(IWorksheet worksheet, Invoice invoice)
        {
            int row = 20;  // 18から2行下に移動
            var taxableDetails = invoice.InvoiceDetails
                .Where(d => d.Type != "法定費用")
                .OrderBy(d => d.DisplayOrder);

            foreach (var detail in taxableDetails)
            {
                if (row > 39) break;  // 38から1行下に移動（明細行が1行減る）
                
                worksheet.Range[$"A{row}"].Text = detail.ItemName ?? "";
                worksheet.Range[$"F{row}"].Text = detail.RepairMethod ?? "";
                worksheet.Range[$"H{row}"].Number = (double)detail.UnitPrice;
                worksheet.Range[$"J{row}"].Number = (double)detail.Quantity;
                worksheet.Range[$"K{row}"].Number = (double)(detail.Quantity * detail.UnitPrice);
                worksheet.Range[$"M{row}"].Number = (double)detail.LaborCost;
                row++;
            }
        }

        // 非課税項目を設定（1行下に移動）
        private void PopulateNonTaxableItems(IWorksheet worksheet, Invoice invoice)
        {
            int row = 42;  // 41から1行下に移動
            var nonTaxableItems = invoice.InvoiceDetails
                .Where(d => d.Type == "法定費用")
                .OrderBy(d => d.DisplayOrder);

            foreach (var item in nonTaxableItems)
            {
                if (row > 46) break;  // 45から1行下に移動
                
                worksheet.Range[$"A{row}"].Text = item.ItemName ?? "";
                worksheet.Range[$"F{row}"].Number = (double)item.UnitPrice;
                row++;
            }
        }

        // 合計を設定
        private void PopulateTotals(IWorksheet worksheet, Invoice invoice)
        {
            var taxableDetails = invoice.InvoiceDetails.Where(d => d.Type != "法定費用");
            var partsSubTotal = taxableDetails.Sum(d => d.Quantity * d.UnitPrice);
            var laborSubTotal = taxableDetails.Sum(d => d.LaborCost);
            var nonTaxableTotal = invoice.InvoiceDetails.Where(d => d.Type == "法定費用").Sum(d => d.UnitPrice);
            
            decimal taxableTotal = partsSubTotal + laborSubTotal;
            int tax = (int)(taxableTotal * 0.1m);

            // ページ小計（1行下に移動）
            worksheet.Range["K40"].Number = (double)partsSubTotal;
            worksheet.Range["M40"].Number = (double)laborSubTotal;
            
            // 小計（1行下に移動）
            worksheet.Range["K42"].Number = (double)partsSubTotal;
            worksheet.Range["M42"].Number = (double)laborSubTotal;
            
            // 課税額計（1行下に移動）
            worksheet.Range["K43"].Number = (double)taxableTotal;
            
            // 消費税（1行下に移動）
            worksheet.Range["K44"].Number = tax;
            
            // 非課税額計（1行下に移動）
            worksheet.Range["F47"].Number = (double)nonTaxableTotal;
            worksheet.Range["K45"].Formula = "=F47";
            
            // 合計（1行下に移動）
            worksheet.Range["K46"].Formula = "=K43+K44+K45";
        }

        // 複数請求書の集計合計を設定（最初のシートのみ）
        private void PopulateAggregatedTotals(IWorksheet worksheet, List<Invoice> invoices)
        {
            var allTaxableDetails = invoices.SelectMany(i => i.InvoiceDetails.Where(d => d.Type != "法定費用"));
            var allPartsSubTotal = allTaxableDetails.Sum(d => d.Quantity * d.UnitPrice);
            var allLaborSubTotal = allTaxableDetails.Sum(d => d.LaborCost);
            var allNonTaxableTotal = invoices.SelectMany(i => i.InvoiceDetails.Where(d => d.Type == "法定費用")).Sum(d => d.UnitPrice);
            
            decimal allTaxableTotal = allPartsSubTotal + allLaborSubTotal;
            int allTax = (int)(allTaxableTotal * 0.1m);

            // ページ小計（1行下に移動）
            worksheet.Range["K40"].Number = (double)allPartsSubTotal;
            worksheet.Range["M40"].Number = (double)allLaborSubTotal;
            
            // 小計（1行下に移動）
            worksheet.Range["K42"].Number = (double)allPartsSubTotal;
            worksheet.Range["M42"].Number = (double)allLaborSubTotal;
            
            // 課税額計（1行下に移動）
            worksheet.Range["K43"].Number = (double)allTaxableTotal;
            
            // 消費税（1行下に移動）
            worksheet.Range["K44"].Number = allTax;
            
            // 非課税額計（1行下に移動）
            worksheet.Range["F47"].Number = (double)allNonTaxableTotal;
            worksheet.Range["K45"].Formula = "=F47";
            
            // 合計（1行下に移動）
            worksheet.Range["K46"].Formula = "=K43+K44+K45";
        }
    }
}