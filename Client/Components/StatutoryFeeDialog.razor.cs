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
        private Dictionary<int, bool> _selectedFees = new();
        private List<InvoiceDetail> _existingInvoiceDetails = new();
        private string _vehicleDisplay = "";
        private string _vehicleCategoryDisplay = "";

        public async Task Open(int invoiceId, int vehicleId)
        {
            _invoiceId = invoiceId;
            _vehicleId = vehicleId;
            _selectedFees.Clear();
            _isVisible = true;
            _isLoading = true;
            StateHasChanged();

            await LoadVehicleAndFees();
            await LoadExistingInvoiceDetails();
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
                        var fees = await Http.GetFromJsonAsync<List<StatutoryFee>>($"api/StatutoryFees/category/{_vehicle.VehicleCategoryId}") ?? new();

                        // ID順でソート
                        _statutoryFees = fees.OrderBy(f => f.Id).ToList();

                        // 各法定費用のチェックボックスを初期化
                        foreach (var fee in _statutoryFees)
                        {
                            _selectedFees[fee.Id] = false;
                        }
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

        private async Task LoadExistingInvoiceDetails()
        {
            try
            {
                if (_invoiceId > 0)
                {
                    var invoice = await Http.GetFromJsonAsync<Invoice>($"api/Invoices/{_invoiceId}");
                    if (invoice != null && invoice.InvoiceDetails != null)
                    {
                        _existingInvoiceDetails = invoice.InvoiceDetails.Where(d => d.Type == "法定費用").ToList();
                        
                        // 既存の法定費用項目にチェックを入れる
                        foreach (var detail in _existingInvoiceDetails)
                        {
                            var fee = _statutoryFees.FirstOrDefault(f => f.FeeType == detail.ItemName);
                            if (fee != null && _selectedFees.ContainsKey(fee.Id))
                            {
                                _selectedFees[fee.Id] = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading existing invoice details: {ex.Message}");
            }
            finally
            {
                StateHasChanged();
            }
        }

        private async Task SaveSelectedFees()
        {
            var selectedFeesList = _statutoryFees
                .Where(f => _selectedFees.ContainsKey(f.Id) && _selectedFees[f.Id])
                .ToList();

            await OnSave.InvokeAsync(selectedFeesList);
            _isVisible = false;
        }

        private void Cancel()
        {
            _isVisible = false;
        }
    }
}