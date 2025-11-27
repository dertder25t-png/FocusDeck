namespace FocusDeck.Contracts.DTOs;

/// <summary>
/// Summary of a Jarvis workflow available on the server.
/// </summary>
/// <param name="Id">Stable workflow identifier (e.g., file or logical name).</param>
/// <param name="Name">Human-readable name.</param>
/// <param name="Description">Optional description or tooltip text.</param>
public sealed record JarvisWorkflowSummaryDto(
    string Id,
    string Name,
    string? Description);

/// <summary>
/// Request payload to enqueue a Jarvis workflow run.
/// </summary>
/// <param name="WorkflowId">Identifier of the workflow to run.</param>
/// <param name="ArgumentsJson">Optional JSON blob with workflow arguments.</param>
public sealed record JarvisRunRequestDto(
    string WorkflowId,
    string? ArgumentsJson);

/// <summary>
/// Response payload for a newly created Jarvis workflow run.
/// </summary>
/// <param name="RunId">Server-issued run identifier.</param>
public sealed record JarvisRunResponseDto(Guid RunId);

/// <summary>
/// Status payload for an existing Jarvis workflow run.
/// </summary>
/// <param name="RunId">Run identifier.</param>
/// <param name="Status">High-level status (e.g., Pending, Running, Succeeded, Failed).</param>
/// <param name="Summary">Optional short summary or last log line.</param>
public sealed record JarvisRunStatusDto(
    Guid RunId,
    string Status,
    string? Summary);

