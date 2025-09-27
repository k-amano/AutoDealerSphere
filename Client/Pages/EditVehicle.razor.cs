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
                    _importSuccess = true;

                    // データベースから最新データを再読み込み
                    await ReloadVehicleData();

                    // 再読み込み後のデータをコンソールに出力
                    await JSRuntime.InvokeVoidAsync("console.log", "=== データベースから再読み込みした車両データ ===");
                    await LogVehicleData(_item);

                    // JSONデータとDBデータの比較
                    await CompareJsonWithDatabase(jsonDoc, _item);

                    _importMessage = "車検証データの取り込みに成功しました。データベースへの保存を確認しました。";
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

        private async Task CompareJsonWithDatabase(JsonDocument jsonDoc, AutoDealerSphere.Shared.Models.Vehicle vehicle)
        {
            await JSRuntime.InvokeVoidAsync("console.log", "=== JSONとデータベースの比較 ===");

            var root = jsonDoc.RootElement;
            var comparisons = new List<object>();

            // 車検証番号の比較
            if (root.TryGetProperty("inspection_certificate_number", out var jsonCertNumber))
            {
                comparisons.Add(new
                {
                    Field = "車検証番号",
                    JSON = jsonCertNumber.GetString(),
                    Database = vehicle.InspectionCertificateNumber,
                    Match = jsonCertNumber.GetString() == vehicle.InspectionCertificateNumber
                });
            }

            // 車名の比較
            if (root.TryGetProperty("vehicle_name", out var jsonVehicleName))
            {
                comparisons.Add(new
                {
                    Field = "車名",
                    JSON = jsonVehicleName.GetString(),
                    Database = vehicle.VehicleName,
                    Match = jsonVehicleName.GetString() == vehicle.VehicleName
                });
            }

            // 型式の比較
            if (root.TryGetProperty("vehicle_model", out var jsonModel))
            {
                comparisons.Add(new
                {
                    Field = "型式",
                    JSON = jsonModel.GetString(),
                    Database = vehicle.VehicleModel,
                    Match = jsonModel.GetString() == vehicle.VehicleModel
                });
            }

            // 車台番号の比較
            if (root.TryGetProperty("chassis_number", out var jsonChassis))
            {
                comparisons.Add(new
                {
                    Field = "車台番号",
                    JSON = jsonChassis.GetString(),
                    Database = vehicle.ChassisNumber,
                    Match = jsonChassis.GetString() == vehicle.ChassisNumber
                });
            }

            await JSRuntime.InvokeVoidAsync("console.table", comparisons);

            // 不一致があるかチェック
            var mismatches = comparisons.Where(c => !(bool)c.GetType().GetProperty("Match").GetValue(c)).ToList();
            if (mismatches.Any())
            {
                await JSRuntime.InvokeVoidAsync("console.warn", $"⚠️ {mismatches.Count}個のフィールドが一致しません");
            }
            else if (comparisons.Any())
            {
                await JSRuntime.InvokeVoidAsync("console.log", "✅ すべての比較フィールドが一致しています");
            }
        }
    }
}