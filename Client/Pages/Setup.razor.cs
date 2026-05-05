using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;

namespace AutoDealerSphere.Client.Pages
{
    public partial class Setup
    {
        [Inject] private HttpClient Http { get; set; }
        [Inject] private NavigationManager Navigation { get; set; }

        private SetupModel model = new();
        private bool isChecking = true;
        private bool isProcessing = false;
        private string errorMessage = "";

        protected override async Task OnInitializedAsync()
        {
            var hasAdmin = await Http.GetFromJsonAsync<bool>("api/User/has-admin");
            if (hasAdmin)
            {
                Navigation.NavigateTo("/", replace: true);
                return;
            }
            isChecking = false;
        }

        private async Task HandleSetup()
        {
            if (model.Password != model.ConfirmPassword)
            {
                errorMessage = "パスワードが一致しません。";
                return;
            }

            isProcessing = true;
            errorMessage = "";

            try
            {
                var user = new User
                {
                    Name = model.Name,
                    Email = model.Email,
                    Password = model.Password,
                    Role = 2
                };

                var response = await Http.PostAsJsonAsync("api/User/add", user);
                if (response.IsSuccessStatusCode)
                {
                    var issuerRegistered = await Http.GetFromJsonAsync<bool>("api/IssuerInfo/is-registered");
                    Navigation.NavigateTo(issuerRegistered ? "/" : "/issuerinfo?setup=1", replace: true);
                }
                else
                {
                    errorMessage = "登録に失敗しました。もう一度お試しください。";
                }
            }
            catch
            {
                errorMessage = "エラーが発生しました。もう一度お試しください。";
            }
            finally
            {
                isProcessing = false;
            }
        }

        private class SetupModel
        {
            [Required(ErrorMessage = "名前を入力してください。")]
            [StringLength(40, ErrorMessage = "名前は40文字までです。")]
            public string Name { get; set; } = "";

            [Required(ErrorMessage = "メールアドレスを入力してください。")]
            [EmailAddress(ErrorMessage = "正しいメールアドレス形式で入力してください。")]
            public string Email { get; set; } = "";

            [Required(ErrorMessage = "パスワードを入力してください。")]
            [MinLength(6, ErrorMessage = "パスワードは6文字以上で入力してください。")]
            public string Password { get; set; } = "";

            [Required(ErrorMessage = "パスワード確認を入力してください。")]
            public string ConfirmPassword { get; set; } = "";
        }
    }
}
