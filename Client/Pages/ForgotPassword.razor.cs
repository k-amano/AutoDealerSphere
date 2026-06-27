using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace AutoDealerSphere.Client.Pages
{
    public partial class ForgotPassword
    {
        [Inject]
        private HttpClient Http { get; set; }

        [Inject]
        private NavigationManager Navigation { get; set; }

        private async Task SendResetEmail()
        {
            if (string.IsNullOrEmpty(email))
            {
                errorMessage = "メールアドレスを入力してください。";
                return;
            }

            isProcessing = true;
            errorMessage = "";

            try
            {
                var baseUrl = Navigation.BaseUri.TrimEnd('/');
                var request = new PasswordResetRequestModel
                {
                    Email = email,
                    BaseUrl = baseUrl
                };

                var response = await Http.PostAsJsonAsync("api/PasswordReset/request", request);
                if (response.IsSuccessStatusCode)
                {
                    emailSent = true;
                }
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

    public class PasswordResetRequestModel
    {
        public string Email { get; set; } = "";
        public string BaseUrl { get; set; } = "";
    }
}
