using System.Security.Cryptography;
using System.Text;
using Isopoh.Cryptography.Argon2;
using Microsoft.Maui.Storage;
using Plugin.Fingerprint.Abstractions;

namespace FocusDeck.Mobile.Services.Auth;

public sealed record MobileVaultExport(string CipherText, string CipherSuite, string KdfMetadataJson);

public class MobileVaultService
{
    private const string VaultKeyPreference = "focusdeck_mobile_vault_key";
    private static readonly TimeSpan KeyRotationInterval = TimeSpan.FromDays(365);
    private readonly IFingerprint _fingerprint;

    public MobileVaultService(IFingerprint fingerprint)
    {
        _fingerprint = fingerprint;
    }

    public async Task<byte[]?> GetOrCreateMasterKeyAsync()
    {
        var encoded = await SecureStorage.Default.GetAsync(VaultKeyPreference);
        if (!string.IsNullOrEmpty(encoded))
        {
            var request = new AuthenticationRequestConfiguration("Unlock FocusDeck Vault", "Provide your fingerprint to access your encrypted data.");
            var result = await _fingerprint.AuthenticateAsync(request);
            if (result.Authenticated)
            {
                return Convert.FromBase64String(encoded);
            }
            return null;
        }

        var key = RandomNumberGenerator.GetBytes(32);
        await SecureStorage.Default.SetAsync(VaultKeyPreference, Convert.ToBase64String(key));
        return key;
    }

    public async Task<MobileVaultExport> ExportEncryptedAsync(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required", nameof(password));
        }

        var masterKey = await GetOrCreateMasterKeyAsync();
        if (masterKey == null)
        {
            throw new InvalidOperationException("Could not get master key.");
        }
        var salt = RandomNumberGenerator.GetBytes(16);
        var derivedKey = DeriveArgon2Key(password, salt, masterKey.Length);
        var nonce = RandomNumberGenerator.GetBytes(12);

        using var aes = new AesGcm(derivedKey);
        var cipher = new byte[masterKey.Length];
        var tag = new byte[16];
        aes.Encrypt(nonce, masterKey, cipher, tag);

        var payload = new byte[salt.Length + nonce.Length + cipher.Length + tag.Length];
        Buffer.BlockCopy(salt, 0, payload, 0, salt.Length);
        Buffer.BlockCopy(nonce, 0, payload, salt.Length, nonce.Length);
        Buffer.BlockCopy(cipher, 0, payload, salt.Length + nonce.Length, cipher.Length);
        Buffer.BlockCopy(tag, 0, payload, salt.Length + nonce.Length + cipher.Length, tag.Length);

        var metadata = new
        {
            cipher = "AES-256-GCM",
            kdf = new
            {
                algorithm = "argon2id",
                version = "1.3",
                memoryKb = 64 * 1024,
                iterations = 4,
                parallelism = 2,
                saltBytes = salt.Length,
                header = "A2"
            }
        };

        return new MobileVaultExport(
            CipherText: "A2:" + Convert.ToBase64String(payload),
            CipherSuite: "AES-256-GCM",
            KdfMetadataJson: System.Text.Json.JsonSerializer.Serialize(metadata));
    }

    public async Task DeleteMasterKeyAsync()
    {
        SecureStorage.Default.Remove(VaultKeyPreference);
        await Task.CompletedTask;
    }

    public async Task<string> EncryptAsync(string plaintext)
    {
        var key = await GetOrCreateMasterKeyAsync();
        if (key == null)
        {
            throw new InvalidOperationException("Could not get master key.");
        }
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(key);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var result = new byte[nonce.Length + cipherBytes.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, nonce.Length, cipherBytes.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length + cipherBytes.Length, tag.Length);

        return Convert.ToBase64String(result);
    }

    public async Task<string> DecryptAsync(string ciphertext)
    {
        var key = await GetOrCreateMasterKeyAsync();
        if (key == null)
        {
            throw new InvalidOperationException("Could not get master key.");
        }
        var cipherBytes = Convert.FromBase64String(ciphertext);
        var nonce = cipherBytes[..12];
        var tag = cipherBytes[^16..];
        var cipher = cipherBytes[12..^16];
        var plainBytes = new byte[cipher.Length];

        using var aes = new AesGcm(key);
        aes.Decrypt(nonce, cipher, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    private static byte[] DeriveArgon2Key(string password, byte[] salt, int length)
    {
        var config = new Argon2Config
        {
            Type = Argon2Type.Id,
            Version = Argon2Version.Nineteen,
            Password = Encoding.UTF8.GetBytes(password),
            Salt = salt,
            MemoryCost = 64 * 1024, // 64 MB
            TimeCost = 4,
            Lanes = 2,
            Threads = 2,
            HashLength = length
        };

        using var argon2 = new Argon2(config);
        return argon2.Hash().Buffer;
    }
}
