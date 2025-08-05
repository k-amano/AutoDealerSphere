using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Components;
using Syncfusion.Blazor.Popups;
using System.Net.Http.Json;

namespace AutoDealerSphere.Client.Components
{
    public partial class StatutoryFeeDialog
    {
        [Inject]
        private HttpClient Http { get; set; } = default!;

        [Parameter]
        public EventCallback<StatutoryFee> OnSave { get; set; }

        private SfDialog? _dialog;
        private bool _isVisible = false;
        private bool _isLoading = false;
        private int _invoiceId;
        private int _vehicleId;
        private Vehicle? _vehicle;
        private List<StatutoryFee> _statutoryFees = new();
        private int _selectedFeeId = 0;
        private string _selectedFeeIdString = "";
        private string _vehicleDisplay = "";
        private string _vehicleCategoryDisplay = "";

        public async Task Open(int invoiceId, int vehicleId)
        {
            _invoiceId = invoiceId;
            _vehicleId = vehicleId;
            _selectedFeeId = 0;
            _selectedFeeIdString = "";
            _isVisible = true;
            _isLoading = true;
            StateHasChanged();

            await LoadVehicleAndFees();
        }

        private async Task LoadVehicleAndFees()
        {
            try
            {
                // 車両情報を取得
                _vehicle = await Http.GetFromJsonAsync<Vehicle>($"api/Vehicles/{_vehicleId}");
                
                if (_vehicle != null)
                {
                    _vehicleDisplay = $"{_vehicle.VehicleName} ({_vehicle.LicensePlateNumber})";
                    _vehicleCategoryDisplay = _vehicle.VehicleCategory?.CategoryName ?? "未設定";

                    // 車両カテゴリに対応する法定費用を取得
                    if (_vehicle.VehicleCategoryId > 0)
                    {
                        _statutoryFees = await Http.GetFromJsonAsync<List<StatutoryFee>>($"api/StatutoryFees/category/{_vehicle.VehicleCategoryId}") ?? new();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading vehicle and fees: {ex.Message}");
                _statutoryFees = new();
            }
            finally
            {
                _isLoading = false;
                StateHasChanged();
            }
        }

        private void OnFeeSelected(int feeId)
        {
            _selectedFeeId = feeId;
            _selectedFeeIdString = feeId.ToString();
        }

        private async Task SaveSelectedFee()
        {
            if (_selectedFeeId > 0)
            {
                var selectedFee = _statutoryFees.FirstOrDefault(f => f.Id == _selectedFeeId);
                if (selectedFee != null)
                {
                    await OnSave.InvokeAsync(selectedFee);
                    _isVisible = false;
                }
            }
        }

        private void Cancel()
        {
            _isVisible = false;
        }
    }
}