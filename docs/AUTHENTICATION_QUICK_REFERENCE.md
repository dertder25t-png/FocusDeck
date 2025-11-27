# 🔐 FocusDeck Authentication - Quick Reference

**TL;DR**: Login at `/login`, all protected routes auto-redirect if not authenticated. That's it!

---

## 📍 Key Routes

| Route | Public? | Purpose |
|-------|---------|---------|
| `/login` | ✅ Yes | Sign in page |
| `/register` | ✅ Yes | Create account |
| `/` | 🔐 No | Dashboard (protected) |
| `/lectures`, `/focus`, `/notes`, etc. | 🔐 No | All app features (protected) |
| `/swagger` | ✅ Yes | API documentation |
| `/v1/auth/*` | ✅ Yes | Auth endpoints |

---

## 🔄 User Journey

```
1. User visits focusdeck.909436.xyz/
   ↓
2. If logged out → Auto-redirect to /login
3. If logged in → Shows dashboard
   ↓
4. User enters credentials on /login
   ↓
5. Success → Token saved to localStorage
6. Token in localStorage → Can access all /app routes
7. Logout → Token cleared, back to /login
```

---

## 👨‍💻 Developer Workflow

### Check if User is Logged In

```typescript
// Check localStorage
const token = localStorage.getItem('focusdeck_access_token')
const isLoggedIn = token && !isTokenExpired(token)

// Or use getAuthToken() helper
import { getAuthToken } from './lib/utils'
try {
  const token = await getAuthToken()
  console.log('User is authenticated')
} catch (err) {
  console.log('User is not authenticated')
}
```

### Make API Calls

```typescript
// Use apiFetch instead of plain fetch
// It automatically adds Bearer token
import { apiFetch } from './lib/utils'

const response = await apiFetch('/v1/lectures', {
  method: 'GET'
})

const data = await response.json()
```

### Redirect User to Login

```typescript
// In component
import { useNavigate } from 'react-router-dom'

const navigate = useNavigate()

// Redirect to login, with current page as return URL
navigate('/login?redirectUrl=' + window.location.pathname)

// Or simple logout
import { logout } from './lib/utils'
logout() // Clears tokens and redirects to /login
```

### Protect a Component

```typescript
// Already protected by ProtectedRoute wrapper
// No need to do anything!

// Component inside AppLayout will never render
// unless user is authenticated
export function MyFeature() {
  return <div>User is definitely logged in here</div>
}
```

---

## 🔑 Token Management

### Where Tokens Are Stored

```typescript
localStorage.getItem('focusdeck_access_token')    // JWT for API calls
localStorage.getItem('focusdeck_refresh_token')   // For renewing access token
localStorage.getItem('focusdeck_user_id')         // Current user
```

### Token Format

```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.
eyJzdWIiOiJ1c2VyLWlkIiwiaWF0IjoxNjk0NDU1MDAwLCJleHAiOjE2OTQ0NTg2MDB9.
[signature]
```

Decode with: `JSON.parse(atob(token.split('.')[1]))`

### Clearing Tokens (Logout)

```typescript
localStorage.removeItem('focusdeck_access_token')
localStorage.removeItem('focusdeck_refresh_token')
localStorage.removeItem('focusdeck_user_id')
window.location.href = '/login'
```

---

## 🚨 Error States

### "Unauthorized" on API Call

**Problem**: Token is expired or invalid

**Solution**:
```typescript
if (response.status === 401) {
  // Token invalid, redirect to login
  window.location.href = '/login?redirectUrl=' + window.location.pathname
}
```

### "Redirects to /login infinitely"

**Problem**: Token in localStorage is corrupted

**Solution**:
```typescript
// Clear and refresh
localStorage.removeItem('focusdeck_access_token')
window.location.reload()
```

### "Login page appears but can't submit form"

**Problem**: Server auth endpoint not responding

**Debug**:
```bash
# Check if server is running
curl https://focusdeck.909436.xyz/healthz

# Check if auth endpoint is accessible
curl -X POST https://focusdeck.909436.xyz/v1/auth/pake/login/start

# Check browser console for errors
# Ctrl+Shift+I → Console tab
```

---

## 🧠 PAKE Flow in a Nutshell

### Registration (`/v1/auth/pake/register/*`)
- `register/start` returns SRP metadata plus Argon2id KDF parameters for modern clients; legacy web accounts still get SHA256 salts.
- `register/finish` saves verifiers + KDF metadata to `PakeCredentials`, records optional vault blobs in `KeyVaults`, and logs the attempt in `AuthEventLogs`.

### Login (`/v1/auth/pake/login/*`)
- `login/start` mirrors the stored KDF (Argon2 metadata first, salt fallback for legacy accounts) and returns server ephemerals plus the session ID.
- `login/finish` validates the proof, issues a JWT with `app_tenant_id`, persists refresh tokens/fingerprints to `RefreshTokens`, and writes successes/failures to `AuthEventLogs`. `RevokedAccessTokens` holds retired JWT IDs, while `PairingSessions` manages QR/device handoffs.

