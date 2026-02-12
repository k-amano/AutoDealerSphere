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

        // Email settings properties
        private AutoDealerSphere.Shared.Models.EmailSettings emailSettings = new AutoDealerSphere.Shared.Models.EmailSettings();
        private string password = "";
        private bool isEmailProcessing = false;

        protected override async Task OnInitializedAsync()
        {
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

                    if (emailResponse.IsSuccessStatusCode)
                    {
                        password = ""; // セキュリティのためクリア
                    }
                    else
                    {
                        var errorContent = await emailResponse.Content.ReadAsStringAsync();
                        Console.WriteLine($"メール設定の保存に失敗しました: {errorContent}");
                    }
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
                var response = await Http.GetFromJsonAsync<AutoDealerSphere.Shared.Models.EmailSettings>("api/EmailSettings");
                if (response != null)
                {
                    emailSettings = response;
                }
                // 設定が存在しない場合は空のオブジェクトが返ってくるので、エラーメッセージは表示しない
            }
            catch (Exception ex)
            {
                // 初回起動時など、設定が未登録の場合は正常な状態なのでエラーメッセージは表示しない
                // サーバー側で適切に処理されているため、通常はここには来ない
                Console.WriteLine($"メール設定の読み込み時の情報: {ex.Message}");
            }
        }

        private async Task TestConnection()
        {
            if (string.IsNullOrEmpty(password))
            {
                Console.WriteLine("パスワードを入力してください。");
                return;
            }

            isEmailProcessing = true;

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
                    var result = await response.Content.ReadFromJsonAsync<bool>();
                    if (result)
                    {
                        Console.WriteLine("接続テストに成功しました。");
                    }
                    else
                    {
                        Console.WriteLine("接続テストに失敗しました。設定を確認してください。");
                    }
                }
                else
                {
                    Console.WriteLine("接続テストに失敗しました。");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"接続テスト中にエラーが発生しました: {ex.Message}");
            }
            finally
            {
                isEmailProcessing = false;
            }
        }
    }

    public class EmailSettingsRequestModel
    {
        public AutoDealerSphere.Shared.Models.EmailSettings Settings { get; set; } = new AutoDealerSphere.Shared.Models.EmailSettings();
        public string PlainPassword { get; set; } = "";
    }
}