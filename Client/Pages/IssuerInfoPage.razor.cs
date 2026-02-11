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
        private EmailSettingsModel emailSettings = new EmailSettingsModel();
        private string password = "";
        private string testEmailAddress = "";
        private string emailErrorMessage = "";
        private string emailSuccessMessage = "";
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

        // Email settings methods
        private async Task LoadEmailSettings()
        {
            try
            {
                var response = await Http.GetFromJsonAsync<EmailSettingsModel>("api/EmailSettings");
                if (response != null)
                {
                    emailSettings = response;
                }
            }
            catch (Exception ex)
            {
                emailErrorMessage = $"メール設定の読み込みに失敗しました: {ex.Message}";
            }
        }

        private async Task SaveEmailSettings()
        {
            if (string.IsNullOrEmpty(password))
            {
                emailErrorMessage = "パスワードを入力してください。";
                return;
            }

            isEmailProcessing = true;
            emailErrorMessage = "";
            emailSuccessMessage = "";

            try
            {
                var request = new EmailSettingsRequestModel
                {
                    Settings = emailSettings,
                    PlainPassword = password
                };

                var response = await Http.PostAsJsonAsync("api/EmailSettings", request);

                if (response.IsSuccessStatusCode)
                {
                    emailSuccessMessage = "メール設定を保存しました。";
                    password = ""; // セキュリティのためクリア
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    emailErrorMessage = $"保存に失敗しました: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                emailErrorMessage = $"保存中にエラーが発生しました: {ex.Message}";
            }
            finally
            {
                isEmailProcessing = false;
            }
        }

        private async Task TestConnection()
        {
            if (string.IsNullOrEmpty(password))
            {
                emailErrorMessage = "パスワードを入力してください。";
                return;
            }

            isEmailProcessing = true;
            emailErrorMessage = "";
            emailSuccessMessage = "";

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
                        emailSuccessMessage = "接続テストに成功しました。";
                    }
                    else
                    {
                        emailErrorMessage = "接続テストに失敗しました。設定を確認してください。";
                    }
                }
                else
                {
                    emailErrorMessage = "接続テストに失敗しました。";
                }
            }
            catch (Exception ex)
            {
                emailErrorMessage = $"接続テスト中にエラーが発生しました: {ex.Message}";
            }
            finally
            {
                isEmailProcessing = false;
            }
        }

        private async Task SendTestEmail()
        {
            if (string.IsNullOrEmpty(testEmailAddress))
            {
                emailErrorMessage = "テストメール送信先を入力してください。";
                return;
            }

            isEmailProcessing = true;
            emailErrorMessage = "";
            emailSuccessMessage = "";

            try
            {
                var response = await Http.PostAsJsonAsync("api/EmailSettings/send-test-email", testEmailAddress);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<bool>();
                    if (result)
                    {
                        emailSuccessMessage = $"テストメールを送信しました: {testEmailAddress}";
                    }
                    else
                    {
                        emailErrorMessage = "テストメール送信に失敗しました。";
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    emailErrorMessage = $"テストメール送信に失敗しました: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                emailErrorMessage = $"テストメール送信中にエラーが発生しました: {ex.Message}";
            }
            finally
            {
                isEmailProcessing = false;
            }
        }
    }

    // クライアント側のモデル（Shared/Modelsと同じ構造）
    public class EmailSettingsModel
    {
        public int Id { get; set; }
        public string SmtpHost { get; set; } = "";
        public int SmtpPort { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string SenderEmail { get; set; } = "";
        public string SenderName { get; set; } = "";
        public string Username { get; set; } = "";
        public string EncryptedPassword { get; set; } = "";
        public DateTime UpdatedAt { get; set; }
    }

    public class EmailSettingsRequestModel
    {
        public EmailSettingsModel Settings { get; set; } = new EmailSettingsModel();
        public string PlainPassword { get; set; } = "";
    }
}