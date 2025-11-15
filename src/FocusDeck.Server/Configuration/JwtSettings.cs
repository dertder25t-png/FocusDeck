using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace FocusDeck.Server.Configuration;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    private string _primaryKey = string.Empty;

    [JsonPropertyName("PrimaryKey")]
    public string PrimaryKey
    {
        get => _primaryKey;
        init => _primaryKey = value ?? string.Empty;
    }

    [JsonPropertyName("Key")]
    public string LegacyKey
    {
        get => _primaryKey;
        init => _primaryKey = value ?? string.Empty;
    }

    public string? SecondaryKey { get; init; }
    public string Issuer { get; init; } = "https://focusdeck.909436.xyz";
    public string Audience { get; init; } = "focusdeck-clients";
    public string[] AllowedIssuers { get; init; } = Array.Empty<string>();
    public string[] AllowedAudiences { get; init; } = Array.Empty<string>();
    public int AccessTokenExpirationMinutes { get; init; } = 60;
    public int RefreshTokenExpirationDays { get; init; } = 7;
    public string KeyRotationInterval { get; init; } = "90.00:00:00";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PrimaryKey))
        {
            throw new InvalidOperationException("JWT PrimaryKey is required and cannot be empty.");
        }

        if (PrimaryKey.Length < 32)
        {
            throw new InvalidOperationException("JWT PrimaryKey must be at least 32 characters long.");
        }

        if (PrimaryKey.IndexOf("your-secret", StringComparison.OrdinalIgnoreCase) >= 0 ||
            PrimaryKey.IndexOf("change_me", StringComparison.OrdinalIgnoreCase) >= 0 ||
            PrimaryKey.IndexOf("placeholder", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            throw new InvalidOperationException("JWT PrimaryKey appears to be a placeholder. Provide a strong secret.");
        }

        if (!string.IsNullOrWhiteSpace(SecondaryKey) && SecondaryKey!.Length < 32)
        {
            throw new InvalidOperationException("JWT SecondaryKey must be at least 32 characters long.");
        }

        if (!TimeSpan.TryParse(KeyRotationInterval, out _))
        {
            throw new InvalidOperationException("JWT KeyRotationInterval must be a valid TimeSpan (e.g., 90.00:00:00).");
        }
    }

    public TimeSpan GetKeyRotationInterval()
    {
        return TimeSpan.Parse(KeyRotationInterval);
    }

    public IReadOnlyCollection<string> GetValidIssuers()
    {
        var issuers = AllowedIssuers
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Select(i => i!.Trim())
            .Concat(new[] { Issuer })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return issuers.AsReadOnly();
    }

    public IReadOnlyCollection<string> GetValidAudiences()
    {
        var audiences = AllowedAudiences
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a!.Trim())
            .Concat(new[] { Audience })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return audiences.AsReadOnly();
    }
}
