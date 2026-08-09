using AutoDealerSphere.Server.Exceptions;
using AutoDealerSphere.Shared.Models;
using System.Net;
using System.Text.Json;

namespace AutoDealerSphere.Server.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BusinessException ex)
        {
            // ユーザーに理由を表示してよい業務エラー
            _logger.LogWarning(ex, "業務エラーが発生しました。Path: {Path}", context.Request.Path);
            await WriteErrorResponseAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "未処理の例外が発生しました。Path: {Path}", context.Request.Path);
            await WriteErrorResponseAsync(context, HttpStatusCode.InternalServerError,
                "サーバーでエラーが発生しました。管理者にお問い合わせください。");
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, HttpStatusCode statusCode, string message)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new ApiErrorResponse
        {
            TraceId = context.TraceIdentifier,
            Message = message,
            StatusCode = (int)statusCode,
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}
