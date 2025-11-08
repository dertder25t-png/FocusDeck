namespace FocusDeck.Services.Implementations.Core;

using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices;
using FocusDeck.Services.Abstractions;
using Konscious.Security.Cryptography;

/// <summary>
/// Implements AES-256-GCM encryption for cloud data protection
/// Uses DPAPI for secure key storage on Windows
/// </summary>
public readonly record struct VaultExport(string CipherText, string CipherSuite, string KdfMetadataJson);

public class EncryptionService : IEncryptionService2
{
    private const int KeySizeBytes = 32; // 256 bits for AES-256
    private const int NonceSizeBytes = 12; // 96 bits for GCM
    private const int TagSizeBytes = 16; // 128 bits for authentication tag
    private const int SaltSizeBytes = 16; // 128 bits for key derivation
    private const string HeaderArgon2 = "A2:"; // prefix for Argon2id-derived vault
    private const string HeaderPbkdf2 = "PB:"; // legacy prefix
    
    // Argon2id parameters
    private const int Argon2DegreesOfParallelism = 4;
    private const int Argon2Iterations = 3;
    private const int Argon2MemorySizeKb = 65536; // 64MB

    // DPAPI entropy
    private static readonly byte[] s_entropy = { 1, 2, 3, 4, 5, 6, 7, 8 };

    private byte[]? _encryptionKey;
    private readonly string _keyStorePath;

    public bool KeyExists => _encryptionKey != null || File.Exists(_keyStorePath);

    public EncryptionService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var focusdeckPath = Path.Combine(appDataPath, "FocusDeck", "Security");
        _keyStorePath = Path.Combine(focusdeckPath, "key.enc");

