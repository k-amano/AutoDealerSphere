using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace AutoDealerSphere.Client.Pages
{
    public partial class IssuerInfoPage
    {
        [Inject]
        private HttpClient Http { get; set; }

        [Inject]
        private NavigationManager Navigation { get; set; }

        private IssuerInfo issuerInfo = new IssuerInfo();

        protected override async Task OnInitializedAsync()
        {
            await LoadIssuerInfo();
        }

        private async Task LoadIssuerInfo()
        {
            try
            {
                var response = await Http.GetFromJsonAsync<IssuerInfo>("api/IssuerInfo");
                if (response != null)
                {
                    issuerInfo = response;
                }
            }
            catch (Exception ex)
            {
                // エラーがあってもデフォルト値で表示
                Console.WriteLine($"データの読み込みに失敗しました: {ex.Message}");
            }
        }

        private async Task SaveIssuerInfo(IssuerInfo issuer)
        {
            try
            {
                var response = await Http.PostAsJsonAsync("api/IssuerInfo", issuer);
                
                if (response.IsSuccessStatusCode)
                {
                    var savedInfo = await response.Content.ReadFromJsonAsync<IssuerInfo>();
                    if (savedInfo != null)
                    {
                        issuerInfo = savedInfo;
                    }
                    
                    // 保存成功のメッセージを表示するか、別のページにリダイレクト
                    StateHasChanged();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"保存に失敗しました: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存中にエラーが発生しました: {ex.Message}");
            }
        }
    }
}