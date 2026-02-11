using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace AutoDealerSphere.Client.Pages
{
    public partial class EmailSettings
    {
        [Inject]
        private HttpClient Http { get; set; }

        [Inject]
        private NavigationManager Navigation { get; set; }

        private EmailSettingsModel emailSettings = new EmailSettingsModel();

        protected override async Task OnInitializedAsync()
        {
            await LoadSettings();
        }

        private async Task LoadSettings()
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
                errorMessage = $"設定の読み込みに失敗しました: {ex.Message}";
            }
        }

        private async Task SaveSettings()
        {
            if (string.IsNullOrEmpty(password))
            {
                errorMessage = "パスワードを入力してください。";
                return;
            }

            isProcessing = true;
            errorMessage = "";
            successMessage = "";

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
                    successMessage = "設定を保存しました。";
                    password = ""; // セキュリティのためクリア
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    errorMessage = $"保存に失敗しました: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"保存中にエラーが発生しました: {ex.Message}";
            }
            finally
            {
                isProcessing = false;
            }
        }

        private async Task TestConnection()
        {
            if (string.IsNullOrEmpty(password))
            {
                errorMessage = "パスワードを入力してください。";
                return;
            }

            isProcessing = true;
            errorMessage = "";
            successMessage = "";

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
                        successMessage = "接続テストに成功しました。";
                    }
                    else
                    {
                        errorMessage = "接続テストに失敗しました。設定を確認してください。";
                    }
                }
                else
                {
                    errorMessage = "接続テストに失敗しました。";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"接続テスト中にエラーが発生しました: {ex.Message}";
            }
            finally
            {
                isProcessing = false;
            }
        }

        private async Task SendTestEmail()
        {
            if (string.IsNullOrEmpty(testEmailAddress))
            {
                errorMessage = "テストメール送信先を入力してください。";
                return;
            }

            isProcessing = true;
            errorMessage = "";
            successMessage = "";

            try
            {
                var response = await Http.PostAsJsonAsync("api/EmailSettings/send-test-email", testEmailAddress);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<bool>();
                    if (result)
                    {
                        successMessage = $"テストメールを送信しました: {testEmailAddress}";
                    }
                    else
                    {
                        errorMessage = "テストメール送信に失敗しました。";
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    errorMessage = $"テストメール送信に失敗しました: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"テストメール送信中にエラーが発生しました: {ex.Message}";
            }
            finally
            {
                isProcessing = false;
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
