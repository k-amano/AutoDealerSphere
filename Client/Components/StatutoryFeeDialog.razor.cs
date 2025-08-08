using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Components;
using Syncfusion.Blazor.Popups;
using System.Net.Http.Json;
using System.Linq;

namespace AutoDealerSphere.Client.Components
{
    public partial class StatutoryFeeDialog
    {
        [Inject]
        private HttpClient Http { get; set; } = default!;

        [Parameter]
        public EventCallback<List<StatutoryFee>> OnSave { get; set; }

        private SfDialog? _dialog;
        private bool _isVisible = false;
        private bool _isLoading = false;
        private int _invoiceId;
        private int _vehicleId;
        private Vehicle? _vehicle;
        private List<StatutoryFee> _statutoryFees = new();
        private List<int> _selectedFeeIds = new();
        private List<InvoiceDetail> _existingStatutoryFeeDetails = new();
        private string _vehicleDisplay = "";
        private string _vehicleCategoryDisplay = "";

        public async Task Open(int invoiceId, int vehicleId)
        {
            _invoiceId = invoiceId;
            _vehicleId = vehicleId;
            _selectedFeeIds.Clear();
            _isVisible = true;
            _isLoading = true;
            StateHasChanged();

            await LoadVehicleAndFees();
            await LoadExistingStatutoryFees();
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
                    
                    // 車両カテゴリ情報を取得
                    if (_vehicle.VehicleCategoryId > 0 && _vehicle.VehicleCategory == null)
                    {
                        var categoryResponse = await Http.GetFromJsonAsync<VehicleCategory>($"api/VehicleCategories/{_vehicle.VehicleCategoryId}");
                        if (categoryResponse != null)
                        {
                            _vehicle.VehicleCategory = categoryResponse;
                        }
                    }
                    
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

        private async Task LoadExistingStatutoryFees()
        {
            try
            {
                // 請求書の既存明細を取得
                var invoice = await Http.GetFromJsonAsync<Invoice>($"api/Invoices/{_invoiceId}");
                if (invoice?.InvoiceDetails != null)
                {
                    // 法定費用タイプの明細を抽出
                    _existingStatutoryFeeDetails = invoice.InvoiceDetails
                        .Where(d => d.Type == "法定費用")
                        .ToList();
                    
                    // 既存の法定費用項目名から、選択済みとしてマーク
                    foreach (var detail in _existingStatutoryFeeDetails)
                    {
                        var matchingFee = _statutoryFees.FirstOrDefault(f => f.FeeType == detail.ItemName);
                        if (matchingFee != null)
                        {
                            _selectedFeeIds.Add(matchingFee.Id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading existing statutory fees: {ex.Message}");
            }
        }

        private void OnFeeSelectionChanged(int feeId, bool isChecked)
        {
            if (isChecked)
            {
                if (!_selectedFeeIds.Contains(feeId))
                {
                    _selectedFeeIds.Add(feeId);
                }
            }
            else
            {
                _selectedFeeIds.Remove(feeId);
            }
        }

        private async Task SaveSelectedFees()
        {
            var selectedFees = _statutoryFees
                .Where(f => _selectedFeeIds.Contains(f.Id))
                .ToList();
                
            await OnSave.InvokeAsync(selectedFees);
            _isVisible = false;
        }

        private void Cancel()
        {
            _isVisible = false;
        }
    }
}