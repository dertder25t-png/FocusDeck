namespace FocusDeck.Shared.Contracts.Auth;

/// <summary>
/// Request payload for beginning user registration.
/// </summary>
/// <param name="UserId">User identifier (stable username / email alias).</param>
public sealed record RegisterStartRequest(string UserId);

/// <summary>
/// Response for register start containing KDF parameters and SRP group info.
/// </summary>
public sealed record RegisterStartResponse(string KdfParametersJson, string Algorithm, string ModulusHex, int Generator);

/// <summary>
/// Completes registration with verifier and optional encrypted vault payload.
/// </summary>
/// <param name="UserId">User identifier submitted in Start.</param>
/// <param name="VerifierBase64">Base64 encoded PAKE verifier.</param>
/// <param name="KdfParametersJson">JSON describing KDF parameters used for the verifier.</param>
/// <param name="VaultDataBase64">Optional encrypted vault payload to bootstrap key storage.</param>
/// <param name="VaultKdfMetadataJson">Optional JSON describing encryption/KDF parameters for the vault.</param>
/// <param name="VaultCipherSuite">Optional cipher suite descriptor (e.g. AES-256-GCM).</param>
public sealed record RegisterFinishRequest(
    string UserId,
    string VerifierBase64,
    string KdfParametersJson,
    string? VaultDataBase64,
    string? VaultKdfMetadataJson = null,
    string? VaultCipherSuite = null);

/// <summary>
/// Response for register finish.
/// </summary>
public sealed record RegisterFinishResponse(bool Success);

/// <summary>
/// Request payload for login start.
/// </summary>
/// <param name="UserId">User identifier to authenticate.</param>
public sealed record LoginStartRequest(
    string UserId,
    string ClientPublicEphemeralBase64,
    string? ClientId = null,
    string? DeviceName = null,
    string? DevicePlatform = null);

/// <summary>
/// Response payload for login start containing session parameters.
/// </summary>
public sealed record LoginStartResponse(
    string? KdfParametersJson,
    string SaltBase64, 
    string ServerPublicEphemeralBase64, 
    Guid SessionId, 
    string Algorithm, 
    string ModulusHex, 
    int Generator);

/// <summary>
/// Completes login with PAKE client proof and optional client metadata.
/// </summary>
/// <param name="UserId">User identifier to authenticate.</param>
/// <param name="SessionId">Server-provided session identifier.</param>
/// <param name="ClientProofBase64">Client-computed proof (Base64).</param>
/// <param name="ClientId">Optional device identifier (machine name / mobile id).</param>
public sealed record LoginFinishRequest(
    string UserId,
    Guid SessionId,
    string ClientProofBase64,
    string? ClientId = null,
    string? DeviceName = null,
    string? DevicePlatform = null);

/// <summary>
/// Response payload for login finish with issued tokens and server proof.
/// </summary>
public sealed record LoginFinishResponse(bool Success, bool HasVault, string AccessToken, string RefreshToken, int ExpiresIn, string ServerProofBase64);


/// <summary>
/// Request to upgrade a legacy credential to use a new KDF.
/// </summary>
/// <param name="UserId">The user to upgrade.</param>
/// <param name="VerifierBase64">The new PAKE verifier, computed with the new KDF.</param>
/// <param name="KdfParametersJson">The new KDF parameters used.</param>
public sealed record UpgradeCredentialRequest(string UserId, string VerifierBase64, string KdfParametersJson);

/// <summary>
/// Response for a credential upgrade request.
/// </summary>
public sealed record UpgradeCredentialResponse(bool Success);


/// <summary>
/// Request payload for starting a device pairing session.
/// </summary>
/// <param name="SourceDeviceId">Identifier of the device initiating pairing.</param>
public sealed record PairStartRequest(string? SourceDeviceId);

/// <summary>
/// Transfers an encrypted vault blob to the pairing session.
/// </summary>
/// <param name="PairingId">Server-issued pairing session identifier.</param>
/// <param name="VaultDataBase64">Encrypted vault data blob.</param>
/// <param name="VaultKdfMetadataJson">Optional JSON describing encryption/KDF parameters for the vault.</param>
/// <param name="VaultCipherSuite">Optional cipher suite descriptor (e.g. AES-256-GCM).</param>
/// <param name="TargetDeviceId">Optional identifier of intended target device.</param>
public sealed record PairTransferRequest(
    Guid PairingId,
    string VaultDataBase64,
    string? VaultKdfMetadataJson = null,
    string? VaultCipherSuite = null,
    string? TargetDeviceId = null);

/// <summary>
/// Redeems pairing session to download encrypted vault onto a target device.
/// </summary>
/// <param name="PairingId">Pairing session identifier.</param>
/// <param name="Code">One-time code (human readable) to authorize redemption.</param>
public sealed record PairRedeemRequest(Guid PairingId, string Code);
