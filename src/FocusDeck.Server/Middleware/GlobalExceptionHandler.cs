using System.Diagnostics;
using System.Net;
using System.Text.Json;
using FocusDeck.Server.Models;

namespace FocusDeck.Server.Middleware;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
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

        // Determine status code and error code based on exception type
        var (statusCode, errorCode) = exception switch
        {
            ArgumentNullException => (HttpStatusCode.BadRequest, "ARGUMENT_NULL"),
            ArgumentException => (HttpStatusCode.BadRequest, "INVALID_ARGUMENT"),
            InvalidOperationException => (HttpStatusCode.BadRequest, "INVALID_OPERATION"),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "FORBIDDEN"),
            FileNotFoundException => (HttpStatusCode.NotFound, "NOT_FOUND"),
            _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        // Add traceId to response headers for tracking
        context.Response.Headers.Append("traceId", traceId);

        var errorEnvelope = new ErrorEnvelope
        {
            TraceId = traceId,
            Code = errorCode,
            Message = GetSafeMessage(exception),
            Details = GetExceptionDetails(exception)
        };

        var json = JsonSerializer.Serialize(errorEnvelope, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        await context.Response.WriteAsync(json);
    }

    private string GetSafeMessage(Exception exception)
    {
        // In production, don't expose internal exception details
        if (_environment.IsProduction())
        {
            return exception switch
            {
                ArgumentException => "Invalid request parameters",
                InvalidOperationException => "The requested operation cannot be completed",
                UnauthorizedAccessException => "Access denied",
                FileNotFoundException => "Resource not found",
                _ => "An error occurred while processing your request"
            };
        }

        return exception.Message;
    }

    private object? GetExceptionDetails(Exception exception)
    {
        // In development, include stack trace for debugging
        if (_environment.IsDevelopment())
        {
            return new
            {
                type = exception.GetType().Name,
                stackTrace = exception.StackTrace?.Split('\n').Take(10).ToArray()
            };
        }

        return null;
    }
}