        // Try to load existing key
        LoadKeyFromStorage();
    }

    /// <summary>
    /// Generate a new random encryption key and store it securely
    /// </summary>
    public void GenerateKeyPair()
    {
        using (var rng = RandomNumberGenerator.Create())
        {
            _encryptionKey = new byte[KeySizeBytes];
            rng.GetBytes(_encryptionKey);
        }

        SaveKeyToStorage();
    }

    /// <summary>
    /// Delete the stored encryption key
    /// </summary>
    public void DeleteKey()
    {
        _encryptionKey = null;
        if (File.Exists(_keyStorePath))
        {
            try
            {
                File.Delete(_keyStorePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete key file: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Encrypt plain text using AES-256-GCM
    /// </summary>
    public string Encrypt(string plainText)
    {
        if (_encryptionKey == null)
        {
            throw new InvalidOperationException("Encryption key not initialized. Call GenerateKeyPair() first.");
        }

        if (string.IsNullOrEmpty(plainText))
        {
            return string.Empty;
        }

        try
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);

            using (var cipher = new AesGcm(_encryptionKey, TagSizeBytes))
            {
                // Generate random nonce
                byte[] nonce = new byte[NonceSizeBytes];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(nonce);
                }

                // Encrypt
                byte[] ciphertext = new byte[plainBytes.Length];
                byte[] tag = new byte[TagSizeBytes];

                cipher.Encrypt(nonce, plainBytes, ciphertext, tag);

                // Combine nonce + ciphertext + tag
                byte[] result = new byte[NonceSizeBytes + ciphertext.Length + TagSizeBytes];
                Buffer.BlockCopy(nonce, 0, result, 0, NonceSizeBytes);
                Buffer.BlockCopy(ciphertext, 0, result, NonceSizeBytes, ciphertext.Length);
                Buffer.BlockCopy(tag, 0, result, NonceSizeBytes + ciphertext.Length, TagSizeBytes);

                return Convert.ToBase64String(result);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Encryption failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Decrypt cipher text using AES-256-GCM
    /// </summary>
    public string Decrypt(string cipherText)
    {
        if (_encryptionKey == null)
        {
            throw new InvalidOperationException("Encryption key not initialized.");
        }

        if (string.IsNullOrEmpty(cipherText))
        {
            return string.Empty;
        }

        try
        {
            byte[] buffer = Convert.FromBase64String(cipherText);

            // Extract nonce, ciphertext, and tag
            byte[] nonce = new byte[NonceSizeBytes];
            Buffer.BlockCopy(buffer, 0, nonce, 0, NonceSizeBytes);

            int ciphertextLength = buffer.Length - NonceSizeBytes - TagSizeBytes;
            byte[] encryptedData = new byte[ciphertextLength];
            Buffer.BlockCopy(buffer, NonceSizeBytes, encryptedData, 0, ciphertextLength);

            byte[] tag = new byte[TagSizeBytes];
            Buffer.BlockCopy(buffer, NonceSizeBytes + ciphertextLength, tag, 0, TagSizeBytes);

            using (var cipher = new AesGcm(_encryptionKey, TagSizeBytes))
            {
                byte[] plaintext = new byte[encryptedData.Length];
                cipher.Decrypt(nonce, encryptedData, tag, plaintext);
                return Encoding.UTF8.GetString(plaintext);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Decryption failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Export encryption key encrypted with a password (for backup)
    /// </summary>
    public VaultExport ExportKeyEncryptedDetailed(string password)
    {
        if (_encryptionKey == null)
        {
            throw new InvalidOperationException("No key to export.");
        }

        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Password cannot be empty.");
        }

        try
        {
            // Derive key from password using Argon2id (strong KDF)
            byte[] salt = new byte[SaltSizeBytes];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            var derivedKey = DeriveArgon2id(password, salt, KeySizeBytes);

            // Encrypt the master key with derived key
            using (var cipher = new AesGcm(derivedKey, TagSizeBytes))
            {
                byte[] nonce = new byte[NonceSizeBytes];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(nonce);
                }

                byte[] ciphertext = new byte[_encryptionKey.Length];
                byte[] tag = new byte[TagSizeBytes];

                cipher.Encrypt(nonce, _encryptionKey, ciphertext, tag);

                // Combine salt + nonce + ciphertext + tag and prefix header
                byte[] result = new byte[SaltSizeBytes + NonceSizeBytes + ciphertext.Length + TagSizeBytes];
                Buffer.BlockCopy(salt, 0, result, 0, SaltSizeBytes);
                Buffer.BlockCopy(nonce, 0, result, SaltSizeBytes, NonceSizeBytes);
                Buffer.BlockCopy(ciphertext, 0, result, SaltSizeBytes + NonceSizeBytes, ciphertext.Length);
                Buffer.BlockCopy(tag, 0, result, SaltSizeBytes + NonceSizeBytes + ciphertext.Length, TagSizeBytes);

                var cipherText = HeaderArgon2 + Convert.ToBase64String(result);
                var metadata = new
                {
                    cipher = "AES-256-GCM",
                    kdf = new
                    {
                        algorithm = "argon2id",
                        iterations = Argon2Iterations,
                        parallelism = Argon2DegreesOfParallelism,
                        memorySize = Argon2MemorySizeKb,
                        saltBytes = SaltSizeBytes,
                        derivedKeyBytes = KeySizeBytes,
                        header = HeaderArgon2.TrimEnd(':')
                    }
                };

                var metadataJson = JsonSerializer.Serialize(metadata);
                return new VaultExport(cipherText, "AES-256-GCM", metadataJson);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Key export failed: {ex.Message}", ex);
        }
    }

    public string ExportKeyEncrypted(string password)
        => ExportKeyEncryptedDetailed(password).CipherText;

    /// <summary>
    /// Import encryption key from password-protected backup
    /// </summary>
    public bool ImportKeyEncrypted(string encryptedKeyData, string password)
    {
        if (string.IsNullOrEmpty(encryptedKeyData) || string.IsNullOrEmpty(password))
        {
            return false;
        }

        try
        {
            bool useArgon2 = false;
            string payload = encryptedKeyData;
            if (encryptedKeyData.StartsWith(HeaderArgon2))
            {
                useArgon2 = true;
                payload = encryptedKeyData.Substring(HeaderArgon2.Length);
            }
            else if (encryptedKeyData.StartsWith(HeaderPbkdf2))
            {
                useArgon2 = false;
                payload = encryptedKeyData.Substring(HeaderPbkdf2.Length);
            }

            byte[] buffer = Convert.FromBase64String(payload);

            // Extract salt, nonce, ciphertext, and tag
            byte[] salt = new byte[SaltSizeBytes];
            Buffer.BlockCopy(buffer, 0, salt, 0, SaltSizeBytes);

            byte[] nonce = new byte[NonceSizeBytes];
            Buffer.BlockCopy(buffer, SaltSizeBytes, nonce, 0, NonceSizeBytes);

            int ciphertextLength = buffer.Length - SaltSizeBytes - NonceSizeBytes - TagSizeBytes;
            byte[] ciphertext = new byte[ciphertextLength];
            Buffer.BlockCopy(buffer, SaltSizeBytes + NonceSizeBytes, ciphertext, 0, ciphertextLength);

            byte[] tag = new byte[TagSizeBytes];
            Buffer.BlockCopy(buffer, SaltSizeBytes + NonceSizeBytes + ciphertextLength, tag, 0, TagSizeBytes);

            byte[] derivedKey = useArgon2
                ? DeriveArgon2id(password, salt, KeySizeBytes)
                : new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256).GetBytes(KeySizeBytes);

            using (var cipher = new AesGcm(derivedKey, TagSizeBytes))
            {
                byte[] plainKey = new byte[ciphertext.Length];
                cipher.Decrypt(nonce, ciphertext, tag, plainKey);

                _encryptionKey = plainKey;
                SaveKeyToStorage();
                return true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Key import failed: {ex.Message}");
            return false;
        }
    }

    private void SaveKeyToStorage()
    {
        if (_encryptionKey == null)
        {
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(_keyStorePath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            byte[] keyData = _encryptionKey;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                keyData = ProtectedData.Protect(_encryptionKey, s_entropy, DataProtectionScope.CurrentUser);
            }

            File.WriteAllBytes(_keyStorePath, keyData);

            // Set restrictive file permissions (owner only)
            var fileInfo = new FileInfo(_keyStorePath);
            var fileSecurity = fileInfo.GetAccessControl();
            fileSecurity.SetAccessRuleProtection(true, false); // Disable inheritance, don't copy
            fileInfo.SetAccessControl(fileSecurity);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save key: {ex.Message}");
        }
    }

    private void LoadKeyFromStorage()
    {
        try
        {
            if (File.Exists(_keyStorePath))
            {
                byte[] keyData = File.ReadAllBytes(_keyStorePath);
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    _encryptionKey = ProtectedData.Unprotect(keyData, s_entropy, DataProtectionScope.CurrentUser);
                }
                else
                {
                    _encryptionKey = keyData;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load key: {ex.Message}");
            _encryptionKey = null;
        }
    }

    private static byte[] DeriveArgon2id(string password, byte[] salt, int length)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = Argon2DegreesOfParallelism,
            Iterations = Argon2Iterations,
            MemorySize = Argon2MemorySizeKb
        };

        return argon2.GetBytes(length);
    }
}
