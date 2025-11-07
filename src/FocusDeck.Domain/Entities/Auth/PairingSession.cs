namespace FocusDeck.Domain.Entities.Auth;

public enum PairingStatus
{
    Pending = 0,
    Ready = 1,
    Completed = 2,
    Expired = 3
}

public class PairingSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty; // short code displayed in QR
    public PairingStatus Status { get; set; } = PairingStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(10);

    // Optional: device id initiating pairing
    public string? SourceDeviceId { get; set; }
    public string? TargetDeviceId { get; set; }

    // Payload transferred (encrypted vault data)
    public string? VaultDataBase64 { get; set; }
    public string? VaultKdfMetadataJson { get; set; }
    public string? VaultCipherSuite { get; set; }
}
