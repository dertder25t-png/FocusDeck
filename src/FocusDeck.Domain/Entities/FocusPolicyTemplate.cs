namespace FocusDeck.Domain.Entities;

/// <summary>
/// User-defined focus policy template
/// </summary>
public class FocusPolicyTemplate : IMustHaveTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Strict { get; set; } = false;
    public bool AutoBreak { get; set; } = true;
    public bool AutoDim { get; set; } = false;
    public bool NotifyPhone { get; set; } = false;
    public int? TargetDurationMinutes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}
