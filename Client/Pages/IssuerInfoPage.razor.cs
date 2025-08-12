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
        private bool isLoading = true;
        private bool isSaving = false;
        private string errorMessage = null;

        protected override async Task OnInitializedAsync()
        {
            await LoadIssuerInfo();
        }

        private async Task LoadIssuerInfo()
        {
            try
            {
                isLoading = true;
                errorMessage = null;
                var response = await Http.GetFromJsonAsync<IssuerInfo>("api/IssuerInfo");
                if (response != null)
                {
                    issuerInfo = response;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"データの読み込みに失敗しました: {ex.Message}";
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task SaveIssuerInfo()
        {
            try
            {
                isSaving = true;
                errorMessage = null;
                
                var response = await Http.PostAsJsonAsync("api/IssuerInfo", issuerInfo);
                
                if (response.IsSuccessStatusCode)
                {
                    var savedInfo = await response.Content.ReadFromJsonAsync<IssuerInfo>();
                    if (savedInfo != null)
                    {
                        issuerInfo = savedInfo;
                    }
                    
                    // 保存成功のメッセージを表示するか、別のページにリダイレクト
                    // ここでは簡単のため、そのまま表示を更新
                    StateHasChanged();
                }
                else
                {
                    errorMessage = "保存に失敗しました。";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"保存中にエラーが発生しました: {ex.Message}";
            }
            finally
            {
                isSaving = false;
            }
        }
    }
}