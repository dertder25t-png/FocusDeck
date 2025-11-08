# Encryption Metadata Schema

This document explains the metadata we store alongside encrypted vaults so any client (desktop, mobile, web) can decrypt or migrate the data safely.

## Vault Export Payload

Desktop exports the master key with EncryptionService.ExportKeyEncryptedDetailed. The result contains:

`json
{
  "cipherText": "A2:BASE64...",
  "cipherSuite": "AES-256-GCM",
  "kdfMetadataJson": "{ ... }"
}
`

- cipherText: Base64 string prefixed with A2: (Argon2/PBKDF2 compatibility) that bundles salt | nonce | ciphertext | tag.
- cipherSuite: Human-readable descriptor for the AEAD algorithm.
- kdfMetadataJson: JSON payload describing how the wrapping key was derived.

### KDF Metadata JSON Structure

Current desktop builds emit the following shape:

`json
{
  "cipher": "AES-256-GCM",
  "kdf": {
    "algorithm": "pbkdf2-sha256",
    "iterations": 200000,
    "saltBytes": 16,
    "derivedKeyBytes": 32,
    "header": "A2"
  }
}
`

Notes:
- We currently fallback to high-iteration PBKDF2 until native Argon2 is available everywhere. The header flag keeps the historical A2 prefix for migration.
- iterations represents the cost parameter. Clients should fail closed if it deviates from expected safe ranges.

## Pairing Transfer Payloads

Pair transfers send the same cipherText plus the metadata fields via PairTransferRequest. Server persists:

- KeyVault.CipherSuite
- KeyVault.KdfMetadataJson
- PairingSession.VaultKdfMetadataJson

Clients redeeming a pairing session must read these fields to determine how to decrypt the vault before importing.

## Versioning Guidance

- Treat unknown cipherSuite or kdf.algorithm values as unsupported and prompt the user to upgrade.
- When introducing new algorithms (e.g., Argon2id native), bump Version on KeyVault, preserve previous metadata, and ensure exporters include backward-compatible information.
- Always log the metadata alongside auth events so auditors can trace which clients used which crypto primitives.
