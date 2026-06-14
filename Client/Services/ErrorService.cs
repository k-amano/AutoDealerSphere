using AutoDealerSphere.Shared.Models;

namespace AutoDealerSphere.Client.Services;

public class ErrorService
{
    public ApiErrorResponse? CurrentError { get; private set; }

    public void SetError(ApiErrorResponse error)
    {
        CurrentError = error;
    }

    public void SetError(string message, string traceId = "")
    {
        CurrentError = new ApiErrorResponse
        {
            Message = message,
            TraceId = traceId,
            StatusCode = 500,
        };
    }

    public void Clear()
    {
        CurrentError = null;
    }
}
