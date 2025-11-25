# PAKE Authentication System - Complete Guide

## Overview

**PAKE (Password-Authenticated Key Exchange)** is a secure authentication mechanism that allows users to register and login with password-derived credentials without ever sending the password to the server. FocusDeck uses **SRP-6a (Secure Remote Password)** protocol with **Argon2id** key derivation.

---

## What is PAKE and Why We Use It

### Problem PAKE Solves
- Users register with a **password**
- Without PAKE: password is sent to server → server stores it → password visible in transit (security risk)
- With PAKE: password never leaves the client; instead, a cryptographic **verifier** is computed and stored

### How PAKE Works (High Level)
1. **Registration:**
   - Client generates a random **salt** and derives a **private key** from salt + password using Argon2id KDF
   - Client computes a **verifier** (cryptographic proof of password knowledge)
   - Client sends **verifier** (not password) to server
   - Server stores verifier; password is never stored or seen

2. **Login:**
   - Client uses the same **salt** + **password** to derive the same **private key**
   - Server generates a challenge, client and server exchange ephemeral keys
   - Both sides compute a shared **session key** using the private key/verifier relationship
   - Both sides verify the session key by exchanging **proofs**
   - If proofs match, login succeeds and tokens are issued

### Security Properties
- ✅ Password never transmitted to server
- ✅ Server only stores verifier (not password)
- ✅ If database is compromised, attacker gets verifier, not password
- ✅ Resistant to dictionary attacks (Argon2id is computationally expensive)
- ✅ No trust in server privacy; mutual authentication

---

## Architecture

### Server Components

#### 1. **AuthPakeController** (`src/FocusDeck.Server/Controllers/v1/AuthPakeController.cs`)
Handles all PAKE endpoints:
- `POST /v1/auth/pake/register/start` - Initiate registration (returns KDF parameters + SRP modulus)
- `POST /v1/auth/pake/register/finish` - Complete registration (client sends verifier)
- `POST /v1/auth/pake/login/start` - Initiate login (server returns challenge)
- `POST /v1/auth/pake/login/finish` - Complete login (client sends proof, server returns tokens)

#### 2. **SrpSessionCache** (`src/FocusDeck.Server/Services/Auth/SrpSessionCache.cs`)
- Stores ephemeral session state during login handshake
- Expires sessions after 5 minutes
- Prevents replay attacks by tracking session IDs

#### 3. **AuthAttemptLimiter** (`src/FocusDeck.Server/Services/Auth/AuthAttemptLimiter.cs`)
- Rate-limits failed login/registration attempts per user + IP
- Blocks after 10 failed attempts in 1 minute
- Prevents brute-force attacks

#### 4. **PakeCredential Entity** (`src/FocusDeck.Domain/Entities/Auth/PakeCredential.cs`)
Stores:
```csharp
public string UserId { get; set; }          // User identifier
public string SaltBase64 { get; set; }       // Random salt (Base64)
public string VerifierBase64 { get; set; }   // SRP verifier (Base64)
public string KdfParametersJson { get; set; } // KDF algorithm + config
public string Algorithm { get; set; }        // SRP algorithm name
public string ModulusHex { get; set; }       // SRP modulus (RFC 5054 group 14)
public int Generator { get; set; }           // SRP generator (usually 2)
public DateTime CreatedAt { get; set; }
public DateTime UpdatedAt { get; set; }
```

### Client Components

#### 1. **WebApp PAKE Library** (`src/FocusDeck.WebApp/src/lib/pake.ts`)
Implements SRP-6a + KDF on the client:
- Parses `KdfParametersJson` to determine algorithm (Argon2id or legacy SHA256)
- Computes private key from salt + password
- Generates client ephemeral keypair
- Computes and verifies session proofs

