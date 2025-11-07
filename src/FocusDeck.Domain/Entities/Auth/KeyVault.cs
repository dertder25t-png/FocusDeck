namespace FocusDeck.Domain.Entities.Auth;

public class KeyVault
{
    public string UserId { get; set; } = string.Empty;
    public string VaultDataBase64 { get; set; } = string.Empty; // salt+nonce+ciphertext+tag (from client export)
    public int Version { get; set; } = 1;
    public string CipherSuite { get; set; } = "AES-256-GCM";
    public string? KdfMetadataJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
