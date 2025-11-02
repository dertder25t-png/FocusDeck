using System.Text.Json.Serialization;

namespace FocusDeck.Server.Models;

/// <summary>
/// Unified error response envelope for all API errors
/// </summary>
public class ErrorEnvelope
{
    /// <summary>
    /// Unique trace identifier for tracking and debugging
    /// </summary>
    [JsonPropertyName("traceId")]
    public required string TraceId { get; set; }

    /// <summary>
    /// Machine-readable error code (e.g., VALIDATION_FAILED, UNAUTHORIZED, etc.)
    /// </summary>
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    /// <summary>
    /// Human-readable error message
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; set; }

    /// <summary>
    /// Optional detailed error information (e.g., validation field errors)
    /// </summary>
    [JsonPropertyName("details")]
    public object? Details { get; set; }
}

/// <summary>
/// Validation field error detail
/// </summary>
public class ValidationFieldError
{
    [JsonPropertyName("field")]
    public required string Field { get; set; }

    [JsonPropertyName("message")]
    public required string Message { get; set; }
}
