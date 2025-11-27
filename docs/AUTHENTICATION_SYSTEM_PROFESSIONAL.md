# FocusDeck Authentication System - Professional Implementation

**Status**: ✅ Complete and Production-Ready  
**Date**: November 2025  
**Version**: 1.0

---

## 🎯 Overview

The FocusDeck authentication system has been completely overhauled to provide a **professional, clean, and secure** login flow. This eliminates the previous confusing setup where:

- ❌ Old UI at root (`/`)
- ❌ Modern UI at `/app`
- ❌ No redirect for unauthenticated users
- ❌ Multiple overlapping login pages

**Now the system is professional and unified:**

- ✅ **Single login page** at `/login`
- ✅ **Protected root path** (`/`) - redirects to login if not authenticated
- ✅ **Modern SPA** served at `/` for authenticated users
- ✅ **Smart redirects** - users return to their requested page after login

---

## 🏗️ Architecture

### Component Stack

```
Browser Request
    ↓
StaticFiles Middleware (serve index.html, assets)
    ↓
AuthenticationMiddleware (NEW - validates tokens)
    ↓
CORS Middleware
    ↓
Authentication Scheme (JWT Bearer)
    ↓
Authorization Attributes ([Authorize])
    ↓
Endpoint (API or SPA Route)
```

### Key Components

#### 1. **AuthenticationMiddleware** (`Server/Middleware/AuthenticationMiddleware.cs`)

Enforces authentication at the server level for all UI routes.

**Responsibilities:**
- Allows public routes through: `/login`, `/register`, `/auth/*`
- Allows API routes through: `/v1/*`, `/swagger/*`, `/health/*`
- Allows static assets through: `.js`, `.css`, `.json`, `.png`, etc.
- Protects all other UI routes
- Validates JWT tokens for protected routes
- Redirects unauthenticated requests to `/login?redirectUrl=...`

**Flow:**
```typescript
if (path is public OR has valid JWT)
  → Pass to next middleware
else if (path is protected UI route)
  → Redirect to /login?redirectUrl={original_path}
else
  → Pass to next middleware (API will handle auth)
```

#### 2. **Professional Login Page** (`WebApp/src/pages/Auth/LoginPage.tsx`)

Modern, user-friendly login interface.

**Features:**
- Gradient background with visual polish
- Real-time form validation
- Inline error messages
- Clear error states
- Responsive mobile design
- Loading indicators
- "Forgot Password" placeholder for future implementation
- Professional typography and spacing

#### 3. **Protected Route Guard** (`WebApp/src/pages/Auth/ProtectedRoute.tsx`)

React Router component that enforces authentication at the client level.

**Features:**
- Checks localStorage for valid JWT token
- Shows loading spinner during verification
- Validates token structure and expiration
- Passes `?redirectUrl=` query param to login page
- Returns user to original page after successful login

#### 4. **SPA Routing** (`WebApp/src/App.tsx`)

Clean, organized route structure.

**Public Routes:**
- `/login` - Sign in page
- `/register` - Registration page

**Protected Routes:**
- `/` - Dashboard (requires auth)
- `/lectures` - Lecture management
- `/focus` - Focus sessions
- `/notes` - Study notes
- `/design` - Design assistant
- `/analytics` - Productivity analytics
- `/settings` - User settings
- `/devices` - Device management
- `/pairing` - Device pairing
- `/provisioning` - Device provisioning
- `/tenants` - Tenant management (admin)
- `/jobs` - Background jobs (admin)

---

## 🔑 Authentication Flow

### Initial Login

```
1. User visits https://focusdeck.909436.xyz/
   ↓
2. ProtectedRoute checks localStorage for token
   ↓
3. No token found → Redirect to /login
   ↓
4. User enters credentials
   ↓
5. LoginPage calls PAKE auth endpoint
   ↓
6. Tokens stored in localStorage
   ↓
7. Redirect to originally requested page (or /)
   ↓
8. AppLayout renders with sidebar and content
```

### Accessing Protected Page While Logged Out

```
1. User visits https://focusdeck.909436.xyz/lectures
   ↓
2. AuthenticationMiddleware checks authorization
   ↓
3. No valid token → Redirect to /login?redirectUrl=/lectures
   ↓
4. ProtectedRoute also checks localStorage
   ↓
5. User logs in → Redirects to /lectures (via redirectUrl param)
```

