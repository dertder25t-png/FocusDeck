namespace FocusDeck.Domain.Entities.Auth;

public class PakeCredential
{
    public string UserId { get; set; } = string.Empty; // email or user identifier
    public string SaltBase64 { get; set; } = string.Empty;
    public string VerifierBase64 { get; set; } = string.Empty; // derived secret (placeholder for SRP verifier)
    public string Algorithm { get; set; } = "SRP-6a-2048-SHA256";
    public string ModulusHex { get; set; } = string.Empty;
    public int Generator { get; set; } = 2;
    public string? KdfParametersJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
