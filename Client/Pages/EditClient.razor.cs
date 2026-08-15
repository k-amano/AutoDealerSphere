using AutoDealerSphere.Shared.Models;
using AutoDealerSphere.Client.Components;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace AutoDealerSphere.Client.Pages
{
    public partial class EditClient
    {
        [Parameter]
        public int ClientId { get; set; }
        private AutoDealerSphere.Shared.Models.Client? _item;
        private bool _initialized = false;
        private List<Vehicle> _vehicles = new();
        private Invoice[] _invoices = Array.Empty<Invoice>();
        private InvoiceBasicInfoDialog? _invoiceDialog;

        protected override async Task OnInitializedAsync()
        {
            var client = await Http.GetFromJsonAsync<AutoDealerSphere.Shared.Models.Client>($"/api/Client/{this.ClientId}");
            if (client != null)
            {
                _item = new AutoDealerSphere.Shared.Models.Client()
                {
                    Id = client.Id,
                    Name = client.Name,
                    Kana = client.Kana,
                    Email = client.Email,
                    Zip = client.Zip,
                    Prefecture = client.Prefecture,
                    Address = client.Address,
                    Building = client.Building,
                    Phone = client.Phone
                };

                _vehicles = await Http.GetFromJsonAsync<List<Vehicle>>($"/api/Vehicles/byClient/{this.ClientId}") ?? new();
                _invoices = await Http.GetFromJsonAsync<Invoice[]>($"/api/Invoices/byClient/{this.ClientId}") ?? Array.Empty<Invoice>();
            }
            _initialized = true;
        }

        private void AddVehicle()
        {
            this.NavigationManager.NavigateTo($"/vehicle/0?clientId={this.ClientId}");
        }

        private async Task CreateInvoice()
        {
            var invoice = new Invoice
            {
                InvoiceNumber = string.Empty, // サーバー側で自動生成
                ClientId = this.ClientId,
                VehicleId = _vehicles.Count == 1 ? _vehicles[0].Id : null,
                InvoiceDate = DateTime.Now,
                WorkCompletedDate = DateTime.Now
            };
            if (_invoiceDialog != null)
            {
                await _invoiceDialog.Open(invoice);
            }
        }

        private async Task OnInvoiceSaved(Invoice invoice)
        {
            var response = await Http.PostAsJsonAsync("/api/Invoices", invoice);
            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<Invoice>();
                if (created != null)
                {
                    this.NavigationManager.NavigateTo($"/invoice/detail/{created.Id}");
                }
            }
        }

        private async Task OnClickOK(AutoDealerSphere.Shared.Models.Client item)
        {
            var result = await Http.PostAsJsonAsync("/api/Client/update", item);
            if (result?.StatusCode == System.Net.HttpStatusCode.OK)
            {
                this.NavigationManager.NavigateTo("/clientlist");
            }
        }

        private async Task OnClickDelete()
        {
            var result = await Http.DeleteAsync($"/api/Client/{this.ClientId}");
            if (result?.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                this.NavigationManager.NavigateTo("/clientlist");
            }
        }
    }
}
