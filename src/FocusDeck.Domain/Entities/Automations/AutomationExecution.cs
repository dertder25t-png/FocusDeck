using FocusDeck.Domain.Entities;

namespace FocusDeck.Domain.Entities.Automations;

/// <summary>
/// Tracks execution history for automations
/// </summary>
public class AutomationExecution : IMustHaveTenant
{
    public int Id { get; set; }
    public Guid AutomationId { get; set; }
    public DateTime ExecutedAt { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }
    public string? TriggerData { get; set; }
    public Guid TenantId { get; set; }
}
