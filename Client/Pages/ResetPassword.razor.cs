using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace AutoDealerSphere.Client.Pages
{
    public partial class ResetPassword
    {
        [Inject]
        private HttpClient Http { get; set; }

        [Inject]
        private NavigationManager Navigation { get; set; }

        private string token = "";

        protected override async Task OnInitializedAsync()
        {
            // URLからトークンを取得
            var uri = new Uri(Navigation.Uri);
            var query = uri.Query;

            if (!string.IsNullOrEmpty(query) && query.Contains("token="))
            {
                var tokenIndex = query.IndexOf("token=") + 6;
                var endIndex = query.IndexOf('&', tokenIndex);
                token = endIndex > 0 ? query.Substring(tokenIndex, endIndex - tokenIndex) : query.Substring(tokenIndex);
                token = Uri.UnescapeDataString(token);
                await VerifyToken();
            }
            else
            {
                errorMessage = "無効なリンクです。";
                isLoading = false;
            }
        }

        private async Task VerifyToken()
        {
            try
            {
                var response = await Http.GetAsync($"api/PasswordReset/verify/{token}");

                if (response.IsSuccessStatusCode)
                    isValidToken = true;
                else
                    errorMessage = "トークンが無効または期限切れです。";
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task ResetPasswordAsync()
        {
            if (string.IsNullOrEmpty(newPassword))
            {
                errorMessage = "新しいパスワードを入力してください。";
                return;
            }

            if (newPassword != confirmPassword)
            {
                errorMessage = "パスワードが一致しません。";
                return;
            }

            if (newPassword.Length < 6)
            {
                errorMessage = "パスワードは6文字以上で入力してください。";
                return;
            }

            isProcessing = true;
            errorMessage = "";

            try
            {
                var request = new ResetPasswordRequestModel
                {
                    Token = token,
                    NewPassword = newPassword
                };

                var response = await Http.PostAsJsonAsync("api/PasswordReset/reset", request);
                if (response.IsSuccessStatusCode)
                    passwordReset = true;
                else
                    errorMessage = "パスワードリセットに失敗しました。";
            }
            finally
            {
                isProcessing = false;
            }
        }

        private void GoToLogin()
        {
            Navigation.NavigateTo("/login");
        }
    }

    public class ResetPasswordRequestModel
    {
        public string Token { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }
}
