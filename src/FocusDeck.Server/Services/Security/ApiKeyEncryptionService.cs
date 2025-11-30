using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace FocusDeck.Server.Services.Security;

public interface IApiKeyEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}

public sealed class ApiKeyEncryptionService : IApiKeyEncryptionService
{
    private readonly IDataProtector _protector;

    public ApiKeyEncryptionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("FocusDeck.ApiKeys.v1");
    }

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext))
            return plaintext;

        return _protector.Protect(plaintext);
    }

    public string Decrypt(string ciphertext)
    {
        if (string.IsNullOrWhiteSpace(ciphertext))
            return ciphertext;

        try
        {
            return _protector.Unprotect(ciphertext);
        }
        catch (CryptographicException)
        {
            // If decryption fails (e.g., key rotation, corruption), return empty to avoid crashes
            return string.Empty;
        }
    }
}
