# PAKE Register Finish Error - Fix Summary

## Problem
You were getting a "PAKE register finish failed" error when trying to create a user account.

## Root Cause
There were **two critical bugs** in the PAKE registration implementation:

### Bug 1: Wrong Parameters in MobilePakeAuthService
**File:** `/root/FocusDeck/src/FocusDeck.Mobile/Services/Auth/MobilePakeAuthService.cs`

The `RegisterFinishRequest` was being constructed with parameters in the wrong order:
```csharp
// ❌ WRONG - passing SaltBase64 where VerifierBase64 should be
var finishRequest = new RegisterFinishRequest(
    userId,
    startResponse.SaltBase64,  // ← This is wrong!
    Convert.ToBase64String(Srp.ToBigEndian(verifier)),
    vault.CipherText,
    vault.KdfMetadataJson,
    vault.CipherSuite);
```

The correct contract requires:
```csharp
// ✅ CORRECT
public sealed record RegisterFinishRequest(
    string UserId,
    string VerifierBase64,        // ← Must be verifier
    string KdfParametersJson,     // ← Must be KDF parameters
    string? VaultDataBase64,
    string? VaultKdfMetadataJson = null,
    string? VaultCipherSuite = null);
```

**Also:** The mobile service was trying to access `startResponse.SaltBase64` which doesn't exist in `RegisterStartResponse`. It should use the KDF parameters instead.

### Bug 2: Inconsistent KDF Usage in E2E Test
**File:** `/root/FocusDeck/tests/FocusDeck.Server.Tests/AuthPakeE2ETests.cs`

The test was using two different key derivation methods that must be consistent:

1. **Registration** used Argon2id (via KDF parameters object)
2. **Login** used legacy SHA256 (via raw salt bytes)

This caused a key mismatch, making login fail after registration. The client proof computed during login wouldn't match what the server expected.

## Solutions Applied

### Fix 1: Corrected MobilePakeAuthService Registration Parameters
```csharp
// ✅ FIXED - Use KDF parameters from registration start response
var kdfParams = System.Text.Json.JsonSerializer.Deserialize<FocusDeck.Shared.Security.SrpKdfParameters>(
    startResponse.KdfParametersJson);
if (kdfParams == null)
{
    _logger.LogWarning("Failed to deserialize KDF parameters for {UserId}", userId);
    return false;
}

var privateKey = Srp.ComputePrivateKey(kdfParams, userId, password);
var verifier = Srp.ComputeVerifier(privateKey);

var finishRequest = new RegisterFinishRequest(
    userId,
    Convert.ToBase64String(Srp.ToBigEndian(verifier)),  // ✅ Correct: Verifier
    startResponse.KdfParametersJson,                     // ✅ Correct: KDF params
    vault.CipherText,
    vault.KdfMetadataJson,
    vault.CipherSuite);
```

### Fix 2: Fixed E2E Test to Use Consistent KDF Method
```csharp
// ✅ FIXED - Use KDF parameters if available, fall back to salt for legacy users
var x2 = loginStartPayload.KdfParametersJson != null
    ? Srp.ComputePrivateKey(JsonSerializer.Deserialize<SrpKdfParameters>(loginStartPayload.KdfParametersJson)!, userId, password)
    : Srp.ComputePrivateKey(Convert.FromBase64String(loginStartPayload.SaltBase64), userId, password);
```

## Verification

The **AuthPakeE2ETests.Pake_Register_Login_VaultRoundTrip** test now **PASSES**, confirming:
- ✅ User registration with PAKE works correctly

Fix: Login start failing with HTTP 500
-----------------------------------

Background: Testers reported that calls to `/v1/auth/pake/login/start` were returning HTTP 500 in production when a server-side record existed but the `PakeCredential.SaltBase64` field was empty.

Cause: Older credentials without KDF metadata could result in an empty salt. The server attempted to Base64-decode an empty string which throws a `FormatException` and resulted in a 500 response.

Fix: `AuthPakeController.LoginStart` now defensively checks whether `SaltBase64` is present. If it is missing, the code tries to pull the salt from `KdfParametersJson`. If still missing or invalid, the handler now returns `400 Bad Request` with a clear `error` message (`"Missing KDF salt"` or `"Invalid KDF salt"`) and records the auth failure. This prevents the server from returning 500s and provides better observability.

Unit test added: `Pake_Login_MissingSalt_ReturnsBadRequest` verifies behaviour for the missing salt scenario.
- ✅ Vault data is securely stored during registration
- ✅ Login flow succeeds after registration
- ✅ Access and refresh tokens are properly issued

## Impact

These fixes ensure that:
1. Mobile app registration will now complete successfully
2. The cryptographic key derivation is consistent between registration and login
3. User account creation flows work end-to-end

## Current State

- Registration persists verifiers/KDF metadata in `PakeCredentials`, vault blobs (when provided) in `KeyVaults`, and audit data in `AuthEventLogs`.
- The mobile/desktop clients send their `DevicePlatform` so the server records Argon2id credentials for modern clients while keeping SHA256 compatibility for older browsers.
- `TenantMembershipService` on login ensures every user owns a tenant/`UserTenant` pair before the SPA displays protected routes.

## Files Modified

1. `/root/FocusDeck/src/FocusDeck.Mobile/Services/Auth/MobilePakeAuthService.cs` - Fixed parameter order and KDF handling
2. `/root/FocusDeck/tests/FocusDeck.Server.Tests/AuthPakeE2ETests.cs` - Fixed KDF consistency in login proof computation
