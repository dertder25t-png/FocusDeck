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

## Issue 3: Login Start 500 (Missing KDF Salt)
### ❌ BEFORE (Broken)
```csharp
var saltBytes = Convert.FromBase64String(cred.SaltBase64);                            // ❌ Throws if SaltBase64 is empty
var verifier = Srp.FromBigEndian(Convert.FromBase64String(cred.VerifierBase64));
var session = _srpSessions.Store(..., saltBytes, ...);
```

When `SaltBase64` was blank, `Convert.FromBase64String` threw `FormatException`, the request bubbled out of `LoginStart`, and the API returned HTTP 500 instead of a structured error.

### ✅ AFTER (Fixed)
```csharp
string? saltBase64 = string.IsNullOrWhiteSpace(cred.SaltBase64)
    ? TryParseKdf(cred.KdfParametersJson)?.SaltBase64
    : cred.SaltBase64;

if (string.IsNullOrWhiteSpace(saltBase64))
{
    return BadRequest(new { error = "Missing KDF salt" });
}

byte[] saltBytes;
try
{
    saltBytes = Convert.FromBase64String(saltBase64);
}
catch
{
    return BadRequest(new { error = "Invalid KDF salt" });
}
```

### Improvements
1. ✅ Login now prefers the stored salt and falls back to the `KdfParametersJson` metadata, so the server can always hydrate `saltBytes` without throwing.
2. ✅ The handler now responds with `400 Bad Request` and logs `missing-salt`/`invalid-salt` failure reasons instead of crashing, making the error visible in telemetry.
3. ✅ The migration `20251114174500_BackfillPakeSaltFromKdf` plus the startup backfill fill the `SaltBase64` column from the stored JSON so future `login/start` calls work for legacy accounts.

---

## Changed Files
1. `/root/FocusDeck/src/FocusDeck.Mobile/Services/Auth/MobilePakeAuthService.cs`
2. `/root/FocusDeck/tests/FocusDeck.Server.Tests/AuthPakeE2ETests.cs`
