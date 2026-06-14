using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace AutoDealerSphere.Client.Services;

public class ErrorHandlingHttpHandler : DelegatingHandler
{
    private readonly ErrorService _errorService;
    private readonly NavigationManager _navigationManager;

    public ErrorHandlingHttpHandler(ErrorService errorService, NavigationManager navigationManager)
    {
        _errorService = errorService;
        _navigationManager = navigationManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch (Exception)
        {
            _errorService.SetError(new ApiErrorResponse
            {
                Message = "",
                StatusCode = 0,
            });
            _navigationManager.NavigateTo("/error");
            throw;
        }

        if (!response.IsSuccessStatusCode)
        {
            // ログイン API は個別ハンドリングするためスキップ
            if (request.RequestUri?.PathAndQuery.Contains("/api/User/login") == true)
                return response;

            ApiErrorResponse? errorResponse = null;
            try
            {
                errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(cancellationToken: cancellationToken);
            }
            catch { }

            _errorService.SetError(errorResponse ?? new ApiErrorResponse
            {
                Message = "",
                StatusCode = (int)response.StatusCode,
            });

            _navigationManager.NavigateTo("/error");
        }

        return response;
    }
}
