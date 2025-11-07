namespace FocusDeck.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty; // SHA-256 hash of the token
    public string ClientFingerprint { get; set; } = string.Empty; // SHA-256 of ClientId/UserAgent
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? DevicePlatform { get; set; }
    public DateTime IssuedUtc { get; set; }
    public DateTime ExpiresUtc { get; set; }
    public DateTime? LastAccessUtc { get; set; }
    public DateTime? RevokedUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public bool IsExpired => DateTime.UtcNow >= ExpiresUtc;
    public bool IsRevoked => RevokedUtc != null;
    public bool IsActive => !IsRevoked && !IsExpired;
}
