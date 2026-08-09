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

        private enum Step { SelectUser, EnterCode, Done }

        private Step step = Step.SelectUser;
        private List<ResetUserItem> users = new();
        private int? selectedUserId;
        private string maskedEmail = "";
        private string verificationCode = "";
        private string newPassword = "";
        private string confirmPassword = "";
        private string errorMessage = "";
        private bool isProcessing = false;

        protected override async Task OnInitializedAsync()
        {
            users = await Http.GetFromJsonAsync<List<ResetUserItem>>("api/PasswordReset/users") ?? new();
        }

        private async Task SendResetCode()
        {
            if (selectedUserId == null)
            {
                errorMessage = "ユーザーを選択してください。";
                return;
            }

            isProcessing = true;
            errorMessage = "";

            try
            {
                var response = await Http.PostAsJsonAsync("api/PasswordReset/request",
                    new PasswordResetRequestModel { UserId = selectedUserId.Value });
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PasswordResetRequestResult>();
                    maskedEmail = result?.MaskedEmail ?? "";
                    step = Step.EnterCode;
                }
            }
            finally
            {
                isProcessing = false;
            }
        }

        private async Task ResetPasswordAsync()
        {
            if (string.IsNullOrEmpty(verificationCode))
            {
                errorMessage = "認証コードを入力してください。";
                return;
            }

            if (string.IsNullOrEmpty(newPassword))
            {
                errorMessage = "新しいパスワードを入力してください。";
                return;
            }

            if (newPassword.Length < 6)
            {
                errorMessage = "パスワードは6文字以上で入力してください。";
                return;
            }

            if (newPassword != confirmPassword)
            {
                errorMessage = "パスワードが一致しません。";
                return;
            }

            isProcessing = true;
            errorMessage = "";

            try
            {
                var response = await Http.PostAsJsonAsync("api/PasswordReset/reset",
                    new ResetPasswordRequestModel { UserId = selectedUserId!.Value, Code = verificationCode, NewPassword = newPassword });
                if (response.IsSuccessStatusCode)
                {
                    step = Step.Done;
                }
            }
            finally
            {
                isProcessing = false;
            }
        }

        private void BackToSelectUser()
        {
            verificationCode = "";
            newPassword = "";
            confirmPassword = "";
            errorMessage = "";
            maskedEmail = "";
            step = Step.SelectUser;
        }

        private void GoToLogin()
        {
            Navigation.NavigateTo("/");
        }
    }

    public class ResetUserItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class PasswordResetRequestModel
    {
        public int UserId { get; set; }
    }

    public class PasswordResetRequestResult
    {
        public string MaskedEmail { get; set; } = "";
    }

    public class ResetPasswordRequestModel
    {
        public int UserId { get; set; }
        public string Code { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }
}
