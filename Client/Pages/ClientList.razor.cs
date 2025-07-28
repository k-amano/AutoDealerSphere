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
            var prefectures = new Dictionary<int, string>
            {
                { 0, "" },
                { 1, "北海道" },
                { 2, "青森県" },
                { 3, "岩手県" },
                { 4, "宮城県" },
                { 5, "秋田県" },
                { 6, "山形県" },
                { 7, "福島県" },
                { 8, "茨城県" },
                { 9, "栃木県" },
                { 10, "群馬県" },
                { 11, "埼玉県" },
                { 12, "千葉県" },
                { 13, "東京都" },
                { 14, "神奈川県" },
                { 15, "新潟県" },
                { 16, "富山県" },
                { 17, "石川県" },
                { 18, "福井県" },
                { 19, "山梨県" },
                { 20, "長野県" },
                { 21, "岐阜県" },
                { 22, "静岡県" },
                { 23, "愛知県" },
                { 24, "三重県" },
                { 25, "滋賀県" },
                { 26, "京都府" },
                { 27, "大阪府" },
                { 28, "兵庫県" },
                { 29, "奈良県" },
                { 30, "和歌山県" },
                { 31, "鳥取県" },
                { 32, "島根県" },
                { 33, "岡山県" },
                { 34, "広島県" },
                { 35, "山口県" },
                { 36, "徳島県" },
                { 37, "香川県" },
                { 38, "愛媛県" },
                { 39, "高知県" },
                { 40, "福岡県" },
                { 41, "佐賀県" },
                { 42, "長崎県" },
                { 43, "熊本県" },
                { 44, "大分県" },
                { 45, "宮崎県" },
                { 46, "鹿児島県" },
                { 47, "沖縄県" }
            };

            return prefectures.ContainsKey(code) ? prefectures[code] : "";
        }
    }
}
