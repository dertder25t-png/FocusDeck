namespace FocusDeck.Domain.Entities;

/// <summary>
/// Represents a single Jarvis workflow run.
/// Phase 3.2: minimal tracking to support status queries and future diagnostics.
/// </summary>
public sealed class JarvisWorkflowRun : IMustHaveTenant
{
    public Guid Id { get; set; }

    /// <summary>Logical workflow identifier (e.g., filename or registry key).</summary>
    public string WorkflowId { get; set; } = string.Empty;

    /// <summary>
    /// High-level status (e.g., Queued, Running, Succeeded, Failed).
    /// Stored as a string to allow simple querying and evolution.
    /// </summary>
    public string Status { get; set; } = "Queued";

    public DateTime RequestedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public Guid TenantId { get; set; }
    public string RequestedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Short summary or last log line for the run.
    /// Useful for dashboards and quick debugging.
    /// </summary>
    public string? LogSummary { get; set; }

    /// <summary>
    /// Optional Hangfire job identifier associated with this run.
    /// </summary>
    public string? JobId { get; set; }
}
