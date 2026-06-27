using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace AutoDealerSphere.Client.Services;

public class ClientLogService
{
    private readonly HttpClient _httpClient;
    private readonly NavigationManager _navigationManager;

    // ErrorHandlingHttpHandler を経由しない素の HttpClient を受け取る
    public ClientLogService(Uri baseAddress, NavigationManager navigationManager)
    {
        _httpClient = new HttpClient { BaseAddress = baseAddress };
        _navigationManager = navigationManager;
    }

    public async Task LogErrorAsync(Exception exception)
    {
        try
        {
            var request = new ClientLogRequest
            {
                Message = exception.Message,
                StackTrace = exception.StackTrace,
                Url = _navigationManager.Uri,
            };
            await _httpClient.PostAsJsonAsync("api/ClientLog", request);
        }
        catch
        {
            // ログ送信失敗は無視（無限ループ防止）
        }
    }
}
