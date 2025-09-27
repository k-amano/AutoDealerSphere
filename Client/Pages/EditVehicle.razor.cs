using Microsoft.AspNetCore.Components;
using AutoDealerSphere.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace AutoDealerSphere.Client.Pages
{
    public partial class EditVehicle : ComponentBase
    {
        [Parameter]
        public int VehicleId { get; set; }

        private AutoDealerSphere.Shared.Models.Vehicle? _item;
        private List<AutoDealerSphere.Shared.Models.Client> _clients = new();
        private List<VehicleCategory> _vehicleCategories = new();
        private bool _initialized = false;
        private IBrowserFile? _selectedFile;
        private bool _importing = false;
        private string _importMessage = string.Empty;
        private bool _importSuccess = false;

        protected override async Task OnInitializedAsync()
        {
            await LoadClients();
            await LoadVehicleCategories();
            
            if (VehicleId > 0)
            {
                var response = await Http.GetAsync($"api/vehicles/{VehicleId}");
                if (response.IsSuccessStatusCode)
                {
                    var vehicle = await response.Content.ReadFromJsonAsync<AutoDealerSphere.Shared.Models.Vehicle>();
                    if (vehicle != null)
                    {
                        _item = new AutoDealerSphere.Shared.Models.Vehicle()
                        {
                            Id = vehicle.Id,
                            ClientId = vehicle.ClientId,
                            Client = vehicle.Client,
                            LicensePlateLocation = vehicle.LicensePlateLocation,
                            LicensePlateClassification = vehicle.LicensePlateClassification,
                            LicensePlateHiragana = vehicle.LicensePlateHiragana,
                            LicensePlateNumber = vehicle.LicensePlateNumber,
                            KeyNumber = vehicle.KeyNumber,
                            ChassisNumber = vehicle.ChassisNumber,
                            TypeCertificationNumber = vehicle.TypeCertificationNumber,
                            CategoryNumber = vehicle.CategoryNumber,
                            VehicleName = vehicle.VehicleName,
                            VehicleModel = vehicle.VehicleModel,
                            Mileage = vehicle.Mileage,
                            FirstRegistrationDate = vehicle.FirstRegistrationDate,
                            Purpose = vehicle.Purpose,
                            PersonalBusinessUse = vehicle.PersonalBusinessUse,
                            BodyShape = vehicle.BodyShape,
                            SeatingCapacity = vehicle.SeatingCapacity,
                            MaxLoadCapacity = vehicle.MaxLoadCapacity,
                            VehicleWeight = vehicle.VehicleWeight,
                            VehicleTotalWeight = vehicle.VehicleTotalWeight,
                            VehicleLength = vehicle.VehicleLength,
                            VehicleWidth = vehicle.VehicleWidth,
                            VehicleHeight = vehicle.VehicleHeight,
                            FrontOverhang = vehicle.FrontOverhang,
                            RearOverhang = vehicle.RearOverhang,
                            ModelCode = vehicle.ModelCode,
                            EngineModel = vehicle.EngineModel,
                            Displacement = vehicle.Displacement,
                            FuelType = vehicle.FuelType,
                            InspectionExpiryDate = vehicle.InspectionExpiryDate,
                            InspectionCertificateNumber = vehicle.InspectionCertificateNumber,
                            UserNameOrCompany = vehicle.UserNameOrCompany,
                            UserAddress = vehicle.UserAddress,
                            BaseLocation = vehicle.BaseLocation,
                            VehicleCategoryId = vehicle.VehicleCategoryId,
                            CreatedAt = vehicle.CreatedAt,
                            UpdatedAt = vehicle.UpdatedAt
                        };
                    }
                }
            }
            else
            {
                _item = new AutoDealerSphere.Shared.Models.Vehicle()
                {
                    ClientId = 0,
                    CreatedAt = DateTime.Now
                };
            }

            _initialized = true;
        }

        private async Task LoadClients()
        {
            var response = await Http.GetAsync("api/client");
            if (response.IsSuccessStatusCode)
            {
                _clients = await response.Content.ReadFromJsonAsync<List<AutoDealerSphere.Shared.Models.Client>>() ?? new();
            }
        }

        private async Task LoadVehicleCategories()
        {
            var response = await Http.GetAsync("api/vehiclecategories");
            if (response.IsSuccessStatusCode)
            {
                _vehicleCategories = await response.Content.ReadFromJsonAsync<List<VehicleCategory>>() ?? new();
            }
        }

        private async Task OnClickOK(AutoDealerSphere.Shared.Models.Vehicle vehicle)
        {
            HttpResponseMessage response;
            var json = JsonSerializer.Serialize(vehicle);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            if (vehicle.Id == 0)
            {
                response = await Http.PostAsync("api/vehicles", content);
            }
            else
            {
                vehicle.UpdatedAt = DateTime.Now;
                json = JsonSerializer.Serialize(vehicle);
                content = new StringContent(json, Encoding.UTF8, "application/json");
                response = await Http.PutAsync($"api/vehicles/{vehicle.Id}", content);
            }

            if (response.IsSuccessStatusCode)
            {
                NavigationManager.NavigateTo("/vehiclelist");
            }
        }

        private async Task OnClickDelete(AutoDealerSphere.Shared.Models.Vehicle vehicle)
        {
            if (vehicle.Id > 0)
            {
                var response = await Http.DeleteAsync($"api/vehicles/{vehicle.Id}");
                if (response.IsSuccessStatusCode)
                {
                    NavigationManager.NavigateTo("/vehiclelist");
                }
            }
        }

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        private void OnFileSelected(InputFileChangeEventArgs e)
        {
            _selectedFile = e.File;
            _importMessage = string.Empty;
        }

        private async Task ImportJsonData()
        {
            if (_selectedFile == null || _item == null)
                return;

            _importing = true;
            _importMessage = string.Empty;
            _importSuccess = false;

            try
            {
                // ファイルサイズチェック（最大10MB）
                if (_selectedFile.Size > 10 * 1024 * 1024)
                {
                    _importMessage = "ファイルサイズが大きすぎます（最大10MB）";
                    return;
                }

                // ファイル読み込み
                using var stream = _selectedFile.OpenReadStream(10 * 1024 * 1024);
                using var reader = new StreamReader(stream);
                var jsonContent = await reader.ReadToEndAsync();

                // JSONパース検証
                JsonDocument jsonDoc;
                try
                {
                    jsonDoc = JsonDocument.Parse(jsonContent);

                    // JSONの内容をコンソールに出力
                    await JSRuntime.InvokeVoidAsync("console.log", "=== 取り込むJSONデータ ===");
                    await JSRuntime.InvokeVoidAsync("console.log", jsonContent);
                }
                catch (JsonException ex)
                {
                    _importMessage = $"JSONフォーマットエラー: {ex.Message}";
                    await JSRuntime.InvokeVoidAsync("console.error", "JSONパースエラー:", ex.Message);
                    return;
                }

                // APIに送信
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await Http.PostAsync($"api/vehicles/{_item.Id}/import-json", content);

                if (response.IsSuccessStatusCode)
                {
                    // データベースから最新データを再読み込み
                    await ReloadVehicleData();

                    // 再読み込み後のデータをコンソールに出力
                    await JSRuntime.InvokeVoidAsync("console.log", "=== データベースから再読み込みした車両データ ===");
                    await LogVehicleData(_item);

                    // JSONデータとDBデータの完全検証
                    var (isVerified, verificationMessage) = await VerifyImportedData(jsonDoc, _item);

                    if (isVerified)
                    {
                        _importSuccess = true;
                        // 車検証番号を含むメッセージ
                        var certNumber = _item?.InspectionCertificateNumber ?? "(番号なし)";
                        _importMessage = $"車検証番号{certNumber}のデータを取り込みました";
                    }
                    else
                    {
                        _importSuccess = false;
                        _importMessage = verificationMessage;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _importMessage = $"取り込みエラー: {response.StatusCode} - {errorContent}";
                    await JSRuntime.InvokeVoidAsync("console.error", "API エラー:", response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _importMessage = $"予期しないエラー: {ex.Message}";
                await JSRuntime.InvokeVoidAsync("console.error", "予期しないエラー:", ex.Message);
            }
            finally
            {
                _importing = false;
                StateHasChanged();
            }
        }

        private async Task ReloadVehicleData()
        {
            if (_item == null || _item.Id <= 0)
                return;

            var response = await Http.GetAsync($"api/vehicles/{_item.Id}");
            if (response.IsSuccessStatusCode)
            {
                var vehicle = await response.Content.ReadFromJsonAsync<AutoDealerSphere.Shared.Models.Vehicle>();
                if (vehicle != null)
                {
                    _item = vehicle;
                    await JSRuntime.InvokeVoidAsync("console.log", "車両データを再読み込みしました");
                }
            }
        }

        private async Task LogVehicleData(AutoDealerSphere.Shared.Models.Vehicle vehicle)
        {
            var vehicleData = new
            {
                Id = vehicle.Id,
                InspectionCertificateNumber = vehicle.InspectionCertificateNumber,
                VehicleName = vehicle.VehicleName,
                VehicleModel = vehicle.VehicleModel,
                ChassisNumber = vehicle.ChassisNumber,
                EngineModel = vehicle.EngineModel,
                TypeCertificationNumber = vehicle.TypeCertificationNumber,
                CategoryNumber = vehicle.CategoryNumber,
                FirstRegistrationDate = vehicle.FirstRegistrationDate?.ToString("yyyy-MM-dd"),
                Purpose = vehicle.Purpose,
                PersonalBusinessUse = vehicle.PersonalBusinessUse,
                BodyShape = vehicle.BodyShape,
                SeatingCapacity = vehicle.SeatingCapacity,
                MaxLoadCapacity = vehicle.MaxLoadCapacity,
                VehicleWeight = vehicle.VehicleWeight,
                VehicleTotalWeight = vehicle.VehicleTotalWeight,
                VehicleLength = vehicle.VehicleLength,
                VehicleWidth = vehicle.VehicleWidth,
                VehicleHeight = vehicle.VehicleHeight,
                FrontOverhang = vehicle.FrontOverhang,
                RearOverhang = vehicle.RearOverhang,
                Displacement = vehicle.Displacement,
                FuelType = vehicle.FuelType,
                InspectionExpiryDate = vehicle.InspectionExpiryDate?.ToString("yyyy-MM-dd"),
                UserNameOrCompany = vehicle.UserNameOrCompany,
                UserAddress = vehicle.UserAddress,
                BaseLocation = vehicle.BaseLocation,
                LicensePlateLocation = vehicle.LicensePlateLocation,
                LicensePlateClassification = vehicle.LicensePlateClassification,
                LicensePlateHiragana = vehicle.LicensePlateHiragana,
                LicensePlateNumber = vehicle.LicensePlateNumber,
                UpdatedAt = vehicle.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss")
            };

            await JSRuntime.InvokeVoidAsync("console.table", vehicleData);
        }

        private async Task<(bool isVerified, string message)> VerifyImportedData(JsonDocument jsonDoc, AutoDealerSphere.Shared.Models.Vehicle vehicle)
        {
            await JSRuntime.InvokeVoidAsync("console.log", "=== 車検証JSONデータの完全検証開始 ===");

            var root = jsonDoc.RootElement;
            var comparisons = new List<object>();
            var mismatches = new List<string>();
            var checkedFields = 0;
            var matchedFields = 0;

            // 初期値をFalseに設定 - 全項目が一致して初めてTrueになる
            bool allFieldsMatch = false;

            // 基本情報の検証
            if (root.TryGetProperty("inspection_certificate_number", out var jsonCertNumber))
            {
                checkedFields++;
                var jsonValue = jsonCertNumber.GetString();
                var dbValue = vehicle.InspectionCertificateNumber;
                var isMatch = jsonValue == dbValue;
                if (isMatch) matchedFields++;
                else mismatches.Add($"車検証番号: JSON=\"{jsonValue}\" DB=\"{dbValue ?? "null"}\"");

                comparisons.Add(new { Field = "車検証番号", JSON = jsonValue, Database = dbValue, Match = isMatch ? "○" : "×" });
            }

            if (root.TryGetProperty("vehicle_name", out var jsonVehicleName))
            {
                checkedFields++;
                var jsonValue = jsonVehicleName.GetString();
                var dbValue = vehicle.VehicleName;
                var isMatch = jsonValue == dbValue;
                if (isMatch) matchedFields++;
                else mismatches.Add($"車名: JSON=\"{jsonValue}\" DB=\"{dbValue ?? "null"}\"");

                comparisons.Add(new { Field = "車名", JSON = jsonValue, Database = dbValue, Match = isMatch ? "○" : "×" });
            }

            if (root.TryGetProperty("vehicle_model", out var jsonModel))
            {
                checkedFields++;
                var jsonValue = jsonModel.GetString();
                var dbValue = vehicle.VehicleModel;
                var isMatch = jsonValue == dbValue;
                if (isMatch) matchedFields++;
                else mismatches.Add($"型式: JSON=\"{jsonValue}\" DB=\"{dbValue ?? "null"}\"");

                comparisons.Add(new { Field = "型式", JSON = jsonValue, Database = dbValue, Match = isMatch ? "○" : "×" });
            }

            if (root.TryGetProperty("chassis_number", out var jsonChassis))
            {
                checkedFields++;
                var jsonValue = jsonChassis.GetString();
                var dbValue = vehicle.ChassisNumber;
                var isMatch = jsonValue == dbValue;
                if (isMatch) matchedFields++;
                else mismatches.Add($"車台番号: JSON=\"{jsonValue}\" DB=\"{dbValue ?? "null"}\"");

                comparisons.Add(new { Field = "車台番号", JSON = jsonValue, Database = dbValue, Match = isMatch ? "○" : "×" });
            }

            if (root.TryGetProperty("engine_model", out var jsonEngine))
            {
                checkedFields++;
                var jsonValue = jsonEngine.GetString();
                var dbValue = vehicle.EngineModel;
                var isMatch = jsonValue == dbValue;
                if (isMatch) matchedFields++;
                else mismatches.Add($"エンジン型式: JSON=\"{jsonValue}\" DB=\"{dbValue ?? "null"}\"");

                comparisons.Add(new { Field = "エンジン型式", JSON = jsonValue, Database = dbValue, Match = isMatch ? "○" : "×" });
            }

            if (root.TryGetProperty("type_certification_number", out var jsonTypeCert))
            {
                checkedFields++;
                var jsonValue = jsonTypeCert.GetString();
                var dbValue = vehicle.TypeCertificationNumber;
                var isMatch = jsonValue == dbValue;
                if (isMatch) matchedFields++;
                else mismatches.Add($"型式指定番号: JSON=\"{jsonValue}\" DB=\"{dbValue ?? "null"}\"");

                comparisons.Add(new { Field = "型式指定番号", JSON = jsonValue, Database = dbValue, Match = isMatch ? "○" : "×" });
            }

            if (root.TryGetProperty("category_number", out var jsonCategory))
            {
                checkedFields++;
                var jsonValue = jsonCategory.GetString();
                var dbValue = vehicle.CategoryNumber;
                var isMatch = jsonValue == dbValue;
                if (isMatch) matchedFields++;
                else mismatches.Add($"類別区分番号: JSON=\"{jsonValue}\" DB=\"{dbValue ?? "null"}\"");

                comparisons.Add(new { Field = "類別区分番号", JSON = jsonValue, Database = dbValue, Match = isMatch ? "○" : "×" });
            }

            // ナンバープレート情報
            if (root.TryGetProperty("license_plate_location", out var jsonLocation))
            {
                checkedFields++;
                var jsonValue = jsonLocation.GetString();
                var dbValue = vehicle.LicensePlateLocation;
                var isMatch = jsonValue == dbValue;
                if (isMatch) matchedFields++;
                else mismatches.Add($"登録地域: JSON=\"{jsonValue}\" DB=\"{dbValue ?? "null"}\"");

                comparisons.Add(new { Field = "登録地域", JSON = jsonValue, Database = dbValue, Match = isMatch ? "○" : "×" });
            }

            if (root.TryGetProperty("license_plate_classification", out var jsonClassification))
            {
                checkedFields++;
                var jsonValue = jsonClassification.GetString();
                var dbValue = vehicle.LicensePlateClassification;
                var isMatch = jsonValue == dbValue;
                if (isMatch) matchedFields++;
                else mismatches.Add($"分類番号: JSON=\"{jsonValue}\" DB=\"{dbValue ?? "null"}\"");

                comparisons.Add(new { Field = "分類番号", JSON = jsonValue, Database = dbValue, Match = isMatch ? "○" : "×" });
            }

            if (root.TryGetProperty("license_plate_hiragana", out var jsonHiragana))
            {
                checkedFields++;
                var jsonValue = jsonHiragana.GetString();
                var dbValue = vehicle.LicensePlateHiragana;
                var isMatch = jsonValue == dbValue;
                if (isMatch) matchedFields++;
                else mismatches.Add($"ひらがな: JSON=\"{jsonValue}\" DB=\"{dbValue ?? "null"}\"");

                comparisons.Add(new { Field = "ひらがな", JSON = jsonValue, Database = dbValue, Match = isMatch ? "○" : "×" });
            }

            if (root.TryGetProperty("license_plate_number", out var jsonNumber))
            {
                checkedFields++;
                var jsonValue = jsonNumber.GetString();
                var dbValue = vehicle.LicensePlateNumber;
                var isMatch = jsonValue == dbValue;
                if (isMatch) matchedFields++;
                else mismatches.Add($"ナンバー: JSON=\"{jsonValue}\" DB=\"{dbValue ?? "null"}\"");

                comparisons.Add(new { Field = "ナンバー", JSON = jsonValue, Database = dbValue, Match = isMatch ? "○" : "×" });
            }

            // 車両諸元
            if (root.TryGetProperty("purpose", out var jsonPurpose))
            {
                checkedFields++;
                var jsonValue = jsonPurpose.GetString();
                var dbValue = vehicle.Purpose;
                var isMatch = jsonValue == dbValue;
                if (isMatch) matchedFields++;
                else mismatches.Add($"用途: JSON=\"{jsonValue}\" DB=\"{dbValue ?? "null"}\"");

                comparisons.Add(new { Field = "用途", JSON = jsonValue, Database = dbValue, Match = isMatch ? "○" : "×" });
            }

            if (root.TryGetProperty("personal_business_use", out var jsonPersonalUse))
            {
                checkedFields++;
                var jsonValue = jsonPersonalUse.GetString();
                var dbValue = vehicle.PersonalBusinessUse;
                var isMatch = jsonValue == dbValue;
                if (isMatch) matchedFields++;
                else mismatches.Add($"自家用・事業用: JSON=\"{jsonValue}\" DB=\"{dbValue ?? "null"}\"");

                comparisons.Add(new { Field = "自家用・事業用", JSON = jsonValue, Database = dbValue, Match = isMatch ? "○" : "×" });
            }

            if (root.TryGetProperty("body_shape", out var jsonBodyShape))
            {
                checkedFields++;
                var jsonValue = jsonBodyShape.GetString();
                var dbValue = vehicle.BodyShape;
                var isMatch = jsonValue == dbValue;
                if (isMatch) matchedFields++;
                else mismatches.Add($"車体の形状: JSON=\"{jsonValue}\" DB=\"{dbValue ?? "null"}\"");

                comparisons.Add(new { Field = "車体の形状", JSON = jsonValue, Database = dbValue, Match = isMatch ? "○" : "×" });
            }

            // 数値フィールドの検証
            if (root.TryGetProperty("seating_capacity", out var jsonSeating))
            {
                checkedFields++;
                var jsonValue = jsonSeating.ValueKind == JsonValueKind.Number ? jsonSeating.GetInt32().ToString() : jsonSeating.GetString();
                var dbValue = vehicle.SeatingCapacity?.ToString();
                var isMatch = jsonValue == dbValue || (int.TryParse(jsonValue, out var jv) && jv == vehicle.SeatingCapacity);
                if (isMatch) matchedFields++;
                else mismatches.Add($"乗車定員: JSON=\"{jsonValue}\" DB=\"{dbValue ?? "null"}\"");

                comparisons.Add(new { Field = "乗車定員", JSON = jsonValue, Database = dbValue, Match = isMatch ? "○" : "×" });
            }

            if (root.TryGetProperty("max_load_capacity", out var jsonMaxLoad))
            {
                checkedFields++;
                var jsonValue = jsonMaxLoad.ValueKind == JsonValueKind.Number ? jsonMaxLoad.GetInt32().ToString() : jsonMaxLoad.GetString();
                var dbValue = vehicle.MaxLoadCapacity?.ToString();
                var isMatch = jsonValue == dbValue || (int.TryParse(jsonValue, out var jv) && jv == vehicle.MaxLoadCapacity);
                if (isMatch) matchedFields++;
                else mismatches.Add($"最大積載量: JSON=\"{jsonValue}\" DB=\"{dbValue ?? "null"}\"");

                comparisons.Add(new { Field = "最大積載量", JSON = jsonValue, Database = dbValue, Match = isMatch ? "○" : "×" });
            }

            if (root.TryGetProperty("vehicle_weight", out var jsonWeight))
            {
                checkedFields++;
                var jsonValue = jsonWeight.ValueKind == JsonValueKind.Number ? jsonWeight.GetInt32().ToString() : jsonWeight.GetString();
                var dbValue = vehicle.VehicleWeight?.ToString();
                var isMatch = jsonValue == dbValue || (int.TryParse(jsonValue, out var jv) && jv == vehicle.VehicleWeight);
                if (isMatch) matchedFields++;
                else mismatches.Add($"車両重量: JSON=\"{jsonValue}\" DB=\"{dbValue ?? "null"}\"");

                comparisons.Add(new { Field = "車両重量", JSON = jsonValue, Database = dbValue, Match = isMatch ? "○" : "×" });
            }

            if (root.TryGetProperty("vehicle_total_weight", out var jsonTotalWeight))
            {
                checkedFields++;
                var jsonValue = jsonTotalWeight.ValueKind == JsonValueKind.Number ? jsonTotalWeight.GetInt32().ToString() : jsonTotalWeight.GetString();
                var dbValue = vehicle.VehicleTotalWeight?.ToString();
                var isMatch = jsonValue == dbValue || (int.TryParse(jsonValue, out var jv) && jv == vehicle.VehicleTotalWeight);
                if (isMatch) matchedFields++;
                else mismatches.Add($"車両総重量: JSON=\"{jsonValue}\" DB=\"{dbValue ?? "null"}\"");

                comparisons.Add(new { Field = "車両総重量", JSON = jsonValue, Database = dbValue, Match = isMatch ? "○" : "×" });
            }

            // 寸法
            if (root.TryGetProperty("vehicle_length", out var jsonLength))
            {
                checkedFields++;
                var jsonValue = jsonLength.ValueKind == JsonValueKind.Number ? jsonLength.GetInt32().ToString() : jsonLength.GetString();
                var dbValue = vehicle.VehicleLength?.ToString();
                var isMatch = jsonValue == dbValue || (int.TryParse(jsonValue, out var jv) && jv == vehicle.VehicleLength);
                if (isMatch) matchedFields++;
                else mismatches.Add($"長さ: JSON=\"{jsonValue}\" DB=\"{dbValue ?? "null"}\"");

                comparisons.Add(new { Field = "長さ", JSON = jsonValue, Database = dbValue, Match = isMatch ? "○" : "×" });
            }

            if (root.TryGetProperty("vehicle_width", out var jsonWidth))
            {
                checkedFields++;
                var jsonValue = jsonWidth.ValueKind == JsonValueKind.Number ? jsonWidth.GetInt32().ToString() : jsonWidth.GetString();
                var dbValue = vehicle.VehicleWidth?.ToString();
                var isMatch = jsonValue == dbValue || (int.TryParse(jsonValue, out var jv) && jv == vehicle.VehicleWidth);
                if (isMatch) matchedFields++;
                else mismatches.Add($"幅: JSON=\"{jsonValue}\" DB=\"{dbValue ?? "null"}\"");

                comparisons.Add(new { Field = "幅", JSON = jsonValue, Database = dbValue, Match = isMatch ? "○" : "×" });
            }

            if (root.TryGetProperty("vehicle_height", out var jsonHeight))
            {
                checkedFields++;
                var jsonValue = jsonHeight.ValueKind == JsonValueKind.Number ? jsonHeight.GetInt32().ToString() : jsonHeight.GetString();
                var dbValue = vehicle.VehicleHeight?.ToString();
                var isMatch = jsonValue == dbValue || (int.TryParse(jsonValue, out var jv) && jv == vehicle.VehicleHeight);
                if (isMatch) matchedFields++;
                else mismatches.Add($"高さ: JSON=\"{jsonValue}\" DB=\"{dbValue ?? "null"}\"");

                comparisons.Add(new { Field = "高さ", JSON = jsonValue, Database = dbValue, Match = isMatch ? "○" : "×" });
            }

            // エンジン関連
            if (root.TryGetProperty("displacement", out var jsonDisplacement))
            {
                checkedFields++;
                decimal? jsonDecimalValue = null;
                if (jsonDisplacement.ValueKind == JsonValueKind.Number)
                {
                    jsonDecimalValue = jsonDisplacement.GetDecimal() / 1000m; // ccからLに変換
                }
                else if (decimal.TryParse(jsonDisplacement.GetString(), out var parsed))
                {
                    jsonDecimalValue = parsed / 1000m;
                }

                var isMatch = jsonDecimalValue.HasValue && Math.Abs((jsonDecimalValue.Value - (vehicle.Displacement ?? 0))) < 0.01m;
                if (isMatch) matchedFields++;
                else mismatches.Add($"排気量: JSON=\"{jsonDecimalValue?.ToString("F2")}L\" DB=\"{vehicle.Displacement?.ToString("F2") ?? "null"}L\"");

                comparisons.Add(new { Field = "排気量", JSON = $"{jsonDecimalValue?.ToString("F2")}L", Database = $"{vehicle.Displacement?.ToString("F2")}L", Match = isMatch ? "○" : "×" });
            }

            if (root.TryGetProperty("fuel_type", out var jsonFuel))
            {
                checkedFields++;
                var jsonValue = jsonFuel.GetString();
                var dbValue = vehicle.FuelType;
                var isMatch = jsonValue == dbValue;
                if (isMatch) matchedFields++;
                else mismatches.Add($"燃料の種類: JSON=\"{jsonValue}\" DB=\"{dbValue ?? "null"}\"");

                comparisons.Add(new { Field = "燃料の種類", JSON = jsonValue, Database = dbValue, Match = isMatch ? "○" : "×" });
            }

            // 使用者情報
            if (root.TryGetProperty("user_name_or_company", out var jsonUserName))
            {
                checkedFields++;
                var jsonValue = jsonUserName.GetString();
                var dbValue = vehicle.UserNameOrCompany;
                var isMatch = jsonValue == dbValue;
                if (isMatch) matchedFields++;
                else mismatches.Add($"使用者: JSON=\"{jsonValue}\" DB=\"{dbValue ?? "null"}\"");

                comparisons.Add(new { Field = "使用者", JSON = jsonValue, Database = dbValue, Match = isMatch ? "○" : "×" });
            }

            if (root.TryGetProperty("user_address", out var jsonUserAddress))
            {
                checkedFields++;
                var jsonValue = jsonUserAddress.GetString();
                var dbValue = vehicle.UserAddress;
                var isMatch = jsonValue == dbValue;
                if (isMatch) matchedFields++;
                else mismatches.Add($"使用者住所: JSON=\"{jsonValue}\" DB=\"{dbValue ?? "null"}\"");

                comparisons.Add(new { Field = "使用者住所", JSON = jsonValue, Database = dbValue, Match = isMatch ? "○" : "×" });
            }

            if (root.TryGetProperty("base_location", out var jsonBaseLocation))
            {
                checkedFields++;
                var jsonValue = jsonBaseLocation.GetString();
                var dbValue = vehicle.BaseLocation;
                var isMatch = jsonValue == dbValue;
                if (isMatch) matchedFields++;
                else mismatches.Add($"使用の本拠: JSON=\"{jsonValue}\" DB=\"{dbValue ?? "null"}\"");

                comparisons.Add(new { Field = "使用の本拠", JSON = jsonValue, Database = dbValue, Match = isMatch ? "○" : "×" });
            }

            // 日付フィールド
            if (root.TryGetProperty("first_registration_date", out var jsonFirstReg))
            {
                checkedFields++;
                var jsonValue = jsonFirstReg.GetString();
                DateTime? jsonDate = null;
                if (!string.IsNullOrEmpty(jsonValue))
                {
                    DateTime.TryParse(jsonValue, out var parsed);
                    jsonDate = parsed;
                }

                var isMatch = jsonDate == vehicle.FirstRegistrationDate ||
                            (jsonDate?.Date == vehicle.FirstRegistrationDate?.Date);
                if (isMatch) matchedFields++;
                else mismatches.Add($"初度登録年月: JSON=\"{jsonDate?.ToString("yyyy-MM-dd")}\" DB=\"{vehicle.FirstRegistrationDate?.ToString("yyyy-MM-dd") ?? "null"}\"");

                comparisons.Add(new { Field = "初度登録年月", JSON = jsonDate?.ToString("yyyy-MM-dd"), Database = vehicle.FirstRegistrationDate?.ToString("yyyy-MM-dd"), Match = isMatch ? "○" : "×" });
            }

            if (root.TryGetProperty("inspection_expiry_date", out var jsonInspection))
            {
                checkedFields++;
                var jsonValue = jsonInspection.GetString();
                DateTime? jsonDate = null;
                if (!string.IsNullOrEmpty(jsonValue))
                {
                    DateTime.TryParse(jsonValue, out var parsed);
                    jsonDate = parsed;
                }

                var isMatch = jsonDate == vehicle.InspectionExpiryDate ||
                            (jsonDate?.Date == vehicle.InspectionExpiryDate?.Date);
                if (isMatch) matchedFields++;
                else mismatches.Add($"車検満了日: JSON=\"{jsonDate?.ToString("yyyy-MM-dd")}\" DB=\"{vehicle.InspectionExpiryDate?.ToString("yyyy-MM-dd") ?? "null"}\"");

                comparisons.Add(new { Field = "車検満了日", JSON = jsonDate?.ToString("yyyy-MM-dd"), Database = vehicle.InspectionExpiryDate?.ToString("yyyy-MM-dd"), Match = isMatch ? "○" : "×" });
            }

            // 結果をコンソールに出力
            await JSRuntime.InvokeVoidAsync("console.table", comparisons);

            // 検証結果のサマリ
            await JSRuntime.InvokeVoidAsync("console.log", $"=== 検証結果 ===");
            await JSRuntime.InvokeVoidAsync("console.log", $"チェックしたフィールド数: {checkedFields}");
            await JSRuntime.InvokeVoidAsync("console.log", $"一致したフィールド数: {matchedFields}");
            await JSRuntime.InvokeVoidAsync("console.log", $"不一致フィールド数: {checkedFields - matchedFields}");

            // 全フィールドが一致した場合のみTrueを設定
            if (checkedFields > 0 && matchedFields == checkedFields)
            {
                allFieldsMatch = true;
                await JSRuntime.InvokeVoidAsync("console.log", "✅ ベリファイ成功: 全フィールドが一致しました");
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("console.error", "❌ ベリファイ失敗: 不一致のフィールドがあります");
                foreach (var mismatch in mismatches)
                {
                    await JSRuntime.InvokeVoidAsync("console.error", $"  - {mismatch}");
                }
            }

            // 結果を返す
            if (allFieldsMatch)
            {
                return (true, string.Empty);
            }
            else
            {
                var errorMessage = $"データ検証失敗: {checkedFields}項目中{checkedFields - matchedFields}項目が不一致\n" + string.Join("\n", mismatches.Take(5));
                if (mismatches.Count > 5)
                {
                    errorMessage += $"\n...他{mismatches.Count - 5}項目";
                }
                return (false, errorMessage);
            }
        }
    }
}