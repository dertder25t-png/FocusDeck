using System;
using System.Security.Cryptography;
using System.Text;

namespace FocusDeck.Server.Services.Auth;

public static class KeyRotationHelper
{
    public static string GenerateSecureKey()
    {
        var bytes = new byte[64]; // 512 bits
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public static string GetKeyVersion(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key must not be empty.", nameof(key));
        }

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        return Convert.ToBase64String(hash, 0, 8);
    }
}
