using System.Net.Http.Json;
using AutoDealerSphere.Shared.Models;

namespace AutoDealerSphere.Client.Services
{
    public class AuthenticationService
    {
        private readonly HttpClient _httpClient;
        private string? _token;

        public AuthenticationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public string? Token => _token;

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/User/login", request);
            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

            if (loginResponse != null && loginResponse.Success && !string.IsNullOrEmpty(loginResponse.Token))
            {
                _token = loginResponse.Token;
                // トークンをlocalStorageに保存（ブラウザリロード時の永続性のため）
                await SaveTokenToStorageAsync(_token);
            }

            return loginResponse ?? new LoginResponse { Success = false, ErrorMessage = "ログインに失敗しました。" };
        }

        public async Task LogoutAsync()
        {
            _token = null;
            await RemoveTokenFromStorageAsync();
        }

        public async Task<string?> GetTokenAsync()
        {
            if (string.IsNullOrEmpty(_token))
            {
                // localStorageから取得を試みる
                _token = await LoadTokenFromStorageAsync();
            }
            return _token;
        }

        // JavaScriptのinteropを使用したlocalStorage操作のメソッド（簡易実装）
        private async Task SaveTokenToStorageAsync(string token)
        {
            // 本来はJSInteropを使用しますが、ここでは簡易的にメモリのみ保存
            await Task.CompletedTask;
        }

        private async Task<string?> LoadTokenFromStorageAsync()
        {
            // 本来はJSInteropを使用しますが、ここでは簡易的にnullを返す
            await Task.CompletedTask;
            return null;
        }

        private async Task RemoveTokenFromStorageAsync()
        {
            // 本来はJSInteropを使用しますが、ここでは簡易的に何もしない
            await Task.CompletedTask;
        }
    }
}