#### 2. **Mobile PAKE Service** (`src/FocusDeck.Mobile/Services/Auth/MobilePakeAuthService.cs`)
- Native C# implementation of SRP-6a
- Deserializes KDF parameters from server
- Computes Argon2id derivatives using `Konscious.Security.Cryptography.Argon2id`

#### 3. **Desktop PAKE (similar to Mobile)**
- Shared `src/FocusDeck.Shared/Security/Srp.cs` library
- Cross-platform SRP + KDF helpers

### Shared Components

#### **Srp.cs** (`src/FocusDeck.Shared/Security/Srp.cs`)
Common cryptographic library (used by server + clients):
```csharp
public record SrpKdfParameters
{
    [JsonPropertyName("alg")]
    public string Algorithm { get; init; }  // "sha256" or "argon2id"
    
    [JsonPropertyName("salt")]
    public string SaltBase64 { get; init; } // Random salt
    
    [JsonPropertyName("p")]
    public int DegreeOfParallelism { get; init; } // Argon2id param
    
    [JsonPropertyName("t")]
    public int Iterations { get; init; }    // Argon2id param
    
    [JsonPropertyName("m")]
    public int MemorySizeKiB { get; init; } // Argon2id param
    
    [JsonPropertyName("aad")]
    public bool UseAssociatedData { get; init; } // Include userId in hash
}
```

---

## Flow Diagrams

### Registration Flow

```
┌─────────────────┐                                    ┌──────────────┐
│     Client      │                                    │    Server    │
└────────┬────────┘                                    └──────┬───────┘
         │                                                    │
         │  POST /register/start (userId)                    │
         ├───────────────────────────────────────────────────>│
         │                                                    │
         │  Response: KdfParametersJson, modulus, gen        │
         │<───────────────────────────────────────────────────┤
         │                                                    │
         │  [Client derives key from password + salt]        │
         │  [Client computes verifier v = g^x mod N]         │
         │                                                    │
         │  POST /register/finish (verifier, KDF)            │
         ├───────────────────────────────────────────────────>│
         │                                                    │
         │                              [Server validates]   │
         │                         [Server stores verifier]  │
         │                                                    │
         │  Response: 200 OK (success)                       │
         │<───────────────────────────────────────────────────┤
```

### Login Flow

```
┌─────────────────┐                                    ┌──────────────┐
│     Client      │                                    │    Server    │
└────────┬────────┘                                    └──────┬───────┘
         │                                                    │
         │  POST /login/start (userId, clientPublic)         │
         ├───────────────────────────────────────────────────>│
         │                                                    │
         │                              [Server lookup cred]  │
         │                           [Server gen ephemeral]  │
         │                         [Server create session]   │
         │                                                    │
         │  Response: KDF, salt, serverPublic, sessionId     │
         │<───────────────────────────────────────────────────┤
         │                                                    │
         │  [Client derives same private key from password]  │
         │  [Client computes session key + proof]            │
         │                                                    │
         │  POST /login/finish (sessionId, clientProof)      │
         ├───────────────────────────────────────────────────>│
         │                                                    │
         │                       [Server derive same session] │
         │                          [Server verify proof]    │
         │                      [Server gen access token]    │
         │                                                    │
         │  Response: 200 OK (tokens, serverProof)           │
         │<───────────────────────────────────────────────────┤
         │                                                    │
         │  [Client verify serverProof]                      │
         │  [Login complete, store tokens locally]           │
```

---

## KDF Algorithms

### Argon2id (Recommended for Desktop/Mobile)
**Configuration in `Srp.GenerateKdfParameters()`:**
```json
{
  "alg": "argon2id",
  "salt": "Fba50dvoDPVjo46IgDs0gQ==",
  "p": 2,              // Parallelism: 2 threads
  "t": 3,              // Iterations: 3 passes
  "m": 65536,          // Memory: 64 MB
  "aad": true          // Bind to userId (additional authenticated data)
}
```

