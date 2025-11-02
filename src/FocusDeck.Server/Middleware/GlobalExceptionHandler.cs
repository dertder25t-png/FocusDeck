using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace FocusDeck.Server.Middleware;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        
        _logger.LogError(exception, "Unhandled exception occurred. TraceId: {TraceId}", traceId);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var errorCode = exception switch
        {
            ArgumentNullException => "ARGUMENT_NULL",
            ArgumentException => "INVALID_ARGUMENT",
            InvalidOperationException => "INVALID_OPERATION",
            UnauthorizedAccessException => "UNAUTHORIZED",
            _ => "INTERNAL_ERROR"
        };

        var response = new
        {
            traceId,
            code = errorCode,
            message = exception.Message
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
