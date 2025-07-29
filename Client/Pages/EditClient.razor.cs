using AutoDealerSphere.Shared.Models;
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
            }
            _initialized = true;
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
