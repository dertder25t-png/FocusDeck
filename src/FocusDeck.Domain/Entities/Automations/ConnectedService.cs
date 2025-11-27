using FocusDeck.Domain.Entities;

namespace FocusDeck.Domain.Entities.Automations
{
    public class ConnectedService : IMustHaveTenant
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = null!; // Link to a user if you have auth
        public ServiceType Service { get; set; }
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime? ExpiresAt { get; set; }
        public DateTime ConnectedAt { get; set; }

        // Extensible JSON for provider-specific settings (e.g., Home Assistant base URL)
        public string? MetadataJson { get; set; }

        // Cached flag to indicate if this integration is configured (tokens/metadata present)
        public bool IsConfigured { get; set; }
        public Guid TenantId { get; set; }
    }
}