**Security:**
- Computationally expensive → resists dictionary attacks
- Memory-hard → resists GPU/ASIC attacks
- Time-cost tuned for ~1 second on modern CPU

### SHA256 (Legacy, Web Clients Only)
**Configuration:**
```json
{
  "alg": "sha256",
  "salt": "Fba50dvoDPVjo46IgDs0gQ==",
  "p": 0,
  "t": 0,
  "m": 0,
  "aad": false
}
```

**Why legacy?**
- WASM (JavaScript) Argon2 is slow (~3-5 seconds on browser)
- SHA256 is instant in JavaScript
- Acceptable trade-off for web clients (user isn't storing sensitive local data)
- Server tracks which KDF was used; clients use matching algorithm on login

**Note:** Web clients should upgrade to native Argon2id when WASM performance improves or native bindings available.

---

## Database Schema

### PakeCredentials Table
```sql
CREATE TABLE PakeCredentials (
  UserId TEXT PRIMARY KEY,
  SaltBase64 TEXT NOT NULL,              -- Random salt (16+ bytes)
  VerifierBase64 TEXT NOT NULL,          -- SRP verifier (Base64)
  KdfParametersJson TEXT NOT NULL,       -- Stores { alg, salt, p, t, m, aad }
  Algorithm TEXT NOT NULL,               -- SRP algorithm (e.g., "SRP-6a-2048-SHA256")
  ModulusHex TEXT NOT NULL,              -- SRP modulus (RFC 5054 group 14)
  Generator INTEGER NOT NULL,            -- SRP generator (2)
  TenantId TEXT NOT NULL,                -- Multi-tenant support
  CreatedAt TEXT NOT NULL,
  UpdatedAt TEXT NOT NULL
);
```

### KeyVaults Table (Optional)
Stores encrypted user vault (for password manager, secrets, etc.):
```sql
CREATE TABLE KeyVaults (
  UserId TEXT PRIMARY KEY,
  VaultDataBase64 TEXT NOT NULL,         -- Encrypted vault (AES-256-GCM)
  VaultCipherSuite TEXT NOT NULL,        -- e.g., "AES-256-GCM"
  KdfMetadataJson TEXT,                  -- How to decrypt vault
  Version INTEGER DEFAULT 1,
  CreatedAt TEXT NOT NULL,
  UpdatedAt TEXT NOT NULL,
  TenantId TEXT NOT NULL
);
```

---

## Configuration

### appsettings.json
```json
{
  "Jwt": {
    "Key": "your-256-bit-key-minimum-32-chars",
    "Issuer": "https://focusdeck.909436.xyz",
    "Audience": "focusdeck-clients",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=focusdeck.db"
  }
}
```

### Rate Limiting
- Per-IP rate limit: 10 auth attempts per minute
- Configurable via `appsettings.json` `RateLimiting:AuthBurst`

### Session Expiry
- SRP sessions expire after 5 minutes (configurable in `SrpSessionCache`)
- Login/register must complete within the window

---

## Common Issues & Troubleshooting

### Issue 1: Register Finish Returns HTTP 500

**Symptoms:**
```
POST /v1/auth/pake/register/finish HTTP/3 500
```

**Root Causes:**
1. **Missing KDF salt** - Client didn't generate or include salt in KdfParametersJson
2. **Invalid verifier encoding** - Verifier not valid Base64
3. **Database constraint** - UserId already exists (re-run register/start to get fresh session)
4. **TenantId missing** - Multi-tenant field not populated

**Fix (in code):**
```csharp
// Validate KDF has salt
if (string.IsNullOrWhiteSpace(kdfParams.SaltBase64))
{
    return BadRequest(new { error = "Missing salt in KDF parameters" });
}

// Validate verifier encoding
try
{
    verifierBytes = Convert.FromBase64String(request.VerifierBase64);
}
catch
{
    return BadRequest(new { error = "Invalid verifier encoding" });
}

// Ensure TenantId is set
pakeCredential.TenantId = Guid.NewGuid().ToString();
```

**Client check:**
```typescript
// Register flow: ensure KDF has salt
if (!kdf || !kdf.salt) {
  throw new Error("KDF parameters missing salt");
}
```

### Issue 2: Login Start Returns HTTP 500 (Missing Salt)

**Symptoms:**
```
POST /v1/auth/pake/login/start HTTP/2 500
PAKE login start unhandled exception
```

**Root Cause:**
- Credential was created before `SaltBase64` was populated
- Controller called `Convert.FromBase64String(cred.SaltBase64)` on empty/null string

**Fix (already implemented):**
```csharp
// Prefer stored salt, fall back to KdfParametersJson
string? saltBase64 = cred.SaltBase64;
if (string.IsNullOrWhiteSpace(saltBase64))
{
    var parsedKdf = TryParseKdf(cred.KdfParametersJson);
    saltBase64 = parsedKdf?.SaltBase64;
}

if (string.IsNullOrWhiteSpace(saltBase64))
{
    return BadRequest(new { error = "Missing KDF salt" });
}

// Try to parse Base64
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

**Remediation:**
1. Run backfill migration: `dotnet ef database update --context AutomationDbContext`
2. Or manually: `UPDATE PakeCredentials SET SaltBase64 = json_extract(KdfParametersJson, '$.salt') WHERE SaltBase64 IS NULL`
3. Restart server (startup backfill will log: `Backfilling {Count} PAKE credential(s) with salt from KDF metadata`)

### Issue 3: "Using legacy SHA256 derivation" on Web

**Symptoms:**
```
[PAKE] Using legacy SHA256 derivation
```

**Why:**
- Credentials registered before Argon2id fix (Nov 13, 2025)
- Server is telling client to use SHA256 instead of Argon2id for backward compatibility

**Expected Behavior:**
- Web clients should use SHA256 (WASM Argon2 is slow)
- Desktop/Mobile clients should use Argon2id
- Once web client is re-registered, it will get Argon2id

**Migration:**
- Have user re-register to get Argon2id credentials
- Or wait for WASM Argon2 performance improvement + client update

### Issue 4: CORS/Cloudflare Script Warnings

**Symptoms:**
```
Cross-Origin Request Blocked: https://static.cloudflareinsights.com/beacon.min.js
```

**Why:**
- Browser's Enhanced Tracking Protection blocks Cloudflare analytics script
- Does NOT affect PAKE login (this is unrelated)

**Fix:**
- Disable Enhanced Tracking Protection in browser for the domain, or
- Remove Cloudflare analytics script from HTML if not needed

---

## Deployment Checklist

- [ ] Database tables created with proper schema (PakeCredentials, KeyVaults, AuthEventLogs)
- [ ] JWT configuration set in environment variables or `appsettings.Production.json`
- [ ] SRP modulus and generator hardcoded in `Srp.cs` (RFC 5054 group 14)
- [ ] KDF parameters configured in `Srp.GenerateKdfParameters()` (Argon2id for desktop/mobile)
- [ ] Rate limiting enabled (`AuthBurst` policy)
- [ ] Session cache configured (default: Redis or in-memory)
- [ ] Log levels configured (Information for PAKE events, Warning for failures)
- [ ] HTTPS enforced (PAKE over HTTP is vulnerable to MITM)
- [ ] Firewall rules allow `/v1/auth/pake/*` endpoints from expected origins
- [ ] Backup database before large migrations
- [ ] Test full registration + login flow on staging before production

---

## Testing

### Unit Tests
```bash
dotnet test tests/FocusDeck.Server.Tests --filter "Pake"
```

Key tests:
- `Pake_Register_Success` - Full registration flow
- `Pake_Login_Success` - Full login flow
- `Pake_Login_MissingSalt_ReturnsBadRequest` - Salt guard logic
- `Pake_Login_InvalidProof_Fails` - Proof verification

### Manual Testing (Web)

**Register:**
```bash
curl -X POST https://focusdeckv1.909436.xyz/v1/auth/pake/register/start \
  -H "Content-Type: application/json" \
  -d '{"userId": "test@example.com", "devicePlatform": "web"}'
```

Response:
```json
{
  "kdfParametersJson": "{\"alg\":\"sha256\",\"salt\":\"...\",\"p\":0,\"t\":0,\"m\":0,\"aad\":false}",
  "algorithm": "SRP-6a-2048-SHA256",
  "modulusHex": "AC6BDB41...",
  "generator": 2
}
```

**Login:**
```bash
curl -X POST https://focusdeckv1.909436.xyz/v1/auth/pake/login/start \
  -H "Content-Type: application/json" \
  -d '{"userId": "test@example.com", "clientPublicEphemeralBase64": "..."}'
```

### Server Logs

Watch for:
```
[INF] PAKE register start for {maskedUserId} (platform=web)
[INF] PAKE register succeeded for {maskedUserId} (hasVault=true)
[INF] PAKE login start for {maskedUserId} (client=...)
[INF] PAKE login succeeded for {maskedUserId}
[WRN] PAKE login failure for {maskedUserId}: {reason}
```

---

## Performance Considerations

### Argon2id Tuning
Current settings:
- **Iterations (t):** 3 passes → ~1 second on modern CPU
- **Memory (m):** 65536 KB (64 MB) → strong vs. GPU attacks
- **Parallelism (p):** 2 threads → balance between speed and hardware usage

**Adjust if needed:**
- **Faster:** reduce `t` to 2, `m` to 32768
- **Slower/Secure:** increase `t` to 4, `m` to 131072

Edit in `Srp.GenerateKdfParameters()`:
```csharp
return new SrpKdfParameters("argon2id", 
    Convert.ToBase64String(salt), 
    degreeOfParallelism: 2,      // ← adjust
    iterations: 3,                // ← adjust
    memorySizeKiB: 65536,          // ← adjust
    aad: true);
```

### Session Cache
- Default: in-memory cache with 5-minute expiry
- For distributed systems: replace with Redis
- Configure in `Startup.cs` `AddScoped<ISrpSessionCache, ...>()`

---

## Security Best Practices

1. **Always use HTTPS** - PAKE over HTTP is vulnerable to MITM
2. **Validate all inputs** - Check Base64 encoding, JSON format, ranges
3. **Rate limit** - Prevent brute-force attacks (already enabled)
4. **Log failures** - Track suspicious patterns
5. **Rotate JWT keys** - Periodically regenerate signing keys
6. **Monitor session creation** - Alert on unusual registration rates
7. **Backup credentials** - Protect verifier table as sensitive
8. **Update Argon2id** - Use latest `Konscious.Security.Cryptography` package

---

## Future Improvements

- [ ] Add email verification to registration
- [ ] Implement password strength requirements on client
- [ ] Add multi-device login sessions (allow multiple active tokens per user)
- [ ] Implement device fingerprinting for suspicious logins
- [ ] Add passwordless authentication (biometric, FIDO2)
- [ ] Support delegated authentication (OAuth2 fallback)
- [ ] Analytics dashboard for auth metrics

---

## Support & Debugging

### Enable Debug Logging
```bash
export Serilog__MinimumLevel__Default="Debug"
export Serilog__MinimumLevel__Override__Microsoft="Debug"
```

### Check Auth Events
```sql
SELECT * FROM AuthEventLogs 
WHERE EventType LIKE 'PAKE_%' 
ORDER BY OccurredAtUtc DESC 
LIMIT 20;
```

### Inspect Credentials
```sql
SELECT UserId, Algorithm, CreatedAt, UpdatedAt 
FROM PakeCredentials;
```

---

**Last Updated:** November 14, 2025  
**Status:** ✅ Production-Ready
