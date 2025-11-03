using System;
using System.Text.Json;
using System.Collections.Generic;

namespace FocusDeck.Domain.Entities.Remote;

/// <summary>
/// Represents a remote action command sent from one device to another
/// </summary>
public class RemoteAction
{
    /// <summary>
    /// Unique identifier for the remote action
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User ID this action belongs to
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Type/kind of action to perform
    /// </summary>
    public RemoteActionKind Kind { get; set; }

    /// <summary>
    /// JSON blob with action-specific payload data
    /// </summary>
    public string PayloadJson { get; set; } = "{}";

    /// <summary>
    /// When the action was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the action was completed (null if still pending)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Whether the action was successful
    /// </summary>
    public bool? Success { get; set; }

    /// <summary>
    /// Error message if action failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Get payload as dictionary
    /// </summary>
    public Dictionary<string, object> GetPayload()
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(PayloadJson) 
                   ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Set payload from dictionary
    /// </summary>
    public void SetPayload(Dictionary<string, object> payload)
    {
        PayloadJson = JsonSerializer.Serialize(payload);
    }

    /// <summary>
    /// Check if action is completed
    /// </summary>
    public bool IsCompleted => CompletedAt.HasValue;

    /// <summary>
    /// Check if action is pending
    /// </summary>
    public bool IsPending => !CompletedAt.HasValue;
}

/// <summary>
/// Types of remote actions that can be performed
/// </summary>
public enum RemoteActionKind
{
    OpenNote = 0,
    OpenDeck = 1,
    RearrangeLayout = 2,
    StartFocus = 3,
    StopFocus = 4
}
