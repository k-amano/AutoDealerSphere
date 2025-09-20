using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoDealerSphere.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutoDealerSphere.Server.Services
{
    public class VehicleImportService : IVehicleImportService
    {
        private readonly IDbContextFactory<SQLDBContext> _contextFactory;
        private readonly ILogger<VehicleImportService> _logger;

        public VehicleImportService(IDbContextFactory<SQLDBContext> contextFactory, ILogger<VehicleImportService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<(int clientsImported, int vehiclesImported, List<string> errors)> ImportFromCsvAsync(string filePath, bool replaceExisting = false)
        {
            var errors = new List<string>();
            int clientsImported = 0;
            int vehiclesImported = 0;
            var processedClientNames = new HashSet<string>(); // 処理された顧客名を追跡

            try
            {
                // ファイルをShift-JISで読み込む
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var lines = await File.ReadAllLinesAsync(filePath, Encoding.GetEncoding("shift_jis"));

                if (lines.Length < 2)
                {
                    errors.Add("ファイルにデータが含まれていません。");
                    return (0, 0, errors);
                }

                // ヘッダー行を解析
                var headers = ParseCsvLine(lines[0]);
                var columnIndexes = GetColumnIndexes(headers);

                using var context = await _contextFactory.CreateDbContextAsync();

                // 既存データを置き換える場合
                if (replaceExisting)
                {
                    try
                    {
                        // 既存の車両データをすべて削除
                        var existingVehicles = await context.Vehicles.ToListAsync();
                        context.Vehicles.RemoveRange(existingVehicles);
                        await context.SaveChangesAsync();

                        // 車両の連番をリセット（SQLiteの場合）
                        await context.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence WHERE name='Vehicles'");

                        // 既存の顧客データをすべて削除
                        var existingClients = await context.Clients.ToListAsync();
                        context.Clients.RemoveRange(existingClients);
                        await context.SaveChangesAsync();
                        
                        // 顧客の連番もリセット
                        await context.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence WHERE name='Clients'");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "既存データの削除中にエラーが発生しました。");
                        errors.Add($"既存データの削除に失敗しました: {ex.Message}");
                        return (0, 0, errors);
                    }
                }

                // データ行を処理
                for (int i = 1; i < lines.Length; i++)
                {
                    try
                    {
                        var values = ParseCsvLine(lines[i]);
                        if (values.Count < headers.Count)
                        {
                            _logger.LogWarning($"行 {i + 1}: カラム数が不足しています。スキップします。");
                            continue;
                        }

                        // 必要なフィールドが空の場合はスキップ
                        var name = GetValue(values, columnIndexes.Name);
                        
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            _logger.LogInformation($"行 {i + 1}: 氏名が空のためスキップします。");
                            continue;
                        }

                        // 住所と郵便番号を取得
                        var zip = GetValue(values, columnIndexes.Zip);
                        var address = GetValue(values, columnIndexes.Address);
                        
                        // 「岩瀬　艶雄」の住所を統一
                        if (name == "岩瀬　艶雄" || name == "岩瀬 艶雄")
                        {
                            address = "香川県東かがわ市西村1445";
                        }
                        
                        // 既存の顧客を氏名で検索（同じ氏名は1つのClientにまとめる）
                        var client = await context.Clients.FirstOrDefaultAsync(c => c.Name == name);

                        if (client == null)
                        {
                            // 都道府県コードを取得し、住所から都道府県を削除
                            var prefectureCode = Prefecture.GetCodeFromAddress(address);
                            var prefectureName = Prefecture.GetName(prefectureCode);
                            var addressWithoutPrefecture = address;
                            if (!string.IsNullOrWhiteSpace(prefectureName) && address.StartsWith(prefectureName))
                            {
                                addressWithoutPrefecture = address.Substring(prefectureName.Length);
                            }

                            // 新規顧客として作成
                            client = new AutoDealerSphere.Shared.Models.Client
                            {
                                // IDは自動採番されるため指定しない
                                Name = name,
                                Email = "", // 必須フィールドのため空文字列
                                Zip = !string.IsNullOrWhiteSpace(zip) ? zip.Replace("-", "") : "",
                                Address = !string.IsNullOrWhiteSpace(addressWithoutPrefecture) ? addressWithoutPrefecture : "",
                                Prefecture = prefectureCode // 住所から都道府県コードを判定
                            };
                            
                            // 顧客を追加して保存
                            context.Clients.Add(client);
                            await context.SaveChangesAsync(); // 新規顧客を保存してIDを確定
                            clientsImported++;
                        }
                        else
                        {
                            // 既存顧客の情報を更新
                            bool updated = false;
                            
                            // 郵便番号が空で、インポートデータに郵便番号がある場合は更新
                            if (string.IsNullOrWhiteSpace(client.Zip) && !string.IsNullOrWhiteSpace(zip))
                            {
                                client.Zip = zip.Replace("-", "");
                                updated = true;
                            }
                            
                            // 住所が空で、インポートデータに住所がある場合は更新
                            if (string.IsNullOrWhiteSpace(client.Address) && !string.IsNullOrWhiteSpace(address))
                            {
                                var prefectureCode = Prefecture.GetCodeFromAddress(address);
                                var prefectureName = Prefecture.GetName(prefectureCode);
                                var addressWithoutPrefecture = address;
                                if (!string.IsNullOrWhiteSpace(prefectureName) && address.StartsWith(prefectureName))
                                {
                                    addressWithoutPrefecture = address.Substring(prefectureName.Length);
                                }
                                client.Address = addressWithoutPrefecture;
                                client.Prefecture = prefectureCode;
                                updated = true;
                            }

                            // 都道府県が未設定（0）で、住所がある場合は更新
                            if (client.Prefecture == 0 && !string.IsNullOrWhiteSpace(address))
                            {
                                client.Prefecture = Prefecture.GetCodeFromAddress(address);
                                updated = true;
                            }
                            
                            if (updated)
                            {
                                await context.SaveChangesAsync();
                            }
                        }

                        // 車両番号を4つのパートに分割
                        var vehicleNumber = GetValue(values, columnIndexes.LicensePlateNumber);
                        string? plateLocation = null;
                        string? plateClassification = null;
                        string? plateHiragana = null;
                        string? plateNumber = null;

                        if (!string.IsNullOrWhiteSpace(vehicleNumber))
                        {
                            var parts = ParseVehicleNumber(vehicleNumber);
                            plateLocation = parts.location;
                            plateClassification = parts.classification;
                            plateHiragana = parts.hiragana;
                            plateNumber = parts.number;
                        }

                        // 車両情報を作成
                        var vehicle = new Vehicle
                        {
                            ClientId = client.Id, // 保存後のIDを使用
                            LicensePlateLocation = plateLocation ?? GetValue(values, columnIndexes.LicensePlateLocation),
                            LicensePlateClassification = plateClassification ?? GetValue(values, columnIndexes.LicensePlateClassification),
                            LicensePlateHiragana = plateHiragana ?? GetValue(values, columnIndexes.LicensePlateHiragana),
                            LicensePlateNumber = plateNumber ?? GetValue(values, columnIndexes.LicensePlateNumber),
                            KeyNumber = GetValue(values, columnIndexes.KeyNumber),
                            ChassisNumber = GetValue(values, columnIndexes.ChassisNumber),
                            TypeCertificationNumber = GetValue(values, columnIndexes.TypeCertificationNumber),
                            CategoryNumber = GetValue(values, columnIndexes.CategoryNumber),
                            VehicleName = GetValue(values, columnIndexes.VehicleName),
                            VehicleModel = GetValue(values, columnIndexes.VehicleModel),
                            Purpose = GetValue(values, columnIndexes.Purpose),
                            PersonalBusinessUse = GetValue(values, columnIndexes.PersonalBusinessUse),
                            BodyShape = GetValue(values, columnIndexes.BodyShape),
                            ModelCode = GetValue(values, columnIndexes.ModelCode),
                            EngineModel = GetValue(values, columnIndexes.EngineModel),
                            FuelType = GetValue(values, columnIndexes.FuelType),
                            InspectionCertificateNumber = GetValue(values, columnIndexes.InspectionCertificateNumber),
                            UserNameOrCompany = GetValue(values, columnIndexes.UserNameOrCompany),
                            UserAddress = GetValue(values, columnIndexes.UserAddress),
                            BaseLocation = GetValue(values, columnIndexes.BaseLocation),
                            // 所有者情報（T_顧客情報.txtでは顧客が所有者）
                            OwnerNameOrCompany = client.Name,
                            OwnerAddress = client.Address,
                            OwnerPostalCode = client.Zip,
                            // インポート情報
                            ImportSource = "CSV",
                            ImportDate = DateTime.Now
                        };

                        // 数値フィールドの解析
                        ParseNumericFields(values, columnIndexes, vehicle);
                        
                        // 日付フィールドの解析
                        ParseDateFields(values, columnIndexes, vehicle);

                        context.Vehicles.Add(vehicle);
                        vehiclesImported++;

                        // バッチで保存（パフォーマンス向上のため）
                        if (i % 100 == 0)
                        {
                            await context.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"行 {i + 1}: エラー - {ex.Message}");
                        _logger.LogError(ex, $"行 {i + 1} の処理中にエラーが発生しました");
                    }
                }

                // 最後の保存
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                errors.Add($"ファイル処理エラー: {ex.Message}");
                _logger.LogError(ex, "インポート処理中にエラーが発生しました");
            }

            return (clientsImported, vehiclesImported, errors);
        }

        private List<string> ParseCsvLine(string line)
        {
            var values = new List<string>();
            var currentValue = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentValue.Append('"');
                        i++; // Skip next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            values.Add(currentValue.ToString());
            return values;
        }

        private ColumnIndexes GetColumnIndexes(List<string> headers)
        {
            var indexes = new ColumnIndexes();
            
            for (int i = 0; i < headers.Count; i++)
            {
                var header = headers[i];
                switch (header)
                {
                    case "fld_顧客ID": indexes.ClientId = i; break;
                    case "fld_氏名": indexes.Name = i; break;
                    case "fld_郵便番号": indexes.Zip = i; break;
                    case "fld_所有者の住所": indexes.Address = i; break;
                    case "fld_車検証表記": indexes.LicensePlateLocation = i; break;
                    case "fld_車検証分類": indexes.LicensePlateClassification = i; break;
                    case "fld_車検証ひらがな": indexes.LicensePlateHiragana = i; break;
                    case "fld_車両番号": indexes.LicensePlateNumber = i; break;
                    case "Key 番号": indexes.KeyNumber = i; break;
                    case "fld_車体番号": indexes.ChassisNumber = i; break;
                    case "fld_型式指定番号": indexes.TypeCertificationNumber = i; break;
                    case "fld_類別区分番号": indexes.CategoryNumber = i; break;
                    case "fld_車名": indexes.VehicleName = i; break;
                    case "fld_車種": indexes.VehicleModel = i; break;
                    case "fld_走行距離": indexes.Mileage = i; break;
                    case "fld_登録年月日": indexes.FirstRegistrationDate = i; break;
                    case "fld_車検証の種": indexes.Purpose = i; break;
                    case "fld_用途": indexes.Purpose = i; break;
                    case "fld_自家用・事業用": indexes.PersonalBusinessUse = i; break;
                    case "fld_車体の形状": indexes.BodyShape = i; break;
                    case "fld_乗車定": indexes.SeatingCapacity = i; break;
                    case "fld_最大積載量": indexes.MaxLoadCapacity = i; break;
                    case "fld_車両重量": indexes.VehicleWeight = i; break;
                    case "fld_車両総重量": indexes.VehicleTotalWeight = i; break;
                    case "fld_長さ": indexes.VehicleLength = i; break;
                    case "fld_幅": indexes.VehicleWidth = i; break;
                    case "fld_高さ": indexes.VehicleHeight = i; break;
                    case "fld_前軸重": indexes.FrontAxleWeight = i; break;
                    case "fld_後軸重": indexes.RearAxleWeight = i; break;
                    case "fld_型式": indexes.ModelCode = i; break;
                    case "fld_原動機の型式": indexes.EngineModel = i; break;
                    case "fld_総排気量又は定格出力": indexes.Displacement = i; break;
                    case "fld_燃料の種": indexes.FuelType = i; break;
                    case "fld_有効期限の満了する日": indexes.InspectionExpiryDate = i; break;
                    case "fld_車検年月": indexes.InspectionCertificateNumber = i; break;
                    case "fld_使用者の氏名又は名称": indexes.UserNameOrCompany = i; break;
                    case "fld_使用者の住所": indexes.UserAddress = i; break;
                    case "fld_使用の本拠の位置": indexes.BaseLocation = i; break;
                }
            }
            
            return indexes;
        }

        private string? GetValue(List<string> values, int index)
        {
            if (index >= 0 && index < values.Count)
            {
                var value = values[index].Trim();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
            return null;
        }

        private void ParseNumericFields(List<string> values, ColumnIndexes indexes, Vehicle vehicle)
        {
            // 走行距離
            if (decimal.TryParse(GetValue(values, indexes.Mileage), out decimal mileage))
            {
                vehicle.Mileage = mileage;
            }

            // 乗車定員
            if (int.TryParse(GetValue(values, indexes.SeatingCapacity), out int seating))
            {
                vehicle.SeatingCapacity = seating;
            }

            // 最大積載量
            if (int.TryParse(GetValue(values, indexes.MaxLoadCapacity), out int maxLoad))
            {
                vehicle.MaxLoadCapacity = maxLoad;
            }

            // 車両重量
            if (int.TryParse(GetValue(values, indexes.VehicleWeight), out int weight))
            {
                vehicle.VehicleWeight = weight;
            }

            // 車両総重量
            if (int.TryParse(GetValue(values, indexes.VehicleTotalWeight), out int totalWeight))
            {
                vehicle.VehicleTotalWeight = totalWeight;
            }

            // 寸法関連
            if (int.TryParse(GetValue(values, indexes.VehicleLength), out int length))
            {
                vehicle.VehicleLength = length;
            }

            if (int.TryParse(GetValue(values, indexes.VehicleWidth), out int width))
            {
                vehicle.VehicleWidth = width;
            }

            if (int.TryParse(GetValue(values, indexes.VehicleHeight), out int height))
            {
                vehicle.VehicleHeight = height;
            }

            if (int.TryParse(GetValue(values, indexes.FrontAxleWeight), out int frontAxle))
            {
                vehicle.FrontAxleWeight = frontAxle;
            }

            if (int.TryParse(GetValue(values, indexes.RearAxleWeight), out int rearAxle))
            {
                vehicle.RearAxleWeight = rearAxle;
            }

            // 排気量
            if (decimal.TryParse(GetValue(values, indexes.Displacement), out decimal displacement))
            {
                vehicle.Displacement = displacement;
            }

            // 排気量と車両重量から車両カテゴリを自動判定
            vehicle.VehicleCategoryId = DetermineVehicleCategory(vehicle.Displacement, vehicle.VehicleWeight);
        }

        private void ParseDateFields(List<string> values, ColumnIndexes indexes, Vehicle vehicle)
        {
            var dateFormats = new[] {
                "yyyy/M/d H:mm:ss",
                "yyyy/M/d",
                "yyyy年M月d日",
                "令和y年M月d日",
                "平成y年M月d日"
            };

            // 登録年月日
            var firstRegDate = GetValue(values, indexes.FirstRegistrationDate);
            if (!string.IsNullOrWhiteSpace(firstRegDate))
            {
                if (DateTime.TryParseExact(firstRegDate, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                {
                    vehicle.FirstRegistrationDate = date;
                }
            }

            // 車検有効期限
            var expiryDate = GetValue(values, indexes.InspectionExpiryDate);
            if (!string.IsNullOrWhiteSpace(expiryDate))
            {
                if (DateTime.TryParseExact(expiryDate, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                {
                    vehicle.InspectionExpiryDate = date;
                }
            }

        }

        private (string? location, string? classification, string? hiragana, string? number) ParseVehicleNumber(string vehicleNumber)
        {
            if (string.IsNullOrWhiteSpace(vehicleNumber))
                return (null, null, null, null);

            // 車両番号の形式: "香川580め9544" または "香川500 め 3924"
            // スペースを削除
            vehicleNumber = vehicleNumber.Replace(" ", "").Replace("　", "");

            // パターン解析
            var location = new System.Text.StringBuilder();
            var classification = new System.Text.StringBuilder();
            var hiragana = new System.Text.StringBuilder();
            var number = new System.Text.StringBuilder();

            int phase = 0; // 0: location, 1: classification, 2: hiragana, 3: number

            foreach (char c in vehicleNumber)
            {
                if (phase == 0)
                {
                    // 地域名（漢字、ひらがな、カタカナ）
                    if (char.IsDigit(c))
                    {
                        phase = 1;
                        classification.Append(c);
                    }
                    else
                    {
                        location.Append(c);
                    }
                }
                else if (phase == 1)
                {
                    // 分類番号（数字）
                    if (char.IsDigit(c))
                    {
                        classification.Append(c);
                    }
                    else
                    {
                        phase = 2;
                        hiragana.Append(c);
                    }
                }
                else if (phase == 2)
                {
                    // ひらがな（通常1文字）
                    if (char.IsDigit(c))
                    {
                        phase = 3;
                        number.Append(c);
                    }
                    else
                    {
                        hiragana.Append(c);
                    }
                }
                else if (phase == 3)
                {
                    // 番号（数字とハイフン）
                    if (char.IsDigit(c) || c == '-' || c == '・')
                    {
                        number.Append(c);
                    }
                }
            }

            return (
                location.Length > 0 ? location.ToString() : null,
                classification.Length > 0 ? classification.ToString() : null,
                hiragana.Length > 0 ? hiragana.ToString() : null,
                number.Length > 0 ? number.ToString() : null
            );
        }

        private int? DetermineVehicleCategory(decimal? displacement, int? vehicleWeight)
        {
            // 排気量が660cc以下の場合は軽自動車（ID: 1）
            if (displacement.HasValue && displacement.Value <= 660)
            {
                return 1; // 軽自動車
            }

            // 軽自動車以外は車両重量のみで判定（自動車重量税の基準）
            if (vehicleWeight.HasValue)
            {
                if (vehicleWeight.Value <= 1000)
                {
                    return 2; // 小型車（1.0t以下）
                }
                else if (vehicleWeight.Value <= 1500)
                {
                    return 3; // 普通車（1.5t以下）
                }
                else if (vehicleWeight.Value <= 2000)
                {
                    return 4; // 中型車（2.0t以下）
                }
                else
                {
                    return 5; // 大型車（2.0t超）
                }
            }

            // 車両重量がない場合はNULL
            return null;
        }

        private class ColumnIndexes
        {
            public int ClientId { get; set; } = -1;
            public int Name { get; set; } = -1;
            public int Zip { get; set; } = -1;
            public int Address { get; set; } = -1;
            public int LicensePlateLocation { get; set; } = -1;
            public int LicensePlateClassification { get; set; } = -1;
            public int LicensePlateHiragana { get; set; } = -1;
            public int LicensePlateNumber { get; set; } = -1;
            public int KeyNumber { get; set; } = -1;
            public int ChassisNumber { get; set; } = -1;
            public int TypeCertificationNumber { get; set; } = -1;
            public int CategoryNumber { get; set; } = -1;
            public int VehicleName { get; set; } = -1;
            public int VehicleModel { get; set; } = -1;
            public int Mileage { get; set; } = -1;
            public int FirstRegistrationDate { get; set; } = -1;
            public int Purpose { get; set; } = -1;
            public int PersonalBusinessUse { get; set; } = -1;
            public int BodyShape { get; set; } = -1;
            public int SeatingCapacity { get; set; } = -1;
            public int MaxLoadCapacity { get; set; } = -1;
            public int VehicleWeight { get; set; } = -1;
            public int VehicleTotalWeight { get; set; } = -1;
            public int VehicleLength { get; set; } = -1;
            public int VehicleWidth { get; set; } = -1;
            public int VehicleHeight { get; set; } = -1;
            public int FrontAxleWeight { get; set; } = -1;
            public int RearAxleWeight { get; set; } = -1;
            public int ModelCode { get; set; } = -1;
            public int EngineModel { get; set; } = -1;
            public int Displacement { get; set; } = -1;
            public int FuelType { get; set; } = -1;
            public int InspectionExpiryDate { get; set; } = -1;
            public int InspectionCertificateNumber { get; set; } = -1;
            public int UserNameOrCompany { get; set; } = -1;
            public int UserAddress { get; set; } = -1;
            public int BaseLocation { get; set; } = -1;
        }
    }
}