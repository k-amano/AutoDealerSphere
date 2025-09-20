using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using AutoDealerSphere.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using System.Text.Json.Serialization;

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

        // JSON Import Properties
        private IBrowserFile? selectedJsonFile;
        private string selectedJsonFileName = string.Empty;
        private bool isImporting = false;
        private string importMessage = string.Empty;
        private bool importSuccess = false;

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

        private void OnJsonFileSelected(InputFileChangeEventArgs e)
        {
            selectedJsonFile = e.File;
            selectedJsonFileName = e.File.Name;
            importMessage = string.Empty;
        }

        private async Task ImportJsonData()
        {
            if (selectedJsonFile == null || _item == null) return;

            isImporting = true;
            importMessage = string.Empty;
            StateHasChanged();

            try
            {
                // Read JSON file content
                using var stream = selectedJsonFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10MB max
                using var reader = new StreamReader(stream);
                var jsonContent = await reader.ReadToEndAsync();

                // Send to server for import
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await Http.PostAsync($"api/vehicles/{VehicleId}/import-json", content);

                if (response.IsSuccessStatusCode)
                {
                    // Verify data was actually saved by reloading
                    var verifyResponse = await Http.GetAsync($"api/vehicles/{VehicleId}");
                    if (verifyResponse.IsSuccessStatusCode)
                    {
                        var updatedVehicle = await verifyResponse.Content.ReadFromJsonAsync<AutoDealerSphere.Shared.Models.Vehicle>();
                        if (updatedVehicle != null)
                        {
                            // Update local data with the verified data from database
                            await ReloadVehicleData(updatedVehicle);
                            importSuccess = true;
                            importMessage = "車検証データの取り込みに成功しました。データベースへの保存を確認しました。";
                        }
                        else
                        {
                            importSuccess = false;
                            importMessage = "エラー: データベースから更新されたデータを取得できませんでした。";
                        }
                    }
                    else
                    {
                        importSuccess = false;
                        importMessage = "エラー: データベースの検証に失敗しました。";
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    importSuccess = false;

                    // Try to parse error response
                    try
                    {
                        var errorObj = JsonSerializer.Deserialize<JsonElement>(errorContent);
                        if (errorObj.TryGetProperty("error", out var errorProp))
                        {
                            importMessage = $"エラー: {errorProp.GetString()}";
                        }
                        else
                        {
                            importMessage = $"エラー: {errorContent}";
                        }
                    }
                    catch
                    {
                        importMessage = $"エラー: サーバーエラー ({response.StatusCode})";
                    }
                }
            }
            catch (Exception ex)
            {
                importSuccess = false;
                importMessage = $"エラー: {ex.Message}";
            }
            finally
            {
                isImporting = false;
                StateHasChanged();
            }
        }

        private async Task ReloadVehicleData(AutoDealerSphere.Shared.Models.Vehicle updatedVehicle)
        {
            // Update all properties with the verified data from database
            _item!.ClientId = updatedVehicle.ClientId;
            _item.LicensePlateLocation = updatedVehicle.LicensePlateLocation;
            _item.LicensePlateClassification = updatedVehicle.LicensePlateClassification;
            _item.LicensePlateHiragana = updatedVehicle.LicensePlateHiragana;
            _item.LicensePlateNumber = updatedVehicle.LicensePlateNumber;
            _item.KeyNumber = updatedVehicle.KeyNumber;
            _item.ChassisNumber = updatedVehicle.ChassisNumber;
            _item.TypeCertificationNumber = updatedVehicle.TypeCertificationNumber;
            _item.CategoryNumber = updatedVehicle.CategoryNumber;
            _item.VehicleName = updatedVehicle.VehicleName;
            _item.VehicleModel = updatedVehicle.VehicleModel;
            _item.Mileage = updatedVehicle.Mileage;
            _item.FirstRegistrationDate = updatedVehicle.FirstRegistrationDate;
            _item.Purpose = updatedVehicle.Purpose;
            _item.PersonalBusinessUse = updatedVehicle.PersonalBusinessUse;
            _item.BodyShape = updatedVehicle.BodyShape;
            _item.SeatingCapacity = updatedVehicle.SeatingCapacity;
            _item.MaxLoadCapacity = updatedVehicle.MaxLoadCapacity;
            _item.VehicleWeight = updatedVehicle.VehicleWeight;
            _item.VehicleTotalWeight = updatedVehicle.VehicleTotalWeight;
            _item.VehicleLength = updatedVehicle.VehicleLength;
            _item.VehicleWidth = updatedVehicle.VehicleWidth;
            _item.VehicleHeight = updatedVehicle.VehicleHeight;
            _item.FrontOverhang = updatedVehicle.FrontOverhang;
            _item.RearOverhang = updatedVehicle.RearOverhang;
            _item.ModelCode = updatedVehicle.ModelCode;
            _item.EngineModel = updatedVehicle.EngineModel;
            _item.Displacement = updatedVehicle.Displacement;
            _item.FuelType = updatedVehicle.FuelType;
            _item.InspectionExpiryDate = updatedVehicle.InspectionExpiryDate;
            _item.InspectionCertificateNumber = updatedVehicle.InspectionCertificateNumber;
            _item.UserNameOrCompany = updatedVehicle.UserNameOrCompany;
            _item.UserAddress = updatedVehicle.UserAddress;
            _item.BaseLocation = updatedVehicle.BaseLocation;
            _item.VehicleCategoryId = updatedVehicle.VehicleCategoryId;
            _item.UpdatedAt = updatedVehicle.UpdatedAt;

            StateHasChanged();
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
    }
}