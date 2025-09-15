using Microsoft.AspNetCore.Components;
using AutoDealerSphere.Shared.Models;
using Syncfusion.Blazor.Popups;
using System.Net.Http.Json;

namespace AutoDealerSphere.Client.Components
{
    public partial class InvoiceBasicInfoDialog
    {
        [Inject] private HttpClient Http { get; set; } = default!;
        [Parameter] public EventCallback<Invoice> OnSave { get; set; }

        private SfDialog? _dialog;
        private bool _isVisible = false;
        private bool _isNew = false;
        private bool _isAddMode = false; // 請求書追加モード
        private Invoice? _model;
        private List<AutoDealerSphere.Shared.Models.Client> _clients = new();
        private List<Vehicle> _vehicles = new();

        public async Task Open(Invoice invoice, bool isAddMode = false)
        {
            _isNew = invoice.Id == 0;
            _isAddMode = isAddMode; // 請求書追加モードを設定
            _model = new Invoice
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                Subnumber = invoice.Subnumber,
                ClientId = invoice.ClientId,
                VehicleId = invoice.VehicleId,
                InvoiceDate = invoice.InvoiceDate,
                WorkCompletedDate = invoice.WorkCompletedDate,
                NextInspectionDate = invoice.NextInspectionDate,
                Mileage = invoice.Mileage,
                Notes = invoice.Notes,
                TaxRate = invoice.TaxRate,
                InvoiceDetails = invoice.InvoiceDetails
            };

            // 顧客リストを取得
            var clients = await Http.GetFromJsonAsync<AutoDealerSphere.Shared.Models.Client[]>("api/Client");
            _clients = clients?.ToList() ?? new List<AutoDealerSphere.Shared.Models.Client>();

            // 既存データの場合、車両リストも取得
            if (_model.ClientId > 0)
            {
                await LoadVehicles(_model.ClientId);
            }

            _isVisible = true;
            StateHasChanged();
        }

        private async Task OnClientChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<int, AutoDealerSphere.Shared.Models.Client> args)
        {
            if (args.Value > 0)
            {
                await LoadVehicles(args.Value);
                _model!.VehicleId = null;
            }
        }

        private async Task LoadVehicles(int clientId)
        {
            var vehicles = await Http.GetFromJsonAsync<Vehicle[]>($"api/Vehicles/byClient/{clientId}");
            _vehicles = vehicles?.ToList() ?? new List<Vehicle>();
        }

        private async Task OnValidSubmit()
        {
            if (_model != null)
            {
                await OnSave.InvokeAsync(_model);
                _isVisible = false;
            }
        }

        private void Cancel()
        {
            _isVisible = false;
        }

        private string GetInvoiceNumberDisplay()
        {
            if (_model == null) return string.Empty;
            if (string.IsNullOrEmpty(_model.InvoiceNumber)) return "自動生成";
            if (_model.Subnumber > 1)
                return $"{_model.InvoiceNumber}-{_model.Subnumber}";
            return _model.InvoiceNumber;
        }
    }
}