using FocusDeck.Domain.Entities;

namespace FocusDeck.Domain.Entities.Auth;

public class RevokedAccessToken : IMustHaveTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Jti { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime RevokedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresUtc { get; set; }
    public Guid TenantId { get; set; }
}