### Accessing Protected Page While Logged In

```
1. User visits https://focusdeck.909436.xyz/lectures
   ↓
2. AuthenticationMiddleware validates JWT token
   ↓
3. Token valid → Pass to next middleware
   ↓
4. ProtectedRoute checks localStorage
   ↓
5. Token valid → Render <Outlet />
   ↓
6. React Router renders LecturesPage
```

### Token Refresh

Currently implemented in `lib/utils.ts` via `apiFetch` helper:

```typescript
export async function apiFetch(url: string, options: RequestInit = {}): Promise<Response> {
  const token = await getAuthToken();
  
  return fetch(url, {
    ...options,
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json',
      ...options.headers,
    },
  });
}
```

**Future Enhancement:** Implement automatic token refresh using `/v1/auth/refresh` endpoint when token nears expiration.

---

## 🔐 KDF Reality

- The shared `Srp` helper serves both Argon2id and legacy SHA256 derivation paths. New registrations capture Argon2 KDF blobs (`alg: "argon2id", t:3, m:65536, p:2, aad:true`) in `PakeCredentials`, while `NormalizeKdfParameters` forces SHA256 for accounts created before the Argon2 cutover so login routes always send viable metadata back to clients.
- Clients honor the returned metadata: desktop/mobile uses Argon2, browsers or legacy rows fall back to SHA256, and the `aad` flag keeps client/server private keys aligned so the "Invalid proof" error goes away.

## 🧾 Auth Data Tables

- `PakeCredentials`: SRP verifier, salt, modulus, generator, and serialized KDF parameters per user.
- `KeyVaults`: Encrypted vault payloads with cipher suite + KDF metadata attached.
- `AuthEventLogs`: Structured records capturing every PAKE register/login attempt, upgrade, tenant switch, and pairing flow.
- `PairingSessions`: QR/device pairing sessions with codes, vault blobs, and statuses tracked.
- `RefreshTokens`: Hashed refresh tokens with client fingerprints, device metadata, tenant ID, and expiration for `/v1/auth/refresh`.
- `RevokedAccessTokens`: Blacklisted JWT IDs (`jti`) for logout/rotation cases.

## 🧭 Tenant & Token Routing

- `TenantMembershipService` makes sure every authenticated identity maps to a `Tenant` + `UserTenant`; it updates existing memberships and only creates a new tenant/owner when no relationship exists.
- `/v1/tenants` triples (`/`, `/current`, `/\{id\}/switch`) let clients list memberships, read the active tenant, and rotate the `app_tenant_id` claim via a fresh JWT + refresh token pair.
- `AuthenticationMiddleware` shares the same `TokenValidationParameters` as the JWT bearer scheme, enforces the `app_tenant_id` claim for SPA requests, and redirects to `/login` whenever validation fails so the SPA always knows the user/tenant context.

## 🔍 Auth Observability Playbook

- **Check the counters**: monitor `auth.pake.register.failure`, `auth.pake.login.failure`, and `auth.jwt.validation.failure`. Each counter carries a `tenant_id` tag and (on failures) a `reason` tag such as `invalid-proof`, `missing-tenant`, or `blocked`, so you can correlate spikes with tenant-specific issues.
- **Use the log templates**: search Seq/your log store for `PAKE login finish`, `PAKE register finish`, or `Tenant switch` entries. These include masked user IDs, device metadata, and the upstream reason strings logged via `Track*` helpers.
- **Diagnose JWT problems**: if protected SPA routes keep redirecting, inspect `auth.jwt.validation.failure` along with log lines that read `JWT validation failed ({Reason}) for path {Path}`; the middleware emits the remote IP and tenant/claim context to guide triage.
- **Wire metrics to production stack**: ensure the `FocusDeck.Authentication` meter surfaces to your Prometheus/OpenTelemetry exporter so the counters above are visible on dashboards and alerting rules.

## 🚀 Deployment

### Prerequisites

```bash
✅ .NET 8.0+ runtime
✅ Node.js 18+ (for building React app)
✅ Build machine with 2GB+ RAM
```

### Build Steps

#### 1. Build React SPA

```bash
cd src/FocusDeck.WebApp
npm install
npm run build
```

Output: `dist/` folder with optimized assets

#### 2. Copy Build to Server

