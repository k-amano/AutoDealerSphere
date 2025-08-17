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

                // ワークシートの初期設定
                SetupWorksheet(worksheet);

                // ヘッダー部分の設定
                SetupHeader(worksheet, invoice, issuerInfo);

                // 車両情報部分の設定
                SetupVehicleInfo(worksheet, invoice);

                // 明細ヘッダーの設定
                SetupDetailHeader(worksheet);

                // 明細行の入力
                var (partsSubTotal, laborSubTotal) = FillInvoiceDetails(worksheet, invoice);

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

                // 非課税項目の入力
                var nonTaxableTotal = FillNonTaxableItems(worksheet, invoice);

                // 合計計算部分の設定
                SetupTotals(worksheet, partsSubTotal, laborSubTotal, nonTaxableTotal);

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

        // ワークシートの初期設定
        private void SetupWorksheet(IWorksheet worksheet)
        {
            // 用紙サイズをA4に設定
            worksheet.PageSetup.PaperSize = ExcelPaperSize.PaperA4;
            worksheet.PageSetup.Orientation = ExcelPageOrientation.Portrait;
            worksheet.PageSetup.TopMargin = 0.75;
            worksheet.PageSetup.BottomMargin = 0.75;
            worksheet.PageSetup.LeftMargin = 0.7;
            worksheet.PageSetup.RightMargin = 0.7;

            // 列幅設定（配列でまとめて設定）
            var columnWidths = new[] 
            {
                7.88, 8.38, 6.38, 2.63, 8.38, 8.38, 0.77, 8.38,
                3.13, 8.13, 8.38, 3.13, 8.38, 3.13
            };
            
            for (int i = 0; i < columnWidths.Length; i++)
            {
                worksheet.SetColumnWidth(i + 1, columnWidths[i]);
            }

            // 行の高さ設定（配列でまとめて設定）
            var rowHeights = new Dictionary<int, double>
            {
                { 6, 6 }, { 12, 6 }, { 14, 6 }, { 16, 6 }
            };
            
            foreach (var kvp in rowHeights)
            {
                worksheet.SetRowHeight(kvp.Key, kvp.Value);
            }
        }

        // ヘッダー部分の設定
        private void SetupHeader(IWorksheet worksheet, Invoice invoice, IssuerInfo issuerInfo)
        {
            // タイトル（行1）
            MergeAndSetCell(worksheet, "C1:K1", "御　請　求　書", style =>
            {
                style.Font.Size = 20;
                style.Font.Bold = true;
                style.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                style.VerticalAlignment = ExcelVAlign.VAlignCenter;
                style.Color = HeaderColor;
            });
            worksheet.Range["C1"].RowHeight = 35;

            // 作成日（行2）
            var headerTexts = new Dictionary<string, string>
            {
                { "K2", "作成日" },
                { "L2", $"令和{DateTime.Now.Year - 2018}年{DateTime.Now.Month}月{DateTime.Now.Day}日" }
            };
            SetCellTexts(worksheet, headerTexts);
            worksheet.Range["K2"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
            worksheet.Range["L2:M2"].Merge();

            // 発行者情報（行3-8の右側）
            if (issuerInfo != null)
            {
                var issuerTexts = new Dictionary<string, string>
                {
                    { "J3", issuerInfo.PostalCode },
                    { "J4", issuerInfo.Address },
                    { "J5", issuerInfo.CompanyName },
                    { "J6", $"{issuerInfo.Position}　{issuerInfo.Name}" },
                    { "J8", $"TEL {issuerInfo.PhoneNumber}" },
                    { "L8", $"FAX {issuerInfo.FaxNumber}" }
                };
                SetCellTexts(worksheet, issuerTexts);
            }

            // 請求先情報（行4-7の左側）
            var clientTexts = new Dictionary<string, string>
            {
                { "A4", "郵便番号" },
                { "B4", invoice.Client?.Zip ?? "" },
                { "A5", "住所" },
                { "B5", invoice.Client?.Address ?? "" },
                { "A7", "氏名" },
                { "B7", invoice.Client?.Name ?? "" },
                { "F7", "様" }
            };
            SetCellTexts(worksheet, clientTexts);

            // 合計金額（行9）
            worksheet.Range["A9"].Text = "合計金額";
            worksheet.Range["A9"].CellStyle.Font.Bold = true;
            
            // 合計金額セル（B10-E11セル結合）
            MergeAndSetCell(worksheet, "B10:E11", $"¥{invoice.Total:N0}", style =>
            {
                style.Font.Size = 20;
                style.Font.Bold = true;
                style.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                style.VerticalAlignment = ExcelVAlign.VAlignBottom;
            });
            worksheet.Range["B10"].RowHeight = 30;
            worksheet.Range["B11:E11"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Double;

            // 振込案内（行9の右側）
            MergeAndSetCell(worksheet, "H9:N9", "銀行振込の場合は、下記口座までお振込みください。", style =>
            {
                style.Color = HeaderColor;
                style.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                style.Font.Bold = true;
            });
            SetRangeBorders(worksheet, "H9:N9", ExcelLineStyle.Hair);

            // 口座情報（行10-11）
            if (issuerInfo != null && !string.IsNullOrEmpty(issuerInfo.Bank1Name))
            {
                MergeAndSetCell(worksheet, "H10:N10", 
                    $"{issuerInfo.Bank1Name} {issuerInfo.Bank1BranchName} {issuerInfo.Bank1AccountType} {issuerInfo.Bank1AccountNumber} {issuerInfo.Bank1AccountHolder}", 
                    style =>
                    {
                        style.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                        style.Color = DataColor;
                    });
                SetRangeBorders(worksheet, "H10:N10", ExcelLineStyle.Hair);
            }
            
            if (issuerInfo != null && !string.IsNullOrEmpty(issuerInfo.Bank2Name))
            {
                MergeAndSetCell(worksheet, "H11:N11", 
                    $"{issuerInfo.Bank2Name} {issuerInfo.Bank2BranchName} {issuerInfo.Bank2AccountType} {issuerInfo.Bank2AccountNumber} {issuerInfo.Bank2AccountHolder}", 
                    style =>
                    {
                        style.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                        style.Color = DataColor;
                    });
                SetRangeBorders(worksheet, "H11:N11", ExcelLineStyle.Hair);
            }
        }

        // 車両情報部分の設定
        private void SetupVehicleInfo(IWorksheet worksheet, Invoice invoice)
        {
            // 車両情報ヘッダー（行13）- Dictionaryでまとめて設定
            var vehicleHeaders = new Dictionary<string, string>
            {
                { "A13", "車両番号" },
                { "D13", "車名" },
                { "F13", "車体番号" },
                { "K13", "初年度登録" }
            };

            foreach (var kvp in vehicleHeaders)
            {
                worksheet.Range[kvp.Key].Text = kvp.Value;
                worksheet.Range[kvp.Key].CellStyle.Color = HeaderColor;
                worksheet.Range[kvp.Key].CellStyle.Font.Bold = true;
                worksheet.Range[kvp.Key].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
            }

            // 車両番号データ
            string licensePlate = "";
            if (invoice.Vehicle != null)
            {
                licensePlate = $"{invoice.Vehicle.LicensePlateLocation ?? ""} {invoice.Vehicle.LicensePlateClassification ?? ""} {invoice.Vehicle.LicensePlateHiragana ?? ""} {invoice.Vehicle.LicensePlateNumber ?? ""}".Trim();
            }
            
            MergeAndSetCell(worksheet, "B13:C13", licensePlate, style => style.Color = DataColor);
            worksheet.Range["E13"].Text = invoice.Vehicle?.VehicleName ?? "";
            worksheet.Range["E13"].CellStyle.Color = DataColor;
            MergeAndSetCell(worksheet, "G13:J13", invoice.Vehicle?.ChassisNumber ?? "", style => style.Color = DataColor);
            MergeAndSetCell(worksheet, "L13:N13", invoice.Vehicle?.FirstRegistrationDate?.ToString("yyyy/MM/dd") ?? "", style => style.Color = DataColor);

            // 車検満了日・形式・走行距離（行15）
            var vehicleDetailHeaders = new Dictionary<string, string>
            {
                { "A15", "車検満了日" },
                { "F15", "形式" },
                { "K15", "走行距離" }
            };

            foreach (var kvp in vehicleDetailHeaders)
            {
                worksheet.Range[kvp.Key].Text = kvp.Value;
                worksheet.Range[kvp.Key].CellStyle.Color = HeaderColor;
                worksheet.Range[kvp.Key].CellStyle.Font.Bold = true;
                worksheet.Range[kvp.Key].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
            }

            MergeAndSetCell(worksheet, "B15:E15", invoice.Vehicle?.InspectionExpiryDate?.ToString("yyyy/MM/dd") ?? "", style => style.Color = DataColor);
            MergeAndSetCell(worksheet, "G15:J15", invoice.Vehicle?.VehicleModel ?? "", style => style.Color = DataColor);
            MergeAndSetCell(worksheet, "L15:N15", invoice.Mileage.HasValue ? $"{invoice.Mileage:N0} km" : "", style =>
            {
                style.HorizontalAlignment = ExcelHAlign.HAlignRight;
                style.Color = DataColor;
            });
        }

        // 明細ヘッダーの設定
        private void SetupDetailHeader(IWorksheet worksheet)
        {
            // 明細ヘッダー定義（配列でまとめて管理）
            var detailHeaders = new[]
            {
                new { Range = "A17:E17", Text = "部品名称／項目" },
                new { Range = "F17:G17", Text = "修理方法" },
                new { Range = "H17:I17", Text = "部品単価" },
                new { Range = "J17", Text = "個数" },
                new { Range = "K17:L17", Text = "部品価格" },
                new { Range = "M17:N17", Text = "工賃" }
            };

            foreach (var header in detailHeaders)
            {
                if (header.Range.Contains(":"))
                {
                    MergeAndSetCell(worksheet, header.Range, header.Text, style =>
                    {
                        style.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                    });
                }
                else
                {
                    worksheet.Range[header.Range].Text = header.Text;
                    worksheet.Range[header.Range].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                }
                SetRangeBorders(worksheet, header.Range, ExcelLineStyle.Hair);
            }
        }

        // 明細行の入力
        private (decimal partsSubTotal, decimal laborSubTotal) FillInvoiceDetails(IWorksheet worksheet, Invoice invoice)
        {
            int row = 18;
            var taxableDetails = invoice.InvoiceDetails
                .Where(d => d.Type != "法定費用")
                .OrderBy(d => d.DisplayOrder);
            
            decimal partsSubTotal = 0;
            decimal laborSubTotal = 0;

            foreach (var detail in taxableDetails)
            {
                // 各項目を入力
                MergeAndSetCell(worksheet, $"A{row}:E{row}", detail.ItemName, style =>
                {
                    style.HorizontalAlignment = ExcelHAlign.HAlignLeft;
                });
                SetRangeBorders(worksheet, $"A{row}:E{row}", ExcelLineStyle.Hair);

                MergeAndSetCell(worksheet, $"F{row}:G{row}", detail.RepairMethod ?? "", style =>
                {
                    style.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                });
                SetRangeBorders(worksheet, $"F{row}:G{row}", ExcelLineStyle.Hair);

                // 数値データを設定
                worksheet.Range[$"H{row}:I{row}"].Merge();
                worksheet.Range[$"H{row}"].Number = (double)detail.UnitPrice;
                worksheet.Range[$"H{row}"].NumberFormat = "#,##0";
                worksheet.Range[$"H{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                SetRangeBorders(worksheet, $"H{row}:I{row}", ExcelLineStyle.Hair);

                worksheet.Range[$"J{row}"].Number = (double)detail.Quantity;
                worksheet.Range[$"J{row}"].NumberFormat = "#,##0.0";
                worksheet.Range[$"J{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                SetRangeBorders(worksheet, $"J{row}", ExcelLineStyle.Hair);

                worksheet.Range[$"K{row}:L{row}"].Merge();
                worksheet.Range[$"K{row}"].Number = (double)(detail.Quantity * detail.UnitPrice);
                worksheet.Range[$"K{row}"].NumberFormat = "#,##0";
                worksheet.Range[$"K{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                SetRangeBorders(worksheet, $"K{row}:L{row}", ExcelLineStyle.Hair);

                worksheet.Range[$"M{row}:N{row}"].Merge();
                worksheet.Range[$"M{row}"].Number = (double)detail.LaborCost;
                worksheet.Range[$"M{row}"].NumberFormat = "#,##0";
                worksheet.Range[$"M{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                SetRangeBorders(worksheet, $"M{row}:N{row}", ExcelLineStyle.Hair);

                partsSubTotal += detail.Quantity * detail.UnitPrice;
                laborSubTotal += detail.LaborCost;

                row++;
            }

            // 空行を行38まで埋める
            FillEmptyDetailRows(worksheet, row, 38);

            // ページ小計（行39）
            SetupPageSubtotal(worksheet, partsSubTotal, laborSubTotal);

            return (partsSubTotal, laborSubTotal);
        }

        // 空の明細行を埋める
        private void FillEmptyDetailRows(IWorksheet worksheet, int startRow, int endRow)
        {
            var emptyRowRanges = new[]
            {
                "A{0}:E{0}", "F{0}:G{0}", "H{0}:I{0}", "J{0}", "K{0}:L{0}", "M{0}:N{0}"
            };

            for (int row = startRow; row <= endRow; row++)
            {
                foreach (var rangeTemplate in emptyRowRanges)
                {
                    var range = string.Format(rangeTemplate, row);
                    if (range.Contains(":"))
                    {
                        worksheet.Range[range].Merge();
                    }
                    SetRangeBorders(worksheet, range, ExcelLineStyle.Hair);
                }
            }
        }

        // ページ小計の設定
        private void SetupPageSubtotal(IWorksheet worksheet, decimal partsSubTotal, decimal laborSubTotal)
        {
            MergeAndSetCell(worksheet, "H39:J39", "ページ小計", style =>
            {
                style.HorizontalAlignment = ExcelHAlign.HAlignCenter;
            });
            SetRangeBorders(worksheet, "H39:J39", ExcelLineStyle.Hair);

            worksheet.Range["K39:L39"].Merge();
            worksheet.Range["K39"].Number = (double)partsSubTotal;
            worksheet.Range["K39"].NumberFormat = "#,##0";
            worksheet.Range["K39"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
            SetRangeBorders(worksheet, "K39:L39", ExcelLineStyle.Hair);

            worksheet.Range["M39:N39"].Merge();
            worksheet.Range["M39"].Number = (double)laborSubTotal;
            worksheet.Range["M39"].NumberFormat = "#,##0";
            worksheet.Range["M39"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
            SetRangeBorders(worksheet, "M39:N39", ExcelLineStyle.Hair);

            // 非課税項目見出し（行40）
            MergeAndSetCell(worksheet, "A40:F40", "非課税項目", style =>
            {
                style.Font.Bold = true;
                style.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                style.Color = HeaderColor;
            });
            SetRangeBorders(worksheet, "A40:F40", ExcelLineStyle.Hair);

            // 小計（行41）
            MergeAndSetCell(worksheet, "H41:J41", "小計");
            SetRangeBorders(worksheet, "H41:J41", ExcelLineStyle.Hair);

            worksheet.Range["K41:L41"].Merge();
            worksheet.Range["K41"].Number = (double)partsSubTotal;
            worksheet.Range["K41"].NumberFormat = "#,##0";
            worksheet.Range["K41"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
            SetRangeBorders(worksheet, "K41:L41", ExcelLineStyle.Hair);

            worksheet.Range["M41:N41"].Merge();
            worksheet.Range["M41"].Number = (double)laborSubTotal;
            worksheet.Range["M41"].NumberFormat = "#,##0";
            worksheet.Range["M41"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
            SetRangeBorders(worksheet, "M41:N41", ExcelLineStyle.Hair);
        }

        // 非課税項目の入力
        private decimal FillNonTaxableItems(IWorksheet worksheet, Invoice invoice)
        {
            int row = 41;
            var nonTaxableItems = invoice.InvoiceDetails
                .Where(d => d.Type == "法定費用")
                .OrderBy(d => d.DisplayOrder);
            
            decimal nonTaxableTotal = 0;

            foreach (var item in nonTaxableItems)
            {
                if (row > 45) break;
                
                MergeAndSetCell(worksheet, $"A{row}:E{row}", item.ItemName);
                SetRangeBorders(worksheet, $"A{row}:E{row}", ExcelLineStyle.Hair);
                
                worksheet.Range[$"F{row}"].Number = (double)item.UnitPrice;
                worksheet.Range[$"F{row}"].NumberFormat = "#,##0";
                worksheet.Range[$"F{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                SetRangeBorders(worksheet, $"F{row}", ExcelLineStyle.Hair);
                
                nonTaxableTotal += item.UnitPrice;
                row++;
            }
            
            // 残りの行を空白で埋める
            for (int i = row; i <= 45; i++)
            {
                worksheet.Range[$"A{i}:E{i}"].Merge();
                SetRangeBorders(worksheet, $"A{i}:E{i}", ExcelLineStyle.Hair);
                SetRangeBorders(worksheet, $"F{i}", ExcelLineStyle.Hair);
            }

            return nonTaxableTotal;
        }

        // 合計計算部分の設定
        private void SetupTotals(IWorksheet worksheet, decimal partsSubTotal, decimal laborSubTotal, decimal nonTaxableTotal)
        {
            decimal taxableTotal = partsSubTotal + laborSubTotal;
            int tax = (int)(taxableTotal * 0.1m);

            // 合計項目の定義（Dictionaryでまとめて管理）
            var totalItems = new[]
            {
                new { Row = 42, Label = "課税額計", Value = taxableTotal, HasColor = false, Formula = (string)null },
                new { Row = 43, Label = "消費税 10%", Value = (decimal)tax, HasColor = false, Formula = (string)null },
                new { Row = 44, Label = "非課税額計", Value = 0m, HasColor = true, Formula = "=F46" },
                new { Row = 45, Label = "合計", Value = 0m, HasColor = false, Formula = "=K42+K43+K44" }
            };

            foreach (var item in totalItems)
            {
                // ラベル部分
                MergeAndSetCell(worksheet, $"H{item.Row}:J{item.Row}", item.Label, style =>
                {
                    if (item.HasColor)
                    {
                        style.Color = HeaderColor;
                    }
                });
                SetRangeBorders(worksheet, $"H{item.Row}:J{item.Row}", ExcelLineStyle.Hair);

                // 値部分
                worksheet.Range[$"K{item.Row}:N{item.Row}"].Merge();
                if (item.Formula != null)
                {
                    worksheet.Range[$"K{item.Row}"].Formula = item.Formula;
                }
                else
                {
                    worksheet.Range[$"K{item.Row}"].Number = (double)item.Value;
                }
                worksheet.Range[$"K{item.Row}"].NumberFormat = "#,##0";
                worksheet.Range[$"K{item.Row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                if (item.Row == 44)
                {
                    worksheet.Range[$"K{item.Row}"].CellStyle.Color = DataColor;
                }
                SetRangeBorders(worksheet, $"K{item.Row}:N{item.Row}", ExcelLineStyle.Hair);
            }

            // 非課税額計（行46）
            MergeAndSetCell(worksheet, "A46:E46", "非課税額計", style =>
            {
                style.Color = HeaderColor;
            });
            SetRangeBorders(worksheet, "A46:E46", ExcelLineStyle.Hair);
            
            worksheet.Range["F46"].Number = (double)nonTaxableTotal;
            worksheet.Range["F46"].NumberFormat = "#,##0";
            worksheet.Range["F46"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
            worksheet.Range["F46"].CellStyle.Color = DataColor;
            SetRangeBorders(worksheet, "F46", ExcelLineStyle.Hair);
        }
    }
}