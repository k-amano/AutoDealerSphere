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

                // 行39の設定をまとめて実行
                SetupRow39(worksheet, partsSubTotal, laborSubTotal);
                
                // 行40-41の設定をまとめて実行
                SetupRows40And41(worksheet, partsSubTotal, laborSubTotal);

                // 非課税項目の入力
                var nonTaxableTotal = FillNonTaxableItems(worksheet, invoice);

                // 合計計算部分の設定
                SetupTotals(worksheet, partsSubTotal, laborSubTotal, nonTaxableTotal);

                // 全体のフォントを設定（個別設定を保持）
                ApplyDefaultFont(worksheet);

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
            worksheet.PageSetup.TopMargin = 0.4;
            worksheet.PageSetup.BottomMargin = 0.4;
            worksheet.PageSetup.LeftMargin = 0.3;
            worksheet.PageSetup.RightMargin = 0.3;

            // 印刷範囲の設定
            worksheet.PageSetup.PrintArea = "A1:N46";

            // 列幅設定（ピクセル単位）
            var columnWidthsInPixels = new[] 
            {
                108, 115, 87, 36, 115, 115, 11, 115,
                43, 111, 115, 43, 115, 43, 115, 125, 115, 115
            };
            
            // ピクセルから文字数への変換
            // 実測値に基づく調整: 現在の列幅が約2倍になっているため、変換係数を調整
            for (int i = 0; i < columnWidthsInPixels.Length; i++)
            {
                // ピクセル値を文字数に変換（調整済み）
                double charWidth = columnWidthsInPixels[i] / 13.5;
                worksheet.SetColumnWidth(i + 1, charWidth);
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
            SetupTitle(worksheet);
            SetupCreationDate(worksheet);
            SetupIssuerInfo(worksheet, issuerInfo);
            SetupClientInfo(worksheet, invoice.Client);
            SetupTotalAmount(worksheet, invoice.Total);
            SetupBankInfo(worksheet, issuerInfo);
        }

        // タイトル設定
        private void SetupTitle(IWorksheet worksheet)
        {
            MergeAndSetCell(worksheet, "C1:K1", "御　請　求　書", style =>
            {
                style.Font.Size = 16;
                style.Font.Bold = true;
                style.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                style.VerticalAlignment = ExcelVAlign.VAlignCenter;
                style.Color = HeaderColor;
            });
            worksheet.Range["C1"].RowHeight = 35;
        }

        // 作成日設定
        private void SetupCreationDate(IWorksheet worksheet)
        {
            var headerTexts = new Dictionary<string, string>
            {
                { "K2", "作成日" },
                { "L2", $"令和{DateTime.Now.Year - 2018}年{DateTime.Now.Month}月{DateTime.Now.Day}日" }
            };
            SetCellTexts(worksheet, headerTexts);
            worksheet.Range["K2"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
            worksheet.Range["K2"].CellStyle.Font.Size = 8;
            worksheet.Range["K2"].CellStyle.Font.Bold = true;
            worksheet.Range["L2:M2"].Merge();
            worksheet.Range["L2"].CellStyle.Font.Size = 8;
            worksheet.Range["L2"].CellStyle.Font.Bold = true;
            worksheet.Range["M2"].CellStyle.Font.Size = 8;
            worksheet.Range["M2"].CellStyle.Font.Bold = true;
            worksheet.Range["N2"].CellStyle.Font.Size = 8;
            worksheet.Range["N2"].CellStyle.Font.Bold = true;
        }

        // 発行者情報設定
        private void SetupIssuerInfo(IWorksheet worksheet, IssuerInfo issuerInfo)
        {
            if (issuerInfo == null) return;
            
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
            
            // フォントサイズを設定
            worksheet.Range["J3"].CellStyle.Font.Size = 11;
            worksheet.Range["J3"].CellStyle.Font.Bold = true;
            worksheet.Range["J4"].CellStyle.Font.Size = 11;
            worksheet.Range["J4"].CellStyle.Font.Bold = true;
            worksheet.Range["J5"].CellStyle.Font.Size = 11;
            worksheet.Range["J5"].CellStyle.Font.Bold = true;
            
            // J6:K7をマージして11ptに設定
            worksheet.Range["J6:K7"].Merge();
            worksheet.Range["J6"].CellStyle.Font.Size = 11;
            worksheet.Range["J6"].CellStyle.Font.Bold = true;
            
            worksheet.Range["J8"].CellStyle.Font.Size = 9;
            worksheet.Range["J8"].CellStyle.Font.Bold = true;
            worksheet.Range["L8"].CellStyle.Font.Size = 9;
            worksheet.Range["L8"].CellStyle.Font.Bold = true;
        }

        // 請求先情報設定
        private void SetupClientInfo(IWorksheet worksheet, AutoDealerSphere.Shared.Models.Client client)
        {
            var clientTexts = new Dictionary<string, string>
            {
                { "A4", "郵便番号" },
                { "B4", client?.Zip ?? "" },
                { "A5", "住所" },
                { "B5", client?.Address ?? "" },
                { "A7", "氏名" },
                { "B7", client?.Name ?? "" },
                { "F7", "様" }
            };
            SetCellTexts(worksheet, clientTexts);
            
            // フォントサイズを設定
            worksheet.Range["A4"].CellStyle.Font.Size = 8;
            worksheet.Range["A4"].CellStyle.Font.Bold = true;
            worksheet.Range["B4"].CellStyle.Font.Size = 9;
            worksheet.Range["B4"].CellStyle.Font.Bold = true;
            worksheet.Range["A5"].CellStyle.Font.Size = 8;
            worksheet.Range["A5"].CellStyle.Font.Bold = true;
            worksheet.Range["B5"].CellStyle.Font.Size = 11;
            worksheet.Range["B5"].CellStyle.Font.Bold = true;
            worksheet.Range["A7"].CellStyle.Font.Size = 8;
            worksheet.Range["A7"].CellStyle.Font.Bold = true;
            worksheet.Range["B7"].CellStyle.Font.Size = 11;
            worksheet.Range["B7"].CellStyle.Font.Bold = true;
            worksheet.Range["F7"].CellStyle.Font.Bold = true;
        }

        // 合計金額設定
        private void SetupTotalAmount(IWorksheet worksheet, decimal total)
        {
            worksheet.Range["A9"].Text = "合計金額";
            worksheet.Range["A9"].CellStyle.Font.Bold = true;
            worksheet.Range["A9"].CellStyle.Font.Size = 12;
            
            MergeAndSetCell(worksheet, "B10:E11", $"¥{total:N0}", style =>
            {
                style.Font.Size = 16;
                style.Font.Bold = true;
                style.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                style.VerticalAlignment = ExcelVAlign.VAlignBottom;
            });
            worksheet.Range["B11:E11"].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Double;
        }

        // 銀行振込情報設定
        private void SetupBankInfo(IWorksheet worksheet, IssuerInfo issuerInfo)
        {
            MergeAndSetCell(worksheet, "H9:N9", "銀行振込の場合は、下記口座までお振込みください。", style =>
            {
                style.Color = HeaderColor;
                style.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                style.Font.Bold = true;
                style.Font.Size = 8;
            });
            SetRangeBorders(worksheet, "H9:N9", ExcelLineStyle.Hair);

            if (issuerInfo != null && !string.IsNullOrEmpty(issuerInfo.Bank1Name))
            {
                SetBankRow(worksheet, "H10:N10", issuerInfo.Bank1Name, issuerInfo.Bank1BranchName,
                    issuerInfo.Bank1AccountType, issuerInfo.Bank1AccountNumber, issuerInfo.Bank1AccountHolder);
            }
            
            if (issuerInfo != null && !string.IsNullOrEmpty(issuerInfo.Bank2Name))
            {
                SetBankRow(worksheet, "H11:N11", issuerInfo.Bank2Name, issuerInfo.Bank2BranchName,
                    issuerInfo.Bank2AccountType, issuerInfo.Bank2AccountNumber, issuerInfo.Bank2AccountHolder);
            }
        }

        // 銀行口座行設定
        private void SetBankRow(IWorksheet worksheet, string range, string bankName, string branchName,
            string accountType, string accountNumber, string accountHolder)
        {
            MergeAndSetCell(worksheet, range,
                $"{bankName} {branchName} {accountType} {accountNumber} {accountHolder}",
                style =>
                {
                    style.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                    style.Color = DataColor;
                    style.Font.Size = 8;
                    style.Font.Bold = true;
                });
            SetRangeBorders(worksheet, range, ExcelLineStyle.Hair);
        }

        // 車両情報部分の設定
        private void SetupVehicleInfo(IWorksheet worksheet, Invoice invoice)
        {
            SetupVehicleHeaders(worksheet);
            SetupVehicleData(worksheet, invoice.Vehicle, invoice.Mileage);
        }

        // 車両情報ヘッダー設定
        private void SetupVehicleHeaders(IWorksheet worksheet)
        {
            // 行13のヘッダー
            var vehicleHeaders = new Dictionary<string, string>
            {
                { "A13", "車両番号" },
                { "D13", "車名" },
                { "F13", "車体番号" },
                { "K13", "初年度登録" }
            };
            SetHeaderCells(worksheet, vehicleHeaders);

            // 行15のヘッダー
            var detailHeaders = new Dictionary<string, string>
            {
                { "A15", "車検満了日" },
                { "F15", "形式" },
                { "K15", "走行距離" }
            };
            SetHeaderCells(worksheet, detailHeaders);
        }

        // ヘッダーセルを設定
        private void SetHeaderCells(IWorksheet worksheet, Dictionary<string, string> headers)
        {
            foreach (var kvp in headers)
            {
                worksheet.Range[kvp.Key].Text = kvp.Value;
                worksheet.Range[kvp.Key].CellStyle.Color = HeaderColor;
                worksheet.Range[kvp.Key].CellStyle.Font.Bold = true;
                worksheet.Range[kvp.Key].CellStyle.Font.Size = 8;
                worksheet.Range[kvp.Key].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
            }
        }

        // 車両データ設定
        private void SetupVehicleData(IWorksheet worksheet, Vehicle vehicle, decimal? mileage)
        {
            // 行13のデータ
            string licensePlate = FormatLicensePlate(vehicle);
            MergeAndSetCell(worksheet, "B13:C13", licensePlate, style => style.Color = DataColor);
            worksheet.Range["E13"].Text = vehicle?.VehicleName ?? "";
            worksheet.Range["E13"].CellStyle.Color = DataColor;
            MergeAndSetCell(worksheet, "G13:J13", vehicle?.ChassisNumber ?? "", style => style.Color = DataColor);
            MergeAndSetCell(worksheet, "L13:N13", vehicle?.FirstRegistrationDate?.ToString("yyyy/MM/dd") ?? "", 
                style => style.Color = DataColor);

            // 行15のデータ
            MergeAndSetCell(worksheet, "B15:E15", vehicle?.InspectionExpiryDate?.ToString("yyyy/MM/dd") ?? "", 
                style => style.Color = DataColor);
            MergeAndSetCell(worksheet, "G15:J15", vehicle?.VehicleModel ?? "", style => style.Color = DataColor);
            MergeAndSetCell(worksheet, "L15:N15", mileage.HasValue ? $"{mileage:N0} km" : "", style =>
            {
                style.HorizontalAlignment = ExcelHAlign.HAlignRight;
                style.Color = DataColor;
            });
        }

        // ライセンスプレートをフォーマット
        private string FormatLicensePlate(Vehicle vehicle)
        {
            if (vehicle == null) return "";
            return $"{vehicle.LicensePlateLocation ?? ""} {vehicle.LicensePlateClassification ?? ""} {vehicle.LicensePlateHiragana ?? ""} {vehicle.LicensePlateNumber ?? ""}".Trim();
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
                FillDetailRow(worksheet, row, detail);
                partsSubTotal += detail.Quantity * detail.UnitPrice;
                laborSubTotal += detail.LaborCost;
                row++;
            }

            FillEmptyDetailRows(worksheet, row, 38);
            SetupPageSubtotal(worksheet, partsSubTotal, laborSubTotal);
            return (partsSubTotal, laborSubTotal);
        }

        // 明細行を入力する
        private void FillDetailRow(IWorksheet worksheet, int row, InvoiceDetail detail)
        {
            // テキスト項目
            MergeAndSetCell(worksheet, $"A{row}:E{row}", detail.ItemName, 
                style => style.HorizontalAlignment = ExcelHAlign.HAlignLeft);
            SetRangeBorders(worksheet, $"A{row}:E{row}", ExcelLineStyle.Hair);

            MergeAndSetCell(worksheet, $"F{row}:G{row}", detail.RepairMethod ?? "", 
                style => style.HorizontalAlignment = ExcelHAlign.HAlignCenter);
            SetRangeBorders(worksheet, $"F{row}:G{row}", ExcelLineStyle.Hair);

            // 数値項目を一括設定
            SetMergedNumberCells(worksheet,
                new MergedNumberCell { 
                    Range = $"H{row}:I{row}", 
                    Value = (double)detail.UnitPrice, 
                    BorderStyle = ExcelLineStyle.Hair 
                },
                new MergedNumberCell { 
                    Range = $"K{row}:L{row}", 
                    Value = (double)(detail.Quantity * detail.UnitPrice), 
                    BorderStyle = ExcelLineStyle.Hair 
                },
                new MergedNumberCell { 
                    Range = $"M{row}:N{row}", 
                    Value = (double)detail.LaborCost, 
                    BorderStyle = ExcelLineStyle.Hair 
                });

            // 個数（マージなし）
            worksheet.Range[$"J{row}"].Number = (double)detail.Quantity;
            worksheet.Range[$"J{row}"].NumberFormat = "#,##0.0";
            worksheet.Range[$"J{row}"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
            SetRangeBorders(worksheet, $"J{row}", ExcelLineStyle.Hair);
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
            // 行39の設定
            MergeAndSetCell(worksheet, "H39:J39", "ページ小計", style =>
            {
                style.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                style.Font.Size = 11;
            });
            SetRangeBorders(worksheet, "H39:J39", ExcelLineStyle.Hair);
            
            SetMergedNumberCells(worksheet,
                new MergedNumberCell { Range = "K39:L39", Value = (double)partsSubTotal, BorderStyle = ExcelLineStyle.Hair },
                new MergedNumberCell { Range = "M39:N39", Value = (double)laborSubTotal, BorderStyle = ExcelLineStyle.Hair });

            // 行40の非課税項目見出し
            MergeAndSetCell(worksheet, "A40:F40", "非課税項目", style =>
            {
                style.Font.Bold = true;
                style.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                style.Color = HeaderColor;
            });
            SetRangeBorders(worksheet, "A40:F40", ExcelLineStyle.Hair);

            // 行41の小計
            MergeAndSetCell(worksheet, "H41:J41", "小計");
            SetRangeBorders(worksheet, "H41:J41", ExcelLineStyle.Hair);
            
            SetMergedNumberCells(worksheet,
                new MergedNumberCell { Range = "K41:L41", Value = (double)partsSubTotal, BorderStyle = ExcelLineStyle.Hair },
                new MergedNumberCell { Range = "M41:N41", Value = (double)laborSubTotal, BorderStyle = ExcelLineStyle.Hair });
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

            SetupTotalRows(worksheet, taxableTotal, tax);
            SetupNonTaxableTotal(worksheet, nonTaxableTotal);
        }

        // 合計行の設定
        private void SetupTotalRows(IWorksheet worksheet, decimal taxableTotal, int tax)
        {
            var totalItems = new[]
            {
                new TotalItem { Row = 42, Label = "課税額計", Value = taxableTotal },
                new TotalItem { Row = 43, Label = "消費税 10%", Value = tax },
                new TotalItem { Row = 44, Label = "非課税額計", HasColor = true, Formula = "=F46" },
                new TotalItem { Row = 45, Label = "合計", Formula = "=K42+K43+K44" }
            };

            foreach (var item in totalItems)
            {
                SetupTotalRow(worksheet, item);
            }
        }

        // 合計行を設定
        private void SetupTotalRow(IWorksheet worksheet, TotalItem item)
        {
            // ラベル部分
            MergeAndSetCell(worksheet, $"H{item.Row}:J{item.Row}", item.Label, style =>
            {
                if (item.HasColor) style.Color = HeaderColor;
            });
            SetRangeBorders(worksheet, $"H{item.Row}:J{item.Row}", ExcelLineStyle.Hair);

            // 値部分
            var range = $"K{item.Row}:N{item.Row}";
            worksheet.Range[range].Merge();
            
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
            if (item.Row == 44) worksheet.Range[$"K{item.Row}"].CellStyle.Color = DataColor;
            SetRangeBorders(worksheet, range, ExcelLineStyle.Hair);
        }

        // 非課税額計の設定
        private void SetupNonTaxableTotal(IWorksheet worksheet, decimal nonTaxableTotal)
        {
            MergeAndSetCell(worksheet, "A46:E46", "非課税額計", style => style.Color = HeaderColor);
            SetRangeBorders(worksheet, "A46:E46", ExcelLineStyle.Hair);
            
            worksheet.Range["F46"].Number = (double)nonTaxableTotal;
            worksheet.Range["F46"].NumberFormat = "#,##0";
            worksheet.Range["F46"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
            worksheet.Range["F46"].CellStyle.Color = DataColor;
            SetRangeBorders(worksheet, "F46", ExcelLineStyle.Hair);
        }

        // 合計項目の定義
        private class TotalItem
        {
            public int Row { get; set; }
            public string Label { get; set; }
            public decimal Value { get; set; }
            public bool HasColor { get; set; }
            public string Formula { get; set; }
        }

        // 行39の設定
        private void SetupRow39(IWorksheet worksheet, decimal partsSubTotal, decimal laborSubTotal)
        {
            MergeAndSetCell(worksheet, "H39:J39", "ページ小計", 
                style => 
                {
                    style.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                    style.Font.Size = 11;
                });
            SetRangeBorders(worksheet, "H39:J39", ExcelLineStyle.Hair);
            
            SetMergedNumberCells(worksheet,
                new MergedNumberCell { Range = "K39:L39", Value = (double)partsSubTotal, BorderStyle = ExcelLineStyle.Hair },
                new MergedNumberCell { Range = "M39:N39", Value = (double)laborSubTotal, BorderStyle = ExcelLineStyle.Hair });
        }

        // 行40-41の設定
        private void SetupRows40And41(IWorksheet worksheet, decimal partsSubTotal, decimal laborSubTotal)
        {
            // 行40: 非課税項目
            MergeAndSetCell(worksheet, "A40:F40", "非課税項目", style =>
            {
                style.Font.Bold = true;
                style.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                style.Color = HeaderColor;
            });
            SetRangeBorders(worksheet, "A40:F40", ExcelLineStyle.Hair);

            // 行41: 小計
            MergeAndSetCell(worksheet, "H41:J41", "小計");
            SetRangeBorders(worksheet, "H41:J41", ExcelLineStyle.Hair);
            
            SetMergedNumberCells(worksheet,
                new MergedNumberCell { Range = "K41:L41", Value = (double)partsSubTotal, BorderStyle = ExcelLineStyle.Hair },
                new MergedNumberCell { Range = "M41:N41", Value = (double)laborSubTotal, BorderStyle = ExcelLineStyle.Hair });
        }
        
        // デフォルトフォントを適用（個別設定は保持）
        private void ApplyDefaultFont(IWorksheet worksheet)
        {
            if (worksheet.UsedRange != null)
            {
                // 使用範囲のすべてのセルに対してフォントを設定
                var usedRange = worksheet.UsedRange;
                for (int row = usedRange.Row; row <= usedRange.LastRow; row++)
                {
                    for (int col = usedRange.Column; col <= usedRange.LastColumn; col++)
                    {
                        var cell = worksheet.Range[row, col];
                        // フォント名を游ゴシックに設定
                        cell.CellStyle.Font.FontName = "游ゴシック";
                        
                        // フォントサイズが設定されていない場合のみ9ptに設定
                        if (cell.CellStyle.Font.Size == 11) // Excelのデフォルトサイズ
                        {
                            cell.CellStyle.Font.Size = 9;
                        }
                        
                        // すべてのセルをBoldにする
                        cell.CellStyle.Font.Bold = true;
                    }
                }
            }
        }
    }
}