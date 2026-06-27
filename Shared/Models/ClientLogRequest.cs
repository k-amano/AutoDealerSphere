namespace AutoDealerSphere.Shared.Models;

public class ClientLogRequest
{
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? Url { get; set; }
}
