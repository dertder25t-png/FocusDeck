# FocusDeck Auth & UI Redirect Fix - Summary

## Issues Fixed ✅

### 1. **Two Different UIs**
- **Problem:** Old UI at `/` and new UI at `/app`
- **Root Cause:** Stale wwwroot files from previous builds
- **Fix:** 
  - Server already redirects `/app/*` → `/`
  - BuildSpa target in .csproj now properly cleans wwwroot before copying new build
  - Linux server cleanup removes all old artifacts

### 2. **No Login Requirement**
- **Problem:** Users could access `https://focusdeckv1.909436.xyz/` without being prompted to login
- **Root Cause:** ProtectedRoute wasn't properly validating JWT on page load
- **Fix:**
  - Enhanced ProtectedRoute with JWT expiry validation
  - Checks localStorage for `focusdeck_access_token`
  - Validates token isn't expired before granting access
  - Shows loading spinner while checking
  - Redirects to `/login` if token missing or invalid

## Changes Made

### 1. ProtectedRoute.tsx (Enhanced)
**File:** `src/FocusDeck.WebApp/src/pages/Auth/ProtectedRoute.tsx`

**Changes:**
```tsx
// Before: Simple token existence check
const isAuthed = !!token

// After: JWT expiry validation + better UX
const isValid = !!(token && token.length > 0 && !isTokenExpired(token))
setIsAuthed(isValid)

// Added:
✓ isTokenExpired() function to decode JWT payload and check exp claim
✓ Better loading UI with spinner animation
✓ Console logging for debugging authentication flow
✓ Verification message when token is invalid
```

**Impact:**
- ✅ Expired tokens no longer grant access
- ✅ Invalid tokens are caught immediately
- ✅ Users see clear loading state
- ✅ Developers can debug via console

### 2. FocusDeck.Server.csproj (Already Fixed)
**File:** `src/FocusDeck.Server/FocusDeck.Server.csproj`

**Changes:**
```xml
<!-- Added: Disable problematic static asset compression -->
<CompressionLevel>never</CompressionLevel>

<!-- Improved: BuildSpa target with better error handling -->
<Exec ... ContinueOnError="false" />  <!-- Fails fast on npm errors -->
<Message Text="BuildSpa Target: Successfully copied..." /> <!-- Logging -->
```

**Impact:**
- ✅ Build no longer fails on asset compression
- ✅ npm build failures are caught immediately
- ✅ Clear visibility into what was built

## Request Flow - After Fix

```
User visits: https://focusdeckv1.909436.xyz/

    ↓

Server (Program.cs):
  GET / → serves index.html
  GET /app/* → redirects to /
  GET /v1/auth/login → API (unprotected, for login)
  GET /v1/dashboard → API (protected, requires [Authorize])

    ↓

Browser (React App):
  App.tsx loads
  BrowserRouter initializes
  Routes checks path:
    - /login → LoginPage (public)
    - /register → RegisterPage (public)
    - /pairing → PairingPage (public)
    - /* (everything else) → ProtectedRoute

    ↓

ProtectedRoute checks:
  ✓ Read localStorage['focusdeck_access_token']
  ✓ Validate JWT isn't expired
  ✓ Show loading spinner while checking
  
  IF no token or expired:
    → Navigate to /login (user logs in)
    → Token stored in localStorage
    → Page reloads
    → ProtectedRoute now grants access
  
  IF token valid:
    → Render <Outlet /> (show dashboard/pages)

    ↓

User sees:
  - LoginPage if NOT authenticated
  - Dashboard + Sidebar if authenticated
```

## Deployment Instructions

**See:** `LINUX_AUTH_REDIRECT_SETUP.md` for detailed step-by-step instructions

**Quick version:**
```bash
cd ~/FocusDeck
git pull origin phase-1

# Clean old builds
rm -rf src/FocusDeck.Server/{obj,bin,wwwroot} src/FocusDeck.WebApp/dist

# Build
dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained -o ~/focusdeck-server-build

# Deploy
sudo systemctl stop focusdeck
sudo cp -r ~/focusdeck-server-build/* /opt/focusdeck/
sudo systemctl start focusdeck

# Verify
curl http://localhost:5000/healthz
```

## Testing Checklist

### ✓ Test 1: Anonymous user → redirected to login
```
1. Open https://focusdeckv1.909436.xyz/
2. Should see LoginPage
3. URL should be https://focusdeckv1.909436.xyz/login
```

### ✓ Test 2: Old /app path redirects
```
1. Open https://focusdeckv1.909436.xyz/app
2. Should 301 redirect to https://focusdeckv1.909436.xyz/
3. Then redirected to login
```

### ✓ Test 3: Login flow works
```
1. Fill in email/password on login page
2. Click "Sign In"
3. Token stored in localStorage
4. Redirect to dashboard
5. See sidebar + dashboard content
```

### ✓ Test 4: Protected pages require auth
```
1. Logout (clear localStorage)
2. Try to access https://focusdeckv1.909436.xyz/lectures
3. Should redirect to login
4. After login, should show /lectures
```

### ✓ Test 5: Expired token detection
```
Browser console:
  localStorage.setItem('focusdeck_access_token', 'eyJ...(expired)...') 
  location.reload()
  Should redirect to login
  Should see "No valid authentication token found" in console
```

## GitHub Commits

1. **514699a** - Improve: Enhance ProtectedRoute authentication flow with JWT expiry validation
   - JWT expiry validation
   - Better loading UI
   - Console logging for debugging

2. **256dde6** - Fix: Disable static asset compression and improve WebApp build target
   - CompressionLevel=never
   - BuildSpa error handling

3. **5bdf9c6** - Refactor: Reorganize auth onboarding UI
   - Moved LoginPage, RegisterPage, PairingPage to Auth folder
   - Added ProtectedRoute component

## Files Modified

| File | Changes | Impact |
|------|---------|--------|
| `src/FocusDeck.WebApp/src/pages/Auth/ProtectedRoute.tsx` | +29 lines | JWT validation, better UX |
| `src/FocusDeck.Server/FocusDeck.Server.csproj` | +5 lines | Disable compression, improve build |
| `Program.cs` | No changes needed | Already has correct routing |
| `App.tsx` | No changes needed | Already uses ProtectedRoute |

## Architecture

### Before
```
/ → serves old index.html → no auth check → sees random UI
/app → redirects to / → same issue
```

### After
```
/ → serves new index.html → ProtectedRoute checks auth
  → no token → LoginPage
  → valid token → Dashboard (AppLayout)
  
/app → redirects to / → same flow

/v1/* → API (protected by [Authorize])
/login → public (no ProtectedRoute)
/register → public (no ProtectedRoute)
```

## Key Improvements

1. **No More Old UI** - Only new React UI served from /
2. **Forced Login** - Anonymous users cannot access any protected pages
3. **Smart Redirects** - After login, return users to requested page
4. **Better Debugging** - Console logs show auth flow issues
5. **Token Validation** - Expired tokens rejected immediately
6. **Clean Builds** - BuildSpa target ensures fresh wwwroot

## Next Steps (Optional)

- [ ] Add toast notifications for login errors (toast.error("Invalid credentials"))
- [ ] Add Playwright tests for auth redirect flow
- [ ] Monitor for failed login attempts in logs
- [ ] Add "remember me" functionality to LoginPage
- [ ] Add password reset flow
- [ ] Add social login (Google, GitHub)
