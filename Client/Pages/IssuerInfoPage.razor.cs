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
                // 発行者情報を保存
                var issuerResponse = await Http.PostAsJsonAsync("api/IssuerInfo", issuer);

                if (issuerResponse.IsSuccessStatusCode)
                {
                    var savedInfo = await issuerResponse.Content.ReadFromJsonAsync<IssuerInfo>();
                    if (savedInfo != null)
                    {
                        issuerInfo = savedInfo;
                    }
                }
                else
                {
                    var errorContent = await issuerResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"発行者情報の保存に失敗しました: {errorContent}");
                    return;
                }

                // メール設定を保存（パスワードが入力されている場合のみ）
                if (!string.IsNullOrEmpty(password))
                {
                    var emailRequest = new EmailSettingsRequestModel
                    {
                        Settings = emailSettings,
                        PlainPassword = password
                    };

                    var emailResponse = await Http.PostAsJsonAsync("api/EmailSettings", emailRequest);

                    if (!emailResponse.IsSuccessStatusCode)
                    {
                        var errorContent = await emailResponse.Content.ReadAsStringAsync();
                        Console.WriteLine($"メール設定の保存に失敗しました: {errorContent}");
                    }
                }

                if (isInitialSetup)
                {
                    Navigation.NavigateTo("/", replace: true);
                    return;
                }
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存中にエラーが発生しました: {ex.Message}");
            }
        }

        // Email settings methods
        private async Task LoadEmailSettings()
        {
            try
            {
                var response = await Http.GetFromJsonAsync<AutoDealerSphere.Shared.Models.EmailSettingsResponse>("api/EmailSettings");
                if (response != null)
                {
                    emailSettings = response.Settings;
                    password = response.PlainPassword;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"メール設定の読み込み時の情報: {ex.Message}");
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
            catch (Exception ex)
            {
                testConnectionMessage = $"接続テスト中にエラーが発生しました: {ex.Message}";
                testConnectionSuccess = false;
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