```bash
# From project root
cp -r src/FocusDeck.WebApp/dist/* src/FocusDeck.Server/wwwroot/
```

#### 3. Build and Deploy Server

```bash
cd src/FocusDeck.Server
dotnet build -c Release
dotnet publish -c Release -o ../../publish
```

#### 4. Deploy to Production

```bash
# Stop existing service
systemctl stop focusdeck

# Copy published files
cp -r ../../publish/* /opt/focusdeck/

# Restart service
systemctl start focusdeck

# Verify
curl https://focusdeck.909436.xyz/healthz
```

### Configuration

Set environment variables for production:

```bash
# JWT Configuration
JWT__Key=your-32-character-secret-key-here
JWT__Issuer=https://focusdeck.909436.xyz
JWT__Audience=focusdeck-clients

# Database
ConnectionStrings__DefaultConnection=Host=db.example.com;Database=focusdeck;User Id=postgres;Password=xxx

# Allowed Origins (CORS)
AllowedOrigins=https://focusdeck.909436.xyz,https://app.focusdeck.909436.xyz
```

---

## 🔒 Security Features

### Server-Side

1. **AuthenticationMiddleware**
   - Validates JWT tokens before rendering pages
   - Checks token expiration
   - Rejects malformed tokens
   - Handles edge cases gracefully

2. **JWT Bearer Validation**
   - Validates issuer and audience
   - Checks expiration with 2-minute clock skew
   - Verifies cryptographic signature
   - Supports multiple valid issuers for migration

3. **Rate Limiting**
   - Auth endpoints limited to 10 requests per minute per IP
   - Protects against brute force attacks

4. **CORS Policy**
   - Strict origin validation
   - Only allows configured origins
   - Explicit HTTP methods and headers

### Client-Side

1. **localStorage Token Storage**
   - Tokens stored with key `focusdeck_access_token`
   - Automatically included in API requests
   - Cleared on logout

2. **XSS Protection**
   - Content-Security-Policy headers
   - No unsafe inline scripts
   - Restricted frame embedding

3. **CSRF Protection**
   - SameSite cookie attribute (when cookies used)
   - CORS validation

---

## 📋 Testing Checklist

### Authentication Flows

- [ ] **Unauthenticated → Login**
  - [ ] Navigate to `/` while logged out
  - [ ] Redirected to `/login`
  - [ ] Enter valid credentials
  - [ ] Success → Redirect to `/`
  - [ ] Error message shows for invalid credentials

- [ ] **Protected Route → Login → Redirect Back**
  - [ ] Navigate to `/lectures` while logged out
  - [ ] URL shows `/login?redirectUrl=%2Flectures`
  - [ ] Log in successfully
  - [ ] Redirected to `/lectures` (not `/`)

- [ ] **Already Authenticated**
  - [ ] Log in once
  - [ ] Navigate to `/` again
  - [ ] No redirect (already authenticated)
  - [ ] Full page renders without delay

- [ ] **Token Expiration**
  - [ ] Log in
  - [ ] Wait for token to expire (or manually set localStorage token to expired JWT)
  - [ ] Try to load protected page
  - [ ] Redirected to `/login`

- [ ] **Logout**
  - [ ] While logged in, click logout button
  - [ ] localStorage tokens cleared
  - [ ] Redirected to `/login`

### API Routes

- [ ] **API without Auth**
  - [ ] `POST /v1/auth/pake/login/start` works without token
  - [ ] `POST /v1/auth/pake/login/finish` works without token

- [ ] **API with Valid Auth**
  - [ ] Protected endpoints return 200 with valid Bearer token
  - [ ] User data matches authenticated user

- [ ] **API without Valid Auth**
  - [ ] Protected endpoints return 401 without token
  - [ ] Protected endpoints return 401 with expired token
  - [ ] Protected endpoints return 401 with invalid token

### Static Assets

- [ ] Stylesheet loads
- [ ] JavaScript bundles load
- [ ] Images render correctly
- [ ] No 404s in browser console

### Edge Cases

- [ ] Navigate directly to `/login` while authenticated → Redirect to `/`
- [ ] Navigate to nonexistent route → Redirect to `/` → Then to `/login` if not authed
- [ ] Rapidly click multiple protected routes → No error state
- [ ] Network error during login → Show error message
- [ ] Very long redirect URL → Handles gracefully

