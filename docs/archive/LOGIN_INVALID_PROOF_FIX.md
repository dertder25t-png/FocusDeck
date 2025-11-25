# Invalid Proof Error - Root Cause Analysis & Fix

## Problem
User gets **401 Unauthorized** with "Invalid proof" error when trying to login after registration.

## Root Cause Found ✓

The web app has a **critical bug** in the `pakeLogin` function:

**File:** `src/FocusDeck.WebApp/src/lib/pake.ts` lines 199-201

```typescript
async function derivePrivateKey(kdf: SrpKdfParameters | null, saltB64Fallback: string, userId: string, password: string): Promise<bigint> {
  const salt = kdf?.salt ?? saltB64Fallback
  if (!salt) {
    throw new Error('Missing KDF salt')
  }
  return computeLegacyPrivateKey(salt, userId, password)  // ❌ ALWAYS uses legacy SHA256!
}
```

### The Issue:
1. **Registration** (Server): Uses Argon2id to derive private key → creates verifier with Argon2id-derived key
2. **Login** (Server): Returns KDF parameters with Argon2id settings
3. **Login** (Web App): 
   - Receives KDF parameters with `alg: "argon2id"`
   - **IGNORES them** and uses legacy SHA256 instead
   - Computes WRONG private key (doesn't match what was registered)
   - Proof validation fails ❌

### Example Flow:

**Registration:**
```
salt = "L9XEvD1otUETaaTljCqvvw=="
password = "mypassword"
userId = "user@example.com"
KDF: Argon2id(password, salt, iterations=3, memory=65536, parallelism=2)
  → privateKey (Argon2id-derived)
  → verifier = g^privateKey mod N
  → Server stores verifier ✓
```

**Login Attempt:**
```
Server returns KDF params: { "alg": "argon2id", "salt": "...", "m": 65536, "t": 3, "p": 2 }
Web App receives KDF params but IGNORES them ❌
Web App does: SHA256(salt || SHA256(userId:password))
  → privateKey (SHA256-derived) ≠ registered privateKey
  → computed proof ≠ expected proof
  → 401 Unauthorized ❌
```

## Solution

### Fix 1: Add Argon2id Support to Web App

Argon2 is now available via the existing `argon2-browser` dependency and the derivation helpers are shared between registration/login.

### Fix 2: Implement Argon2id Derivation

`derivePrivateKey` now uses Argon2id when the server provides KDF metadata and falls back to SHA256 only when `alg` equals `sha256`, so the computed proofs always match the server-side verifiers:

```typescript
async function derivePrivateKey(kdf: SrpKdfParameters | null, saltB64Fallback: string, userId: string, password: string): Promise<bigint> {
  if (kdf?.alg === 'argon2id' && kdf.salt) {
    // Use Argon2id with provided parameters
    return computeArgon2idPrivateKey(kdf, userId, password)
  } else {
    // Legacy: use SHA256
    const salt = kdf?.salt ?? saltB64Fallback
    if (!salt) {
      throw new Error('Missing KDF salt')
    }
    return computeLegacyPrivateKey(salt, userId, password)
  }
}

async function computeArgon2idPrivateKey(kdf: SrpKdfParameters, userId: string, password: string): Promise<bigint> {
  const saltBytes = base64ToBytes(kdf.salt)
  const passwordBytes = new TextEncoder().encode(password)
  const associatedData = new TextEncoder().encode(userId)
  
  // Use Argon2id WASM library
  const hash = await argon2id({
    password: passwordBytes,
    salt: saltBytes,
    iterations: kdf.t ?? 3,
    memorySize: kdf.m ?? 65536,
    parallelism: kdf.p ?? 2,
    associatedData: associatedData,
    hashLen: 32,
  })
  
  return bytesToBigInt(hash)
}
```

## Implementation Steps

1. ✅ Diagnose the issue (DONE - see this document)
2. ✅ Add `argon2-browser` dependency to `package.json`
3. ✅ Implement `computeArgon2idPrivateKey` function
4. ✅ Update `derivePrivateKey` to use Argon2id when provided
5. ✅ Test registration and login flow
6. ✅ Deploy to production

## Current Status

- Modern registrations use Argon2id metadata; older accounts continue to receive SHA256 salts that match their stored verifiers.
- JWTs issued after login present an `app_tenant_id` claim, `AuthenticationMiddleware` validates the token+tenant claim, and `TenantMembershipService` ensures every user/device maps to a tenant before the SPA renders.

## Impact

- **Mobile apps**: Already fixed (fixes committed in earlier PRs)
- **Desktop app**: Depends on its implementation  
- **Web app**: Will be fixed with this update
- **Security**: Ensures Argon2id protection is actually used

## Testing

After fix, users should be able to:
1. ✓ Register with their password
2. ✓ Login immediately after registration
3. ✓ Login after server restart
4. ✓ Receive access token

## Files to Change

- `src/FocusDeck.WebApp/package.json` - Add argon2-browser
- `src/FocusDeck.WebApp/src/lib/pake.ts` - Update key derivation logic

## Compatibility

- ✓ Backward compatible with legacy SHA256 users
- ✓ Forward compatible with Argon2id users  
- ✓ No breaking changes

## Related Login Start 500

- **Symptom:** `/v1/auth/pake/login/start` used to throw a `FormatException` and return HTTP 500 when a credential had an empty `SaltBase64` because the controller called `Convert.FromBase64String` unconditionally.
- **Fix:** `AuthPakeController.LoginStart` now prefers the stored salt, falls back to `KdfParametersJson` when the column is blank, and returns `400 Bad Request` with `"Missing KDF salt"` or `"Invalid KDF salt"` instead of crashing. The handler also emits structured logs for those failure reasons so you can correlate 400 responses with specific users/devices.
- **Migration:** `20251114174500_BackfillPakeSaltFromKdf.cs` populates the `SaltBase64` column from the stored KDF JSON, and the startup backfill (log line `Backfilling {Count} PAKE credential(s) with salt from KDF metadata`) covers any remaining rows when the app boots.
- **Verification:** Run `dotnet test tests/FocusDeck.Server.Tests/ --filter Pake_Login_MissingSalt_ReturnsBadRequest` or hit `/v1/auth/pake/login/start` after trimming the `saltBase64` for a test user; you should now receive `400` with the `Missing KDF salt` payload instead of 500.