## 🧭 Tenant Flow

- `TenantMembershipService` makes sure every identity maps to a `Tenant` + `UserTenant`; first logins bootstrap a tenant (e.g., `user@example.com's Space`) and keep `TenantUser` metadata in sync.
- Use `/v1/tenants/current` to read the active tenant, `/v1/tenants` to list memberships, and `/v1/tenants/{id}/switch` to mint new tokens scoped to the selected `app_tenant_id`.
- `AuthenticationMiddleware` validates JWTs (header or SPA cookie) before serving protected routes and forbids requests that lack the `app_tenant_id` claim.

## 🧾 Auth Storage Snapshot

- `PakeCredentials`: SRP verifiers + KDF blobs per user.
- `KeyVaults`: Encrypted vault payloads + cipher/KDF metadata for restoration.
- `AuthEventLogs`: Structured audit of every register/login/pair/tenant event.
- `PairingSessions`: QR/device pairing workflow state.
- `RevokedAccessTokens`: Blacklisted `jti`s for logout/rotation.
- `RefreshTokens`: Client fingerprints, device info, expiration, tenant association for refresh flows.

## ⚙️ Server Configuration

### Enable Authentication Middleware

In `Program.cs`:

```csharp
// After auth/authz middleware
app.UseAuthentication();
app.UseAuthorization();
app.UseAuthenticationMiddleware(); // ← Add this line
```

### Protect API Endpoint

```csharp
[HttpGet("my-data")]
[Authorize] // ← This marks endpoint as protected
public async Task<IActionResult> GetMyData()
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    return Ok(new { userId });
}
```

### Allow Public API Endpoint

```csharp
[HttpPost("login")]
[AllowAnonymous] // ← Explicitly allow without auth
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    // ... auth logic
}
```

---

## 🧪 Quick Test

### Test Unauthenticated Access

```bash
# Should redirect to /login
curl -I https://focusdeck.909436.xyz/

# Should return HTML (login page)
curl https://focusdeck.909436.xyz/login
```

### Test API Authentication

```bash
# Should fail (no token)
curl https://focusdeck.909436.xyz/v1/lectures

# Should fail (invalid token)
curl -H "Authorization: Bearer invalid" https://focusdeck.909436.xyz/v1/lectures

# Should work (valid token)
curl -H "Authorization: Bearer eyJhbGc..." https://focusdeck.909436.xyz/v1/lectures
```

---

## 📊 Deployment Checklist

Before deploying to production:

- [ ] Environment variables set (`JWT__Key`, etc.)
- [ ] React app built: `npm run build`
- [ ] Build copied to wwwroot: `cp dist/* ../FocusDeck.Server/wwwroot/`
- [ ] Server compiled: `dotnet build -c Release`
- [ ] Health check passes: `curl /healthz`
- [ ] Login page loads: `curl /login`
- [ ] API accessible: `curl /v1/health`
- [ ] Unauthenticated users redirect: `curl /` → 302 to /login
- [ ] Authenticated users can access app

---

## 🔗 Related Files

```
src/FocusDeck.Server/
  ├── Middleware/
  │   └── AuthenticationMiddleware.cs      ← Server auth logic
  ├── Program.cs                            ← Middleware setup
  └── Controllers/v1/
      └── AuthController.cs                 ← Login endpoints

src/FocusDeck.WebApp/src/
  ├── App.tsx                               ← Route definitions
  ├── pages/Auth/
  │   ├── LoginPage.tsx                     ← Login UI
  │   └── ProtectedRoute.tsx                ← Route protection
  └── lib/
      └── utils.ts                          ← Token helpers
```

---

## ❓ FAQ

**Q: How do I test locally?**  
A: Use `dotnet watch run` for the server and `npm run dev` for the React app. Server on `http://localhost:5000`, React on `http://localhost:5173`.

**Q: Can I use different auth method (OAuth, etc.)?**  
A: The middleware doesn't care how you authenticate - it just checks for a valid JWT token. You can replace the login endpoint with any OAuth flow.

**Q: How long do tokens last?**  
A: Check `JWT__AccessTokenExpirationMinutes` config (default 60 minutes) and `JWT__RefreshTokenExpirationDays` (default 7 days).

**Q: What if user is logged in but token expires?**  
A: They'll see the /login page next time they load. Implement auto-refresh using `refreshToken` endpoint for seamless experience.

**Q: Can I allow public access to some pages?**  
A: Add to `IsPublicRoute()` in AuthenticationMiddleware.cs and mark React routes with no `<ProtectedRoute>` wrapper.

---

**Last Updated**: November 11, 2025  
**Status**: ✅ Ready for Production
