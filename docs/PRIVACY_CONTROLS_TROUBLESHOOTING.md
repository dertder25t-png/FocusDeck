# Privacy Controls Troubleshooting Guide

## Issue: "No privacy controls are available yet"

### Root Cause
The privacy controls endpoint (`/v1/privacy/consent`) **requires authentication** via the `[Authorize]` attribute. When users see "No privacy controls are available yet," it means the client is either:

1. Not sending a JWT token in the Authorization header
2. Sending an invalid/expired JWT token
3. The JWT token doesn't contain the required claims (`sub` or `ClaimTypes.NameIdentifier`)

### Diagnosis Steps

#### 1. Check Server Logs
Look for warnings when GetUserId() can't resolve a claim:

```bash
tail -f /home/focusdeck/FocusDeck/logs/server.log | grep -i "privacy\|GetConsent\|User identifier missing"
```

Expected log entries:
- ✅ **Success**: No warnings, just DB queries for PrivacySettings
- ❌ **Problem**: `"GetConsent called without valid user identifier. Claims: ..."`

#### 2. Verify JWT Configuration Match

**Development (source):**
```json
{
  "Jwt": {
    "Issuer": "https://focusdeck.909436.xyz",
    "Audience": "focusdeck-clients",
    "Key": "FocusDeck2025SecretKeyProduction12345678"
  }
}
```

**Production (deployed):**
```bash
cat /home/focusdeck/FocusDeck/publish/appsettings.json | grep -A 5 '"Jwt"'
cat /home/focusdeck/FocusDeck/publish/appsettings.Production.json | grep -A 5 '"Jwt"'
```

Both must have:
- Same `Issuer`
- Same `Audience`  
- Same `Key` (40+ characters, not a placeholder)

#### 3. Test the Endpoint

**Without Auth (should return 401):**
```bash
curl -v http://localhost:5000/v1/privacy/consent
```

Expected: `HTTP/1.1 401 Unauthorized`

**With Valid Auth Token:**
```bash
# First, login to get a token
TOKEN=$(curl -s -X POST http://localhost:5000/v1/auth/pake/login/finish \
  -H "Content-Type: application/json" \
  -d '{"userId":"test@example.com",...}' | jq -r '.accessToken')

# Then use it
curl -H "Authorization: Bearer $TOKEN" http://localhost:5000/v1/privacy/consent
```

Expected: `200 OK` with privacy settings array

#### 4. Check Browser Network Tab

When the web UI calls `/v1/privacy/consent`:

**Headers to inspect:**
```
Authorization: Bearer eyJhbGciOiJIUzI1Ni...
```

**Possible responses:**

| Status | Body | Meaning |
|--------|------|---------|
| 401 | (empty) | No token sent or token validation failed |
| 401 | `{"error":"User identifier missing..."}` | Token valid but missing `sub` or `ClaimTypes.NameIdentifier` claim |
| 200 | `[{contextType:"ActiveWindowTitle",...}]` | Success! |

### Fix: Client Side

#### Web App (React)

The issue is likely in how the token is being sent. Check `src/FocusDeck.WebApp/src/lib/api.ts`:

```typescript
// ✅ Correct
const response = await fetch('/v1/privacy/consent', {
  headers: {
    'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
    'Content-Type': 'application/json'
  }
});

// ❌ Wrong - missing Authorization header
const response = await fetch('/v1/privacy/consent');
```

#### Desktop (WPF)

Check the `HttpClient` configuration in `KeyProvisioningService` or API client:

```csharp
// ✅ Correct
var request = new HttpRequestMessage(HttpMethod.Get, "/v1/privacy/consent");
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

// ❌ Wrong - no auth header
var request = new HttpRequestMessage(HttpMethod.Get, "/v1/privacy/consent");
```

### Fix: Token Claims

Verify that `TokenService.cs` is adding both claims when generating tokens:

```csharp
// src/FocusDeck.Server/Services/Auth/TokenService.cs
new(ClaimTypes.NameIdentifier, userId),  // ✅ This should exist
new(JwtRegisteredClaimNames.Sub, userId), // ✅ And this
```

The PrivacyController checks for both:

```csharp
var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
    ?? User?.FindFirst("sub")?.Value
    ?? string.Empty;
```

### Enhanced Error Response

The server now returns detailed error messages when userId is missing:

**Before:**
```json
HTTP 200 OK
[]
```

**After (if claims missing):**
```json
HTTP 401 Unauthorized
{
  "error": "User identifier missing from authentication token"
}
```

Plus server logs:
```
[WARN] GetConsent called without valid user identifier. Claims: aud=focusdeck-clients, iss=https://focusdeck.909436.xyz, exp=...
```

This helps diagnose whether:
- No claims at all → token validation failed
- Claims present but no `sub`/`NameIdentifier` → TokenService bug

### Testing Checklist

- [ ] `/v1/privacy/consent` returns 401 without auth token
- [ ] `/v1/privacy/consent` returns 401 with "User identifier missing" if claims are wrong
- [ ] `/v1/privacy/consent` returns 200 with privacy settings array when auth is valid
- [ ] Server logs show the actual claims when GetUserId() fails
- [ ] JWT config matches between development and production
- [ ] Web UI is sending `Authorization: Bearer <token>` header
- [ ] Desktop app is setting the Authorization header on HttpClient

### Expected Success Flow

1. User logs in → receives JWT with `sub` and `ClaimTypes.NameIdentifier` claims
2. Client stores token in localStorage/SecureStorage
3. Client sends `GET /v1/privacy/consent` with `Authorization: Bearer <token>` header
4. Server validates token → extracts userId from claims
5. Server queries `PrivacySettings` table for user's settings
6. Server returns merged list of 7 context types with current consent status
7. UI displays toggle switches for each privacy control

### Default Privacy Settings

All 7 context types default to **disabled** until user explicitly enables them:

| Context Type | Default | Privacy Tier |
|--------------|---------|--------------|
| ActiveWindowTitle | ❌ Disabled | Medium |
| TypingVelocity | ❌ Disabled | Medium |
| MouseEntropy | ❌ Disabled | Medium |
| AmbientNoise | ❌ Disabled | High |
| DeviceMotion | ❌ Disabled | Medium |
| ScreenState | ❌ Disabled | Medium |
| PhysicalLocation | ❌ Disabled | High |

Users should see all 7 items even with an empty database (no stored preferences yet).

---

## Quick Fix Commands

**Check if server is running:**
```bash
ps aux | grep "[d]otnet.*FocusDeck"
curl http://localhost:5000/healthz
```

**View recent privacy logs:**
```bash
tail -50 /home/focusdeck/FocusDeck/logs/server.log | grep -i privacy
```

**Restart server:**
```bash
pkill -9 dotnet
cd /home/focusdeck/FocusDeck/publish
ASPNETCORE_ENVIRONMENT=Production nohup /usr/bin/dotnet FocusDeck.Server.dll > /home/focusdeck/FocusDeck/logs/server.log 2>&1 &
```

**Test with curl (no auth - should fail):**
```bash
curl -v http://localhost:5000/v1/privacy/consent 2>&1 | grep "401"
```

**Verify JWT config:**
```bash
diff <(cat /root/FocusDeck/src/FocusDeck.Server/appsettings.json | grep -A 5 Jwt) \
     <(cat /home/focusdeck/FocusDeck/publish/appsettings.Production.json | grep -A 5 Jwt)
```

---

**Last Updated:** November 15, 2025  
**Server Version:** Phase 1 (privacy controls with enhanced error logging)
