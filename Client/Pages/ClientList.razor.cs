using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using Newtonsoft.Json;

namespace AutoDealerSphere.Client.Pages
{
    public partial class ClientList
    {
        private List<AutoDealerSphere.Shared.Models.Client>? Clients { get; set; } = null;
        private AutoDealerSphere.Shared.Models.ClientSearch Search { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        private void AddClient()
        {
            NavigationManager.NavigateTo($"/client/0");
        }

        private async Task EditClient(int id)
        {
            NavigationManager.NavigateTo($"/client/{id}");
        }

        private async Task OnSearch(ClientSearch search)
        {
            var result = await Http.PostAsJsonAsync<ClientSearch>("/api/Client/search", search);
            result.EnsureSuccessStatusCode();
            Clients = JsonConvert.DeserializeObject<List<AutoDealerSphere.Shared.Models.Client>>(await result.Content.ReadAsStringAsync()) ?? new();
        }

        private async Task LoadData()
        {
            var result = await Http.GetFromJsonAsync<List<AutoDealerSphere.Shared.Models.Client>>($"/api/Client");
            if (result != null)
            {
                Clients = result;
            }
            else
            {
                Clients = new();
            }
        }

        private string GetPrefectureNameFromContext(object context)
        {
            var client = context as AutoDealerSphere.Shared.Models.Client;
            if (client == null) return "";
            return GetPrefectureName(client.Prefecture);
        }

        private void EditClientFromContext(object context)
        {
            var client = context as AutoDealerSphere.Shared.Models.Client;
            if (client != null)
            {
                NavigationManager.NavigateTo($"/client/{client.Id}");
            }
        }

        private string GetPrefectureName(int code)
        {
            return Prefecture.GetName(code);
        }
    }
}
