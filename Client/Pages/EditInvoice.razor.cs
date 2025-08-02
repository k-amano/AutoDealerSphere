using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace AutoDealerSphere.Client.Pages
{
    public partial class EditInvoice
    {
        [Parameter]
        public int InvoiceId { get; set; }
        
        private Invoice? _invoice;
        private List<AutoDealerSphere.Shared.Models.Client> _clients = new();
        private List<Vehicle> _vehicles = new();
        private List<Part> _parts = new();
        private bool _initialized = false;

        protected override async Task OnInitializedAsync()
        {
            // 顧客リストを取得
            var clients = await Http.GetFromJsonAsync<AutoDealerSphere.Shared.Models.Client[]>("api/Client");
            _clients = clients?.ToList() ?? new List<AutoDealerSphere.Shared.Models.Client>();
            
            // 部品リストを取得
            var parts = await Http.GetFromJsonAsync<Part[]>("api/Parts");
            _parts = parts?.ToList() ?? new List<Part>();

            if (InvoiceId == 0)
            {
                // 新規作成
                _invoice = new Invoice
                {
                    InvoiceDate = DateTime.Today,
                    WorkCompletedDate = DateTime.Today,
                    TaxRate = 10m,
                    InvoiceDetails = new List<InvoiceDetail>()
                };
                
                // 請求書番号を取得
                var response = await Http.GetFromJsonAsync<InvoiceNumberResponse>("api/Invoices/new-number");
                if (response != null)
                {
                    _invoice.InvoiceNumber = response.InvoiceNumber;
                }
            }
            else
            {
                // 既存データの編集
                _invoice = await Http.GetFromJsonAsync<Invoice>($"/api/Invoices/{InvoiceId}");
                if (_invoice != null && _invoice.ClientId > 0)
                {
                    await LoadVehicles(_invoice.ClientId);
                }
            }
            
            _initialized = true;
        }

        private async Task OnClientChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<int, AutoDealerSphere.Shared.Models.Client> args)
        {
            if (args.Value > 0)
            {
                await LoadVehicles(args.Value);
                _invoice!.VehicleId = 0; // 車両選択をリセット
            }
        }

        private async Task LoadVehicles(int clientId)
        {
            var vehicles = await Http.GetFromJsonAsync<Vehicle[]>($"api/Vehicles/byClient/{clientId}");
            _vehicles = vehicles?.ToList() ?? new List<Vehicle>();
        }

        private void AddDetail()
        {
            _invoice!.InvoiceDetails.Add(new InvoiceDetail
            {
                Quantity = 1,
                IsTaxable = true,
                DisplayOrder = _invoice.InvoiceDetails.Count
            });
            CalculateTotals();
        }

        private void RemoveDetail(InvoiceDetail detail)
        {
            _invoice!.InvoiceDetails.Remove(detail);
            CalculateTotals();
        }

        private void CalculateTotals()
        {
            decimal taxableSubTotal = 0;
            decimal nonTaxableSubTotal = 0;

            foreach (var detail in _invoice!.InvoiceDetails)
            {
                var subTotal = detail.SubTotal;
                if (detail.IsTaxable)
                {
                    taxableSubTotal += subTotal;
                }
                else
                {
                    nonTaxableSubTotal += subTotal;
                }
            }

            _invoice.TaxableSubTotal = taxableSubTotal;
            _invoice.NonTaxableSubTotal = nonTaxableSubTotal;
            _invoice.Tax = Math.Floor(taxableSubTotal * _invoice.TaxRate / 100);
            _invoice.Total = taxableSubTotal + nonTaxableSubTotal + _invoice.Tax;
        }

        private async Task OnValidSubmit()
        {
            CalculateTotals();
            
            if (InvoiceId == 0)
            {
                // 新規作成
                var response = await Http.PostAsJsonAsync("/api/Invoices", _invoice);
                if (response.IsSuccessStatusCode)
                {
                    NavigationManager.NavigateTo("/invoicelist");
                }
            }
            else
            {
                // 更新
                var response = await Http.PutAsJsonAsync($"/api/Invoices/{InvoiceId}", _invoice);
                if (response.IsSuccessStatusCode)
                {
                    NavigationManager.NavigateTo("/invoicelist");
                }
            }
        }

        private async Task OnDeleteClick()
        {
            var response = await Http.DeleteAsync($"/api/Invoices/{InvoiceId}");
            if (response.IsSuccessStatusCode)
            {
                NavigationManager.NavigateTo("/invoicelist");
            }
        }

        private void OnCancelClick()
        {
            NavigationManager.NavigateTo("/invoicelist");
        }
        
        private class InvoiceNumberResponse
        {
            public string InvoiceNumber { get; set; } = string.Empty;
        }
    }
}