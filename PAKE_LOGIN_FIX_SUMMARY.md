# PAKE Login "Invalid Proof" Error - Fix Summary

## Problem Identified

Users were experiencing "Invalid proof" errors when trying to log in. The root cause was a mismatch in the Associated Data (AAD) parameter usage between client and server during Argon2id key derivation.

## Root Cause Analysis

### The Issue
1. **Missing AAD Property**: The C# `SrpKdfParameters` class was missing the `aad` (Associated Data) boolean property
2. **Client-Server Mismatch**: 
   - TypeScript client checked `kdf.aad` to determine whether to include userId as associated data
   - C# server ALWAYS included userId as associated data regardless of the flag
3. **Result**: Client and server computed different private keys → different proofs → "Invalid proof" error

### Technical Details
```typescript
// Client-side (pake.ts) - conditional AAD usage
const associatedData = kdf.aad === false ? null : encoder.encode(userId)
```

```csharp
// Server-side (Srp.cs) - ALWAYS used AAD (before fix)
AssociatedData = Encoding.UTF8.GetBytes(userId)  // No conditional check!
```

## Fix Applied

### 1. Updated `SrpKdfParameters` Class
Added the missing `aad` property:
```csharp
public record SrpKdfParameters
{
    // ... existing properties ...
    [JsonPropertyName("aad")]
    public bool UseAssociatedData { get; }

    public SrpKdfParameters(string algorithm, string saltBase64, 
        int degreeOfParallelism = 0, int iterations = 0, 
        int memorySizeKiB = 0, bool aad = true)
    {
        // ...
        UseAssociatedData = aad;
    }
}
```

### 2. Updated `ComputePrivateKey` Method
Made it respect the `UseAssociatedData` flag:
```csharp
public static BigInteger ComputePrivateKey(SrpKdfParameters kdf, string userId, string password)
{
    using var argon2 = new Argon2id(passwordBytes)
    {
        Salt = salt,
        DegreeOfParallelism = kdf.DegreeOfParallelism,
        Iterations = kdf.Iterations,
        MemorySize = kdf.MemorySizeKiB
    };

    // Only set AssociatedData if the KDF parameters specify it
    if (kdf.UseAssociatedData)
    {
        argon2.AssociatedData = Encoding.UTF8.GetBytes(userId);
    }

    var hash = argon2.GetBytes(32);
    return HashBytesToInteger(hash);
}
```

### 3. Updated KDF Generation Methods
- `GenerateKdfParameters()` (Argon2id): Sets `aad: true`
- `GenerateLegacyKdfParameters()` (SHA256): Sets `aad: false`

## Deployment Status

✅ **Server deployed**: November 13, 2025 at 22:15 UTC  
✅ **Build status**: All tests passing  
✅ **Service status**: Running successfully on production

## Impact on Existing Users

### New Registrations
- All new user registrations will work correctly with the proper AAD flag

### Existing Users
Users who registered **before this fix** may experience one of the following:

1. **Best case**: Their account was created with SHA256 (web platform) and will continue to work
2. **Problem case**: Their account has Argon2id metadata but was stored with mismatched verifier

### Resolution for Affected Users

If you cannot log in after this fix, you have two options:

#### Option 1: Contact Support
We can manually check your account's credential metadata and fix it if needed.

#### Option 2: Re-register (if acceptable)
If your account doesn't contain critical data, you can:
1. Clear browser cache/local storage
2. Re-register with the same email
3. Your new credentials will be created with the correct parameters

## Files Modified

1. `/root/FocusDeck/src/FocusDeck.Shared/Security/Srp.cs`
   - Added `UseAssociatedData` property to `SrpKdfParameters`
   - Updated `ComputePrivateKey` to respect the AAD flag
   - Updated KDF generation methods to set appropriate `aad` values

2. `/root/FocusDeck/src/FocusDeck.Server/Controllers/v1/AuthPakeController.cs`
   - Updated `SerializeLegacyKdf` to include `aad: false`

3. Test files updated:
   - `/root/FocusDeck/tests/FocusDeck.Shared.Tests/SrpTests.cs`
   - `/root/FocusDeck/tests/FocusDeck.Server.Tests/AuthPakeE2ETests.cs`

## Verification

To verify the fix is working:
1. Clear your browser cache and local storage
2. Try registering a new test account
3. Log in with the new account
4. Check browser console - you should see successful Argon2id derivation without errors

## Console Output (Success)
```
[PAKE] derivePrivateKey called with KDF: Object { alg: "argon2id", aad: true, ... }
[PAKE] Using Argon2id derivation
[PAKE] Argon2id hash computed successfully
[PAKE] Argon2id derivation successful
✅ Login successful
```

## Next Steps

If you're still experiencing issues:
1. Check the browser console for errors
2. Note the exact error message
3. Contact support with:
   - Your email/userId
   - Screenshot of console error
   - Approximate date of account creation

---
**Fix Date**: November 13, 2025  
**Issue**: Invalid proof error during PAKE login  
**Status**: ✅ RESOLVED
