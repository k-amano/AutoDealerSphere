using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using Newtonsoft.Json;

namespace AutoDealerSphere.Client.Pages
{
    public partial class ClientList
    {
        private List<AutoDealerSphere.Shared.Models.Client> Clients { get; set; } = new();
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
            try
            {
                var result = await Http.PostAsJsonAsync<ClientSearch>("/api/Client/search", search);
                if (result.IsSuccessStatusCode)
                {
                    Clients = JsonConvert.DeserializeObject<List<AutoDealerSphere.Shared.Models.Client>>(await result.Content.ReadAsStringAsync()) ?? new();
                }
                else
                {
                    // 検索APIがない場合はフロントエンドでフィルタリング
                    await LoadData();
                    
                    if (search.Id > 0)
                    {
                        Clients = Clients.Where(c => c.Id == search.Id).ToList();
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(search.Name))
                        {
                            Clients = Clients.Where(c => c.Name.Contains(search.Name)).ToList();
                        }
                        if (!string.IsNullOrEmpty(search.Email))
                        {
                            Clients = Clients.Where(c => c.Email.Contains(search.Email)).ToList();
                        }
                    }
                }
            }
            catch
            {
                // エラーが発生した場合は全件取得
                await LoadData();
            }
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
