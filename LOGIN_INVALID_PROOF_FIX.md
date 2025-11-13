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

Add `argon2-browser` or `argon2-wasm` package which provides Argon2 in WebAssembly format.

### Fix 2: Implement Argon2id Derivation

Update `derivePrivateKey` function to use Argon2id when KDF parameters are provided:

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
2. ⬜ Add `argon2-browser` dependency to `package.json`
3. ⬜ Implement `computeArgon2idPrivateKey` function
4. ⬜ Update `derivePrivateKey` to use Argon2id when provided
5. ⬜ Test registration and login flow
6. ⬜ Deploy to production

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
