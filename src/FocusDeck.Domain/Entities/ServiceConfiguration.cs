namespace FocusDeck.Domain.Entities;

/// <summary>
/// Stores OAuth client credentials and other service configuration
/// that users can configure through the UI without editing appsettings.json
/// </summary>
public class ServiceConfiguration : IMustHaveTenant
{
    public Guid Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? ApiKey { get; set; }

    /// <summary>
    /// Additional configuration stored as JSON
    /// </summary>
    public string? AdditionalConfig { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}
