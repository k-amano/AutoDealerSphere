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
        private bool isInitialSetup = false;

        // Email settings properties
        private AutoDealerSphere.Shared.Models.EmailSettings emailSettings = new AutoDealerSphere.Shared.Models.EmailSettings();
        private string password = "";
        private bool isEmailProcessing = false;
        private string testConnectionMessage = "";
        private bool testConnectionSuccess = false;

        protected override async Task OnInitializedAsync()
        {
            var uri = new Uri(Navigation.Uri);
            isInitialSetup = uri.Query.Contains("setup=1");
            await LoadIssuerInfo();
            await LoadEmailSettings();
        }

        private async Task LoadIssuerInfo()
        {
            var response = await Http.GetFromJsonAsync<IssuerInfo>("api/IssuerInfo");
            if (response != null)
            {
                issuerInfo = response;
            }
        }

        private async Task SaveIssuerInfo(IssuerInfo issuer)
        {
            var issuerResponse = await Http.PostAsJsonAsync("api/IssuerInfo", issuer);

            if (issuerResponse.IsSuccessStatusCode)
            {
                var savedInfo = await issuerResponse.Content.ReadFromJsonAsync<IssuerInfo>();
                if (savedInfo != null)
                {
                    issuerInfo = savedInfo;
                }
            }

            // メール設定を保存（パスワードが入力されている場合のみ）
            if (!string.IsNullOrEmpty(password))
            {
                var emailRequest = new EmailSettingsRequestModel
                {
                    Settings = emailSettings,
                    PlainPassword = password
                };

                await Http.PostAsJsonAsync("api/EmailSettings", emailRequest);
            }

            if (isInitialSetup)
            {
                Navigation.NavigateTo("/", replace: true);
                return;
            }
            StateHasChanged();
        }

        // Email settings methods
        private async Task LoadEmailSettings()
        {
            var response = await Http.GetFromJsonAsync<AutoDealerSphere.Shared.Models.EmailSettingsResponse>("api/EmailSettings");
            if (response != null)
            {
                emailSettings = response.Settings;
                password = response.PlainPassword;
            }
        }

        private async Task TestConnection()
        {
            if (string.IsNullOrEmpty(password))
            {
                testConnectionMessage = "パスワードを入力してください。";
                testConnectionSuccess = false;
                return;
            }

            isEmailProcessing = true;
            testConnectionMessage = "";

            try
            {
                var request = new EmailSettingsRequestModel
                {
                    Settings = emailSettings,
                    PlainPassword = password
                };

                var response = await Http.PostAsJsonAsync("api/EmailSettings/test-connection", request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<TestConnectionResult>();
                    testConnectionSuccess = result?.Success ?? false;
                    testConnectionMessage = result?.Message ?? "結果を取得できませんでした。";
                }
                else
                {
                    testConnectionMessage = "接続テストに失敗しました。";
                    testConnectionSuccess = false;
                }
            }
            finally
            {
                isEmailProcessing = false;
                StateHasChanged();
            }
        }
    }

    public class EmailSettingsRequestModel
    {
        public AutoDealerSphere.Shared.Models.EmailSettings Settings { get; set; } = new AutoDealerSphere.Shared.Models.EmailSettings();
        public string PlainPassword { get; set; } = "";
    }

    public class TestConnectionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }
}