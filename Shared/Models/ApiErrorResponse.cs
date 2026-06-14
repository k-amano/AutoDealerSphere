namespace AutoDealerSphere.Shared.Models;

public class ApiErrorResponse
{
    public string TraceId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
}
