using FocusDeck.SharedKernel.Privacy;
using FocusDeck.SharedKernel.Tenancy;

namespace FocusDeck.Domain.Entities;

/// <summary>
/// Stores the tenant-specific consent state for each capture context type.
/// </summary>
public sealed class PrivacySetting : IMustHaveTenant
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string ContextType { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public PrivacyTier Tier { get; set; } = PrivacyTier.Medium;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
