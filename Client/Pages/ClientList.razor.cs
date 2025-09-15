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
                    
                    // 名前またはカナの部分一致
                    if (!string.IsNullOrEmpty(search.NameOrKana) && Clients != null)
                    {
                        Clients = Clients.Where(c =>
                            c.Name.Contains(search.NameOrKana) ||
                            (c.Kana != null && c.Kana.Contains(search.NameOrKana))
                        ).ToList();
                    }

                    // メールアドレスの部分一致
                    if (!string.IsNullOrEmpty(search.Email) && Clients != null)
                    {
                        Clients = Clients.Where(c => c.Email.Contains(search.Email)).ToList();
                    }

                    // 電話番号の部分一致
                    if (!string.IsNullOrEmpty(search.Phone) && Clients != null)
                    {
                        Clients = Clients.Where(c => c.Phone != null && c.Phone.Contains(search.Phone)).ToList();
                    }

                    // 住所の部分一致（郵便番号、都道府県、住所）
                    if (!string.IsNullOrEmpty(search.Address) && Clients != null)
                    {
                        Clients = Clients.Where(c =>
                            c.Zip.Contains(search.Address) ||
                            GetPrefectureName(c.Prefecture).Contains(search.Address) ||
                            c.Address.Contains(search.Address)
                        ).ToList();
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