---

## 🐛 Troubleshooting

### "Redirects to /login infinitely"

**Cause**: Token in localStorage is invalid or malformed

**Solution**:
```javascript
// In browser console
localStorage.removeItem('focusdeck_access_token')
window.location.reload()
```

### "Login page shows but form won't submit"

**Cause**: API endpoint not responding

**Solution**:
1. Check server is running: `curl https://focusdeck.909436.xyz/healthz`
2. Check logs: `journalctl -u focusdeck -f`
3. Verify CORS: Check browser console for CORS errors
4. Check PAKE endpoint: `curl -X POST https://focusdeck.909436.xyz/v1/auth/pake/login/start`

### "Get redirected to /login after successful login"

**Cause**: Token not properly stored or JWT validation failing

**Solution**:
1. Check `storeTokens` function called correctly
2. Verify token in localStorage: `localStorage.getItem('focusdeck_access_token')`
3. Decode token: `JSON.parse(atob(token.split('.')[1]))`
4. Check expiration: `new Date(payload.exp * 1000)`

### "API works but page shows login"

**Cause**: Server not returning index.html for SPA routes

**Solution**:
1. Check `wwwroot/index.html` exists
2. Verify `app.UseDefaultFiles()` in Program.cs
3. Check static file middleware order
4. Rebuild React: `npm run build && cp dist/* ../FocusDeck.Server/wwwroot/`

---

## 📝 Development Tips

### Hot Reload During Development

```bash
# Terminal 1: Start backend
cd src/FocusDeck.Server
dotnet watch run

# Terminal 2: Start React dev server
cd src/FocusDeck.WebApp
npm run dev
```

Visit `http://localhost:5173` (React dev server)

### Debug Tokens

```typescript
// In browser console
const token = localStorage.getItem('focusdeck_access_token')
const payload = JSON.parse(atob(token.split('.')[1]))
console.log('Decoded token:', payload)
console.log('Expires:', new Date(payload.exp * 1000))
```

### Test Unauthenticated

Clear tokens and reload:

```typescript
// In browser console
localStorage.removeItem('focusdeck_access_token')
localStorage.removeItem('focusdeck_refresh_token')
window.location.href = '/'
```

---

## 🔄 Migration from Old System

If you had users accessing the old scattered UI:

### Old URLs → New URLs

| Old Path | New Path | Notes |
|----------|----------|-------|
| `/app/` | `/` | Main app is now at root |
| `/app/login` | `/login` | Single unified login page |
| `/admin/...` | `/settings` | Admin features now in settings |
| `/old-ui/` | Deprecated | No longer available |

### User Communication

Send email to users:

> **Important: FocusDeck Login Change**
>
> We've improved the FocusDeck login system for a better experience!
>
> **What changed:**
> - Single, unified login page at focusdeck.909436.xyz
> - All app features under one clear interface
> - Better security and faster load times
>
> **No action needed:** Your existing password works the same way.
> Just visit focusdeck.909436.xyz and sign in normally.

---

## 🚦 Next Steps

### Planned Improvements

1. **Password Reset** - Implement "Forgot Password" flow
2. **OAuth Integration** - Google/Microsoft Sign-in
3. **Two-Factor Authentication** - Enhanced security
4. **Session Management** - Visible active sessions
5. **Audit Logs** - Track login/logout events
6. **Device Trust** - Remember trusted devices

### Code References

- **Middleware**: `/src/FocusDeck.Server/Middleware/AuthenticationMiddleware.cs`
- **Login Page**: `/src/FocusDeck.WebApp/src/pages/Auth/LoginPage.tsx`
- **Protected Route**: `/src/FocusDeck.WebApp/src/pages/Auth/ProtectedRoute.tsx`
- **App Routing**: `/src/FocusDeck.WebApp/src/App.tsx`
- **API Helpers**: `/src/FocusDeck.WebApp/src/lib/utils.ts`
- **Server Config**: `/src/FocusDeck.Server/Program.cs` (lines 565-575)

---

## 📞 Support

For questions or issues:

1. Check troubleshooting section above
2. Review server logs: `journalctl -u focusdeck -f`
3. Check browser console for errors
4. Verify configuration matches documentation
5. Create GitHub issue with logs and steps to reproduce

---

**Last Updated**: November 11, 2025  
**Status**: Production Ready ✅
