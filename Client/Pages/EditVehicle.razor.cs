using Microsoft.AspNetCore.Components;
using AutoDealerSphere.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Components.Forms;
using System.IO;

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

        private void OnFileSelected(InputFileChangeEventArgs e)
        {
            _selectedFile = e.File;
            _importMessage = string.Empty;
        }

        private async Task ImportJsonAsync()
        {
            if (_selectedFile == null || _item == null || _item.Id == 0)
            {
                _importMessage = "ファイルが選択されていないか、車両が保存されていません。";
                _importSuccess = false;
                return;
            }

            try
            {
                // JSONファイルを読み込む
                using var stream = _selectedFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10MB max
                using var reader = new StreamReader(stream);
                var jsonContent = await reader.ReadToEndAsync();

                // サーバーにJSONデータを送信
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await Http.PostAsync($"api/vehicles/{_item.Id}/import-json", content);

                if (response.IsSuccessStatusCode)
                {
                    _importMessage = "JSONデータの取り込みに成功しました。";
                    _importSuccess = true;

                    // 更新されたデータを再読み込みして画面を更新
                    await ReloadVehicleData();
                    StateHasChanged();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _importMessage = $"取り込みエラー: {response.StatusCode} - {errorContent}";
                    _importSuccess = false;
                }
            }
            catch (Exception ex)
            {
                _importMessage = $"エラーが発生しました: {ex.Message}";
                _importSuccess = false;
            }
        }

        private async Task ReloadVehicleData()
        {
            if (_item != null && _item.Id > 0)
            {
                var response = await Http.GetAsync($"api/vehicles/{_item.Id}");
                if (response.IsSuccessStatusCode)
                {
                    var vehicle = await response.Content.ReadFromJsonAsync<AutoDealerSphere.Shared.Models.Vehicle>();
                    if (vehicle != null)
                    {
                        // 既存のオブジェクトのプロパティを更新
                        _item.ClientId = vehicle.ClientId;
                        _item.Client = vehicle.Client;
                        _item.LicensePlateLocation = vehicle.LicensePlateLocation;
                        _item.LicensePlateClassification = vehicle.LicensePlateClassification;
                        _item.LicensePlateHiragana = vehicle.LicensePlateHiragana;
                        _item.LicensePlateNumber = vehicle.LicensePlateNumber;
                        _item.KeyNumber = vehicle.KeyNumber;
                        _item.ChassisNumber = vehicle.ChassisNumber;
                        _item.TypeCertificationNumber = vehicle.TypeCertificationNumber;
                        _item.CategoryNumber = vehicle.CategoryNumber;
                        _item.VehicleName = vehicle.VehicleName;
                        _item.VehicleModel = vehicle.VehicleModel;
                        _item.Mileage = vehicle.Mileage;
                        _item.FirstRegistrationDate = vehicle.FirstRegistrationDate;
                        _item.Purpose = vehicle.Purpose;
                        _item.PersonalBusinessUse = vehicle.PersonalBusinessUse;
                        _item.BodyShape = vehicle.BodyShape;
                        _item.SeatingCapacity = vehicle.SeatingCapacity;
                        _item.MaxLoadCapacity = vehicle.MaxLoadCapacity;
                        _item.VehicleWeight = vehicle.VehicleWeight;
                        _item.VehicleTotalWeight = vehicle.VehicleTotalWeight;
                        _item.VehicleLength = vehicle.VehicleLength;
                        _item.VehicleWidth = vehicle.VehicleWidth;
                        _item.VehicleHeight = vehicle.VehicleHeight;
                        _item.FrontOverhang = vehicle.FrontOverhang;
                        _item.RearOverhang = vehicle.RearOverhang;
                        _item.ModelCode = vehicle.ModelCode;
                        _item.EngineModel = vehicle.EngineModel;
                        _item.Displacement = vehicle.Displacement;
                        _item.FuelType = vehicle.FuelType;
                        _item.InspectionExpiryDate = vehicle.InspectionExpiryDate;
                        _item.InspectionCertificateNumber = vehicle.InspectionCertificateNumber;
                        _item.UserNameOrCompany = vehicle.UserNameOrCompany;
                        _item.UserAddress = vehicle.UserAddress;
                        _item.BaseLocation = vehicle.BaseLocation;
                        _item.VehicleCategoryId = vehicle.VehicleCategoryId;
                        _item.UpdatedAt = vehicle.UpdatedAt;
                    }
                }
            }
        }
    }
}