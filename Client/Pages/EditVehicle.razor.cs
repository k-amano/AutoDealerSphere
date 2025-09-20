using Microsoft.AspNetCore.Components;
using AutoDealerSphere.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Components.Forms;

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

        private async Task ImportJsonData()
        {
            if (_selectedFile == null || _item == null)
            {
                _importMessage = "ファイルが選択されていません。";
                _importSuccess = false;
                return;
            }

            try
            {
                var maxFileSize = 10 * 1024 * 1024; // 10MB
                using var stream = _selectedFile.OpenReadStream(maxFileSize);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var jsonContent = Encoding.UTF8.GetString(memoryStream.ToArray());

                // Send JSON to backend for processing
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await Http.PostAsync($"api/vehicles/{VehicleId}/import-json", content);

                if (response.IsSuccessStatusCode)
                {
                    // Reload vehicle data to show imported values
                    var updatedVehicle = await response.Content.ReadFromJsonAsync<AutoDealerSphere.Shared.Models.Vehicle>();
                    if (updatedVehicle != null)
                    {
                        // Update current item with imported data
                        _item.VehicleName = updatedVehicle.VehicleName;
                        _item.VehicleModel = updatedVehicle.VehicleModel;
                        _item.ChassisNumber = updatedVehicle.ChassisNumber;
                        _item.TypeCertificationNumber = updatedVehicle.TypeCertificationNumber;
                        _item.CategoryNumber = updatedVehicle.CategoryNumber;
                        _item.FirstRegistrationDate = updatedVehicle.FirstRegistrationDate;
                        _item.InspectionExpiryDate = updatedVehicle.InspectionExpiryDate;
                        _item.InspectionCertificateNumber = updatedVehicle.InspectionCertificateNumber;
                        _item.Purpose = updatedVehicle.Purpose;
                        _item.PersonalBusinessUse = updatedVehicle.PersonalBusinessUse;
                        _item.BodyShape = updatedVehicle.BodyShape;
                        _item.FuelType = updatedVehicle.FuelType;
                        _item.Displacement = updatedVehicle.Displacement;
                        _item.EngineModel = updatedVehicle.EngineModel;
                        _item.ModelCode = updatedVehicle.ModelCode;
                        _item.VehicleWeight = updatedVehicle.VehicleWeight;
                        _item.VehicleTotalWeight = updatedVehicle.VehicleTotalWeight;
                        _item.VehicleLength = updatedVehicle.VehicleLength;
                        _item.VehicleWidth = updatedVehicle.VehicleWidth;
                        _item.VehicleHeight = updatedVehicle.VehicleHeight;
                        _item.FrontOverhang = updatedVehicle.FrontOverhang;
                        _item.RearOverhang = updatedVehicle.RearOverhang;
                        _item.PassengerCapacity = updatedVehicle.PassengerCapacity;
                        _item.MaxLoadCapacity = updatedVehicle.MaxLoadCapacity;
                        _item.FrontAxleWeight = updatedVehicle.FrontAxleWeight;
                        _item.RearAxleWeight = updatedVehicle.RearAxleWeight;
                        _item.RatedOutput = updatedVehicle.RatedOutput;
                        _item.BodyColor = updatedVehicle.BodyColor;
                        _item.UserNameOrCompany = updatedVehicle.UserNameOrCompany;
                        _item.UserAddress = updatedVehicle.UserAddress;
                        _item.UserPostalCode = updatedVehicle.UserPostalCode;
                        _item.OwnerNameOrCompany = updatedVehicle.OwnerNameOrCompany;
                        _item.OwnerAddress = updatedVehicle.OwnerAddress;
                        _item.OwnerPostalCode = updatedVehicle.OwnerPostalCode;
                        _item.BaseLocation = updatedVehicle.BaseLocation;
                        _item.RegistrationDate = updatedVehicle.RegistrationDate;
                        _item.ManufactureDate = updatedVehicle.ManufactureDate;
                        _item.InspectionType = updatedVehicle.InspectionType;
                        _item.InspectionDate = updatedVehicle.InspectionDate;
                        _item.IssueDate = updatedVehicle.IssueDate;
                        _item.IssueOffice = updatedVehicle.IssueOffice;
                        _item.CertificateVersion = updatedVehicle.CertificateVersion;
                        _item.ImportSource = "JSON";
                        _item.ImportDate = DateTime.Now;
                        _item.OriginalData = jsonContent;

                        _importMessage = "車検証データの取り込みが完了しました。";
                        _importSuccess = true;
                        StateHasChanged();
                    }
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    _importMessage = $"取り込みエラー: {errorMessage}";
                    _importSuccess = false;
                }
            }
            catch (Exception ex)
            {
                _importMessage = $"ファイル読み込みエラー: {ex.Message}";
                _importSuccess = false;
            }
        }
    }
}