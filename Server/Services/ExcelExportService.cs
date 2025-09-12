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

        // セル位置定数 - 原本用
        private const string INVOICE_NUMBER_CELL = "L2";
        private const string INVOICE_DATE_CELL = "L3";
        private const string INVOICE_REGISTRATION_NUMBER_CELL = "L4";
        
        private const string ISSUER_POSTAL_CODE_CELL = "J5";
        private const string ISSUER_ADDRESS_CELL = "J6";
        private const string ISSUER_COMPANY_NAME_CELL = "J7";
        private const string ISSUER_POSITION_NAME_CELL = "J8";
        private const string ISSUER_PHONE_CELL = "J10";
        private const string ISSUER_FAX_CELL = "L10";
        
        private const string CLIENT_ZIP_CELL = "B6";
        private const string CLIENT_ADDRESS_CELL = "B7";
        private const string CLIENT_NAME_CELL = "B9";
        
        private const string TOTAL_AMOUNT_CELL = "B12";
        
        private const string BANK1_INFO_CELL = "H12";
        private const string BANK2_INFO_CELL = "H13";
        
        private const string VEHICLE_LICENSE_PLATE_CELL = "B15";
        private const string VEHICLE_NAME_CELL = "E15";
        private const string VEHICLE_CHASSIS_NUMBER_CELL = "G15";
        private const string VEHICLE_FIRST_REGISTRATION_CELL = "L15";
        private const string VEHICLE_INSPECTION_EXPIRY_CELL = "B17";
        private const string VEHICLE_MODEL_CELL = "G17";
        private const string VEHICLE_MILEAGE_CELL = "L17";
        
        private const int INVOICE_DETAILS_START_ROW = 20;
        private const int INVOICE_DETAILS_END_ROW = 39;
        
        private const int NON_TAXABLE_START_ROW = 42;
        private const int NON_TAXABLE_END_ROW = 46;
        
        private const string PARTS_SUBTOTAL_CELL = "K40";
        private const string LABOR_SUBTOTAL_CELL = "M40";
        private const string PARTS_SUBTOTAL2_CELL = "K42";
        private const string LABOR_SUBTOTAL2_CELL = "M42";
        private const string TAXABLE_TOTAL_CELL = "K43";
        private const string TAX_CELL = "K44";
        private const string NON_TAXABLE_TOTAL_CELL = "F47";
        private const string NON_TAXABLE_TOTAL_REF_CELL = "K45";
        private const string GRAND_TOTAL_CELL = "K46";
        
        // 控え用の行オフセット
        private const int COPY_ROW_OFFSET = 47;

        private readonly IInvoiceService _invoiceService;
        private readonly IIssuerInfoService _issuerInfoService;

        public ExcelExportService(IInvoiceService invoiceService, IIssuerInfoService issuerInfoService)
        {
            _invoiceService = invoiceService;
            _issuerInfoService = issuerInfoService;
        }

        // 控え用のセル位置を計算
        private string GetCopyCell(string originalCell)
        {
            var rowNumber = GetRowNumber(originalCell);
            var columnPart = originalCell.Substring(0, originalCell.Length - rowNumber.ToString().Length);
            return $"{columnPart}{rowNumber + COPY_ROW_OFFSET}";
        }

        // 控え用の行番号を計算
        private int GetCopyRow(int originalRow)
        {
            return originalRow + COPY_ROW_OFFSET;
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

            // 同一請求書番号の全ての請求書を取得（Dictionary<InvoiceId, Invoice>形式）
            var relatedInvoicesDict = await _invoiceService.GetInvoicesByInvoiceNumberAsync(invoice.InvoiceNumber);
            
            // 明細データがない請求書は除外
            var invoicesWithDetails = relatedInvoicesDict
                .Where(kvp => kvp.Value.InvoiceDetails.Any())
                .OrderBy(kvp => kvp.Value.Subnumber)
                .ToList();
            
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
                
                // 明細データがある請求書のシートを作成
                for (int i = 0; i < invoicesWithDetails.Count; i++)
                {
                    var invoiceEntry = invoicesWithDetails[i];
                    var currentInvoice = invoiceEntry.Value;
                    var currentInvoiceId = invoiceEntry.Key;
                    
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

                    // テンプレートにデータを投入（InvoiceIdも渡す）
                    await PopulateTemplateWithDataAsync(worksheet, currentInvoice, issuerInfo, i == 0, currentInvoiceId);
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

        // 郵便番号をフォーマット（XXX-XXXX形式）
        private string FormatPostalCode(string postalCode)
        {
            if (string.IsNullOrEmpty(postalCode)) return "";
            
            // 数字のみを抽出
            var numbers = new string(postalCode.Where(char.IsDigit).ToArray());
            
            // 7桁の場合のみフォーマット
            if (numbers.Length == 7)
            {
                return $"{numbers.Substring(0, 3)}-{numbers.Substring(3, 4)}";
            }
            
            return postalCode;
        }

        // 日付をフォーマット（yyyy年mm月dd日形式）
        private string FormatDate(DateTime? date)
        {
            if (!date.HasValue) return "";
            return date.Value.ToString("yyyy年MM月dd日");
        }






        
        // テンプレートにデータを投入
        private async Task PopulateTemplateWithDataAsync(IWorksheet worksheet, Invoice invoice, IssuerInfo issuerInfo, bool isFirstSheet = true, int? invoiceId = null)
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
                // 同一請求書番号の全ての合計を計算（既に取得済みのdictionaryから計算）
                var relatedInvoicesDict = await _invoiceService.GetInvoicesByInvoiceNumberAsync(invoice.InvoiceNumber);
                var totalAmount = relatedInvoicesDict.Values.Sum(i => i.Total);
                PopulateTotalAmount(worksheet, totalAmount);
            }
            else
            {
                // 2枚目以降は合計を表示しない（原本と控え両方）
                worksheet.Range[TOTAL_AMOUNT_CELL].Text = "";
                worksheet.Range[GetCopyCell(TOTAL_AMOUNT_CELL)].Text = "";
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
                var relatedInvoicesDict = await _invoiceService.GetInvoicesByInvoiceNumberAsync(invoice.InvoiceNumber);
                PopulateAggregatedTotals(worksheet, relatedInvoicesDict.Values.ToList());
            }
            else
            {
                PopulateTotals(worksheet, invoice);
            }
        }

        // 請求書情報を設定（原本と控え）
        private void PopulateInvoiceInfo(IWorksheet worksheet, Invoice invoice, IssuerInfo issuerInfo)
        {
            // 請求書番号（複数車両の場合は{InvoiceNumber}-{Subnumber}の形式）
            var displayInvoiceNumber = invoice.Subnumber > 1 ? $"{invoice.InvoiceNumber}-{invoice.Subnumber}" : invoice.InvoiceNumber;
            var invoiceDate = FormatDate(invoice.InvoiceDate);
            var invoiceRegistrationNumber = issuerInfo?.InvoiceNumber ?? "";

            // 原本
            worksheet.Range[INVOICE_NUMBER_CELL].Text = displayInvoiceNumber ?? "";
            worksheet.Range[INVOICE_DATE_CELL].Text = invoiceDate;
            worksheet.Range[INVOICE_REGISTRATION_NUMBER_CELL].Text = invoiceRegistrationNumber;
            
            // 控え
            worksheet.Range[GetCopyCell(INVOICE_NUMBER_CELL)].Text = displayInvoiceNumber ?? "";
            worksheet.Range[GetCopyCell(INVOICE_DATE_CELL)].Text = invoiceDate;
            worksheet.Range[GetCopyCell(INVOICE_REGISTRATION_NUMBER_CELL)].Text = invoiceRegistrationNumber;
        }

        // 発行者情報を設定（原本と控え）
        private void PopulateIssuerInfo(IWorksheet worksheet, IssuerInfo issuerInfo)
        {
            if (issuerInfo == null) return;
            
            var postalCode = FormatPostalCode(issuerInfo.PostalCode);
            var address = issuerInfo.Address ?? "";
            var companyName = issuerInfo.CompanyName ?? "";
            var positionName = $"{issuerInfo.Position ?? ""}　{issuerInfo.Name ?? ""}";
            var phoneNumber = $"TEL {issuerInfo.PhoneNumber ?? ""}";
            var faxNumber = $"FAX {issuerInfo.FaxNumber ?? ""}";

            // 原本
            worksheet.Range[ISSUER_POSTAL_CODE_CELL].Text = postalCode;
            worksheet.Range[ISSUER_ADDRESS_CELL].Text = address;
            worksheet.Range[ISSUER_COMPANY_NAME_CELL].Text = companyName;
            worksheet.Range[ISSUER_POSITION_NAME_CELL].Text = positionName;
            worksheet.Range[ISSUER_PHONE_CELL].Text = phoneNumber;
            worksheet.Range[ISSUER_FAX_CELL].Text = faxNumber;

            // 控え
            worksheet.Range[GetCopyCell(ISSUER_POSTAL_CODE_CELL)].Text = postalCode;
            worksheet.Range[GetCopyCell(ISSUER_ADDRESS_CELL)].Text = address;
            worksheet.Range[GetCopyCell(ISSUER_COMPANY_NAME_CELL)].Text = companyName;
            worksheet.Range[GetCopyCell(ISSUER_POSITION_NAME_CELL)].Text = positionName;
            worksheet.Range[GetCopyCell(ISSUER_PHONE_CELL)].Text = phoneNumber;
            worksheet.Range[GetCopyCell(ISSUER_FAX_CELL)].Text = faxNumber;
        }

        // 請求先情報を設定（原本と控え）
        private void PopulateClientInfo(IWorksheet worksheet, AutoDealerSphere.Shared.Models.Client client)
        {
            if (client == null) return;

            var zip = FormatPostalCode(client.Zip);
            var address = client.Address ?? "";
            var name = client.Name ?? "";
            
            // 原本
            worksheet.Range[CLIENT_ZIP_CELL].Text = zip;
            worksheet.Range[CLIENT_ADDRESS_CELL].Text = address;
            worksheet.Range[CLIENT_NAME_CELL].Text = name;

            // 控え
            worksheet.Range[GetCopyCell(CLIENT_ZIP_CELL)].Text = zip;
            worksheet.Range[GetCopyCell(CLIENT_ADDRESS_CELL)].Text = address;
            worksheet.Range[GetCopyCell(CLIENT_NAME_CELL)].Text = name;
        }

        // 合計金額を設定（原本と控え）
        private void PopulateTotalAmount(IWorksheet worksheet, decimal total)
        {
            var totalText = $"¥{total:N0}";
            
            // 原本
            worksheet.Range[TOTAL_AMOUNT_CELL].Text = totalText;
            
            // 控え
            worksheet.Range[GetCopyCell(TOTAL_AMOUNT_CELL)].Text = totalText;
        }

        // 振込先口座情報を設定（原本と控え）
        private void PopulateBankAccountInfo(IWorksheet worksheet, IssuerInfo issuerInfo)
        {
            if (issuerInfo == null) return;
            
            // 第1銀行口座
            if (!string.IsNullOrEmpty(issuerInfo.Bank1Name))
            {
                var bank1Info = $"{issuerInfo.Bank1Name ?? ""} {issuerInfo.Bank1BranchName ?? ""} {issuerInfo.Bank1AccountType ?? ""} {issuerInfo.Bank1AccountNumber ?? ""} {issuerInfo.Bank1AccountHolder ?? ""}".Trim();
                
                // 原本
                worksheet.Range[BANK1_INFO_CELL].Text = bank1Info;
                
                // 控え
                worksheet.Range[GetCopyCell(BANK1_INFO_CELL)].Text = bank1Info;
            }
            
            // 第2銀行口座
            if (!string.IsNullOrEmpty(issuerInfo.Bank2Name))
            {
                var bank2Info = $"{issuerInfo.Bank2Name ?? ""} {issuerInfo.Bank2BranchName ?? ""} {issuerInfo.Bank2AccountType ?? ""} {issuerInfo.Bank2AccountNumber ?? ""} {issuerInfo.Bank2AccountHolder ?? ""}".Trim();
                
                // 原本
                worksheet.Range[BANK2_INFO_CELL].Text = bank2Info;
                
                // 控え
                worksheet.Range[GetCopyCell(BANK2_INFO_CELL)].Text = bank2Info;
            }
        }

        // 車両情報を設定（原本と控え）
        private void PopulateVehicleInfo(IWorksheet worksheet, Vehicle vehicle, decimal? mileage)
        {
            if (vehicle == null) return;
            
            // データ準備
            var licensePlate = FormatLicensePlate(vehicle);
            var vehicleName = vehicle.VehicleName ?? "";
            var chassisNumber = vehicle.ChassisNumber ?? "";
            var firstRegistrationDate = FormatDate(vehicle.FirstRegistrationDate);
            var inspectionExpiryDate = FormatDate(vehicle.InspectionExpiryDate);
            var vehicleModel = vehicle.VehicleModel ?? "";
            var mileageText = mileage.HasValue ? $"{mileage:N0} km" : "";

            // 原本
            worksheet.Range[VEHICLE_LICENSE_PLATE_CELL].Text = licensePlate;
            worksheet.Range[VEHICLE_NAME_CELL].Text = vehicleName;
            worksheet.Range[VEHICLE_CHASSIS_NUMBER_CELL].Text = chassisNumber;
            worksheet.Range[VEHICLE_FIRST_REGISTRATION_CELL].Text = firstRegistrationDate;
            worksheet.Range[VEHICLE_INSPECTION_EXPIRY_CELL].Text = inspectionExpiryDate;
            worksheet.Range[VEHICLE_MODEL_CELL].Text = vehicleModel;
            worksheet.Range[VEHICLE_MILEAGE_CELL].Text = mileageText;

            // 控え
            worksheet.Range[GetCopyCell(VEHICLE_LICENSE_PLATE_CELL)].Text = licensePlate;
            worksheet.Range[GetCopyCell(VEHICLE_NAME_CELL)].Text = vehicleName;
            worksheet.Range[GetCopyCell(VEHICLE_CHASSIS_NUMBER_CELL)].Text = chassisNumber;
            worksheet.Range[GetCopyCell(VEHICLE_FIRST_REGISTRATION_CELL)].Text = firstRegistrationDate;
            worksheet.Range[GetCopyCell(VEHICLE_INSPECTION_EXPIRY_CELL)].Text = inspectionExpiryDate;
            worksheet.Range[GetCopyCell(VEHICLE_MODEL_CELL)].Text = vehicleModel;
            worksheet.Range[GetCopyCell(VEHICLE_MILEAGE_CELL)].Text = mileageText;
        }

        // 請求明細を設定（原本と控え）
        private void PopulateInvoiceDetails(IWorksheet worksheet, Invoice invoice)
        {
            int row = INVOICE_DETAILS_START_ROW;
            var taxableDetails = invoice.InvoiceDetails
                .Where(d => d.Type != "法定費用")
                .OrderBy(d => d.DisplayOrder);

            foreach (var detail in taxableDetails)
            {
                if (row > INVOICE_DETAILS_END_ROW) break;
                
                var itemName = detail.ItemName ?? "";
                var repairMethod = detail.RepairMethod ?? "";
                var unitPrice = (double)detail.UnitPrice;
                var quantity = (double)detail.Quantity;
                var amount = (double)(detail.Quantity * detail.UnitPrice);
                var laborCost = (double)detail.LaborCost;

                // 原本
                worksheet.Range[$"A{row}"].Text = itemName;
                worksheet.Range[$"F{row}"].Text = repairMethod;
                worksheet.Range[$"H{row}"].Number = unitPrice;
                worksheet.Range[$"J{row}"].Number = quantity;
                worksheet.Range[$"K{row}"].Number = amount;
                worksheet.Range[$"M{row}"].Number = laborCost;

                // 控え
                var copyRow = GetCopyRow(row);
                worksheet.Range[$"A{copyRow}"].Text = itemName;
                worksheet.Range[$"F{copyRow}"].Text = repairMethod;
                worksheet.Range[$"H{copyRow}"].Number = unitPrice;
                worksheet.Range[$"J{copyRow}"].Number = quantity;
                worksheet.Range[$"K{copyRow}"].Number = amount;
                worksheet.Range[$"M{copyRow}"].Number = laborCost;

                row++;
            }
        }

        // 非課税項目を設定（原本と控え）
        private void PopulateNonTaxableItems(IWorksheet worksheet, Invoice invoice)
        {
            int row = NON_TAXABLE_START_ROW;
            var nonTaxableItems = invoice.InvoiceDetails
                .Where(d => d.Type == "法定費用")
                .OrderBy(d => d.DisplayOrder);

            foreach (var item in nonTaxableItems)
            {
                if (row > NON_TAXABLE_END_ROW) break;
                
                var itemName = item.ItemName ?? "";
                var unitPrice = (double)item.UnitPrice;

                // 原本
                worksheet.Range[$"A{row}"].Text = itemName;
                worksheet.Range[$"F{row}"].Number = unitPrice;

                // 控え
                var copyRow = GetCopyRow(row);
                worksheet.Range[$"A{copyRow}"].Text = itemName;
                worksheet.Range[$"F{copyRow}"].Number = unitPrice;

                row++;
            }
        }

        // 合計を設定（原本と控え）
        private void PopulateTotals(IWorksheet worksheet, Invoice invoice)
        {
            var taxableDetails = invoice.InvoiceDetails.Where(d => d.Type != "法定費用");
            var partsSubTotal = taxableDetails.Sum(d => d.Quantity * d.UnitPrice);
            var laborSubTotal = taxableDetails.Sum(d => d.LaborCost);
            var nonTaxableTotal = invoice.InvoiceDetails.Where(d => d.Type == "法定費用").Sum(d => d.UnitPrice);
            
            decimal taxableTotal = partsSubTotal + laborSubTotal;
            int tax = (int)(taxableTotal * 0.1m);

            // 原本
            // ページ小計
            worksheet.Range[PARTS_SUBTOTAL_CELL].Number = (double)partsSubTotal;
            worksheet.Range[LABOR_SUBTOTAL_CELL].Number = (double)laborSubTotal;
            
            // 小計
            worksheet.Range[PARTS_SUBTOTAL2_CELL].Number = (double)partsSubTotal;
            worksheet.Range[LABOR_SUBTOTAL2_CELL].Number = (double)laborSubTotal;
            
            // 課税額計
            worksheet.Range[TAXABLE_TOTAL_CELL].Number = (double)taxableTotal;
            
            // 消費税
            worksheet.Range[TAX_CELL].Number = tax;
            
            // 非課税額計
            worksheet.Range[NON_TAXABLE_TOTAL_CELL].Number = (double)nonTaxableTotal;
            worksheet.Range[NON_TAXABLE_TOTAL_REF_CELL].Formula = $"={NON_TAXABLE_TOTAL_CELL}";
            
            // 合計
            worksheet.Range[GRAND_TOTAL_CELL].Formula = $"={TAXABLE_TOTAL_CELL}+{TAX_CELL}+{NON_TAXABLE_TOTAL_REF_CELL}";

            // 控え
            // ページ小計
            worksheet.Range[GetCopyCell(PARTS_SUBTOTAL_CELL)].Number = (double)partsSubTotal;
            worksheet.Range[GetCopyCell(LABOR_SUBTOTAL_CELL)].Number = (double)laborSubTotal;
            
            // 小計
            worksheet.Range[GetCopyCell(PARTS_SUBTOTAL2_CELL)].Number = (double)partsSubTotal;
            worksheet.Range[GetCopyCell(LABOR_SUBTOTAL2_CELL)].Number = (double)laborSubTotal;
            
            // 課税額計
            worksheet.Range[GetCopyCell(TAXABLE_TOTAL_CELL)].Number = (double)taxableTotal;
            
            // 消費税
            worksheet.Range[GetCopyCell(TAX_CELL)].Number = tax;
            
            // 非課税額計
            worksheet.Range[GetCopyCell(NON_TAXABLE_TOTAL_CELL)].Number = (double)nonTaxableTotal;
            worksheet.Range[GetCopyCell(NON_TAXABLE_TOTAL_REF_CELL)].Formula = $"={GetCopyCell(NON_TAXABLE_TOTAL_CELL)}";
            
            // 合計
            worksheet.Range[GetCopyCell(GRAND_TOTAL_CELL)].Formula = $"={GetCopyCell(TAXABLE_TOTAL_CELL)}+{GetCopyCell(TAX_CELL)}+{GetCopyCell(NON_TAXABLE_TOTAL_REF_CELL)}";
        }

        // 複数請求書の集計合計を設定（最初のシートのみ、原本と控え）
        private void PopulateAggregatedTotals(IWorksheet worksheet, List<Invoice> invoices)
        {
            var allTaxableDetails = invoices.SelectMany(i => i.InvoiceDetails.Where(d => d.Type != "法定費用"));
            var allPartsSubTotal = allTaxableDetails.Sum(d => d.Quantity * d.UnitPrice);
            var allLaborSubTotal = allTaxableDetails.Sum(d => d.LaborCost);
            var allNonTaxableTotal = invoices.SelectMany(i => i.InvoiceDetails.Where(d => d.Type == "法定費用")).Sum(d => d.UnitPrice);
            
            decimal allTaxableTotal = allPartsSubTotal + allLaborSubTotal;
            int allTax = (int)(allTaxableTotal * 0.1m);

            // 原本
            // ページ小計
            worksheet.Range[PARTS_SUBTOTAL_CELL].Number = (double)allPartsSubTotal;
            worksheet.Range[LABOR_SUBTOTAL_CELL].Number = (double)allLaborSubTotal;
            
            // 小計
            worksheet.Range[PARTS_SUBTOTAL2_CELL].Number = (double)allPartsSubTotal;
            worksheet.Range[LABOR_SUBTOTAL2_CELL].Number = (double)allLaborSubTotal;
            
            // 課税額計
            worksheet.Range[TAXABLE_TOTAL_CELL].Number = (double)allTaxableTotal;
            
            // 消費税
            worksheet.Range[TAX_CELL].Number = allTax;
            
            // 非課税額計
            worksheet.Range[NON_TAXABLE_TOTAL_CELL].Number = (double)allNonTaxableTotal;
            worksheet.Range[NON_TAXABLE_TOTAL_REF_CELL].Formula = $"={NON_TAXABLE_TOTAL_CELL}";
            
            // 合計
            worksheet.Range[GRAND_TOTAL_CELL].Formula = $"={TAXABLE_TOTAL_CELL}+{TAX_CELL}+{NON_TAXABLE_TOTAL_REF_CELL}";

            // 控え
            // ページ小計
            worksheet.Range[GetCopyCell(PARTS_SUBTOTAL_CELL)].Number = (double)allPartsSubTotal;
            worksheet.Range[GetCopyCell(LABOR_SUBTOTAL_CELL)].Number = (double)allLaborSubTotal;
            
            // 小計
            worksheet.Range[GetCopyCell(PARTS_SUBTOTAL2_CELL)].Number = (double)allPartsSubTotal;
            worksheet.Range[GetCopyCell(LABOR_SUBTOTAL2_CELL)].Number = (double)allLaborSubTotal;
            
            // 課税額計
            worksheet.Range[GetCopyCell(TAXABLE_TOTAL_CELL)].Number = (double)allTaxableTotal;
            
            // 消費税
            worksheet.Range[GetCopyCell(TAX_CELL)].Number = allTax;
            
            // 非課税額計
            worksheet.Range[GetCopyCell(NON_TAXABLE_TOTAL_CELL)].Number = (double)allNonTaxableTotal;
            worksheet.Range[GetCopyCell(NON_TAXABLE_TOTAL_REF_CELL)].Formula = $"={GetCopyCell(NON_TAXABLE_TOTAL_CELL)}";
            
            // 合計
            worksheet.Range[GetCopyCell(GRAND_TOTAL_CELL)].Formula = $"={GetCopyCell(TAXABLE_TOTAL_CELL)}+{GetCopyCell(TAX_CELL)}+{GetCopyCell(NON_TAXABLE_TOTAL_REF_CELL)}";
        }
    }
}