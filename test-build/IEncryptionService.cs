namespace FocusDeck.Services.Abstractions;

public readonly record struct VaultExport(string CipherText, string CipherSuite, string KdfMetadataJson);

public interface IEncryptionService
{
    bool KeyExists { get; }
    void GenerateKeyPair();
    void DeleteKey();
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
    VaultExport ExportKeyEncryptedDetailed(string password);
    string ExportKeyEncrypted(string password);
    bool ImportKeyEncrypted(string encryptedKeyData, string password);
}
