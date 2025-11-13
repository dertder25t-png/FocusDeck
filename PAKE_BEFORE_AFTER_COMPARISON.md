# PAKE Register Finish Error - Before & After

## Issue 1: MobilePakeAuthService - Wrong Parameters

### ❌ BEFORE (Broken)
```csharp
var saltBytes = Convert.FromBase64String(startResponse.SaltBase64);  // ← SaltBase64 doesn't exist!
var privateKey = Srp.ComputePrivateKey(saltBytes, userId, password);
var verifier = Srp.ComputeVerifier(privateKey);
var vault = await _vaultService.ExportEncryptedAsync(password);

var finishRequest = new RegisterFinishRequest(
    userId,
    startResponse.SaltBase64,  // ❌ WRONG! Should be VerifierBase64
    Convert.ToBase64String(Srp.ToBigEndian(verifier)),  // ❌ WRONG position! Should be 3rd param
    vault.CipherText,
    vault.KdfMetadataJson,
    vault.CipherSuite);
```

**Problems:**
1. `startResponse.SaltBase64` - This property doesn't exist in `RegisterStartResponse`
2. First verifier parameter should be the actual `VerifierBase64`, not salt
3. KDF parameters are being passed in wrong position

### ✅ AFTER (Fixed)
```csharp
var kdfParams = System.Text.Json.JsonSerializer.Deserialize<FocusDeck.Shared.Security.SrpKdfParameters>(
    startResponse.KdfParametersJson);
if (kdfParams == null)
{
    _logger.LogWarning("Failed to deserialize KDF parameters for {UserId}", userId);
    return false;
}

var privateKey = Srp.ComputePrivateKey(kdfParams, userId, password);  // ✅ Use full KDF params
var verifier = Srp.ComputeVerifier(privateKey);
var vault = await _vaultService.ExportEncryptedAsync(password);

var finishRequest = new RegisterFinishRequest(
    userId,
    Convert.ToBase64String(Srp.ToBigEndian(verifier)),  // ✅ VerifierBase64 in 2nd position
    startResponse.KdfParametersJson,                     // ✅ KdfParametersJson in 3rd position
    vault.CipherText,
    vault.KdfMetadataJson,
    vault.CipherSuite);
```

**Improvements:**
1. ✅ Correctly deserializes KDF parameters (which includes salt internally)
2. ✅ Uses full KDF object for proper Argon2id key derivation
3. ✅ Parameters in correct order matching contract
4. ✅ Added error handling for KDF deserialization

---

## Issue 2: E2E Test - KDF Mismatch

### ❌ BEFORE (Broken)
```csharp
// Registration (line 79)
var x = Srp.ComputePrivateKey(kdf, userId, password);  // ← Uses Argon2id

// ... later during login (line 107)
var salt = Convert.FromBase64String(loginStartPayload.SaltBase64);
var x2 = Srp.ComputePrivateKey(salt, userId, password);  // ❌ Uses SHA256!
```

**Problem:**
- Registration derives key using Argon2id: `x = H(argon2id(password, salt, userId))`
- Login derives key using SHA256: `x2 = H(salt | H(userId:password))`
- Keys don't match → `x != x2` → login fails!

### ✅ AFTER (Fixed)
```csharp
// Registration (line 79)
var x = Srp.ComputePrivateKey(kdf, userId, password);  // ← Uses Argon2id

// ... later during login (lines 105-108)
var x2 = loginStartPayload.KdfParametersJson != null
    ? Srp.ComputePrivateKey(
        JsonSerializer.Deserialize<SrpKdfParameters>(loginStartPayload.KdfParametersJson)!,
        userId, 
        password)
    : Srp.ComputePrivateKey(Convert.FromBase64String(loginStartPayload.SaltBase64), userId, password);
```

**Improvements:**
1. ✅ Login checks for KDF parameters from server
2. ✅ Uses Argon2id when available (matches registration)
3. ✅ Falls back to legacy SHA256 only for old users without KDF params
4. ✅ Cryptographic consistency assured

---

## How the Fix Works

### Registration Flow
```
1. Client calls /register/start
   → Server sends KdfParametersJson (with salt, iterations, memory, etc.)

2. Client deserializes KDF parameters
   → Uses Argon2id to derive private key x
   → Computes verifier v = g^x mod N
   
3. Client calls /register/finish with:
   - userId ✅
   - verifierBase64 ✅
   - kdfParametersJson ✅
   - vaultData (encrypted)
   
4. Server validates and stores credential
   → Success! User account created
```

### Login Flow (After Fix)
```
1. Client calls /login/start
   → Server sends KdfParametersJson (matching what was stored at registration)

2. Client receives KDF parameters
   → Uses **same** Argon2id to derive private key x2
   → Now x2 == x (matches what was registered!)
   
3. Client computes proof with matching session key
   → Proof matches server's expected proof
   → Success! User logs in
```

---

## Test Results

### Before Fix
```
❌ FAILED: AuthPakeE2ETests.Pake_Register_Login_VaultRoundTrip
Error: Value is not the exact type
Expected: OkObjectResult (Success)
Actual: UnauthorizedObjectResult (Login failed - proof mismatch)
```

### After Fix
```
✅ PASSED: AuthPakeE2ETests.Pake_Register_Login_VaultRoundTrip
Total tests: 1
Passed: 1
Time: 3 seconds
```

---

## Changed Files
1. `/root/FocusDeck/src/FocusDeck.Mobile/Services/Auth/MobilePakeAuthService.cs`
2. `/root/FocusDeck/tests/FocusDeck.Server.Tests/AuthPakeE2ETests.cs`
