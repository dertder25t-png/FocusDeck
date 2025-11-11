# Linux Server: Fix UI & Authentication Redirect

## Problem Analysis
You have two issues:
1. **Old UI visible at `/` while new UI is at `/app`** - The wwwroot folder has leftover files from an older build
2. **No login prompt** - Users can access `/` without being forced to login

## Solution Overview

The fix involves:
1. Pulling the latest code from GitHub (phase-1 branch)
2. Cleaning old wwwroot files
3. Rebuilding the server to generate clean wwwroot with new UI
4. Testing the flow: `/` → `/login` → authenticated user sees dashboard

## Step-by-Step Instructions

### 1. Pull Latest Code
```bash
cd ~/FocusDeck
git pull origin phase-1
```

**Recent commits:**
- ✅ Enhanced ProtectedRoute with JWT expiry validation
- ✅ Fixed static asset compression in server build
- ✅ Improved auth flow with better logging

### 2. Clean Old Build Artifacts
```bash
# Remove old builds
rm -rf src/FocusDeck.Server/obj
rm -rf src/FocusDeck.Server/bin
rm -rf src/FocusDeck.WebApp/dist
rm -rf src/FocusDeck.Server/wwwroot

# Remove any cached node modules (optional but safe)
rm -rf src/FocusDeck.WebApp/node_modules/.cache
```

### 3. Build Release Binary
```bash
cd ~/FocusDeck
dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained -o ~/focusdeck-server-build
```

**What this does:**
- npm ci installs WebApp dependencies
- npm run build compiles new React UI
- BuildSpa target copies dist/ to wwwroot/
- dotnet build creates self-contained server binary

### 4. Stop Current Server
```bash
sudo systemctl stop focusdeck
```

### 5. Deploy New Build
```bash
sudo cp -r ~/focusdeck-server-build/* /opt/focusdeck/
sudo chown -R focusdeck:focusdeck /opt/focusdeck/
```

### 6. Start Server
```bash
sudo systemctl start focusdeck
```

### 7. Verify Logs
```bash
sudo journalctl -u focusdeck -f
```

You should see:
```
[INFO] Starting FocusDeck Server
[INFO] User Service initialized
[INFO] Health checks registered
```

### 8. Test the Flow

#### Test 1: Root redirects to login
```bash
curl -i http://localhost:5000/
# You should see a 200 OK with index.html containing React app
```

Then open browser to `http://localhost:5000/` or `https://focusdeckv1.909436.xyz/`

**Expected behavior:**
- ✅ Page loads the new UI
- ✅ ProtectedRoute checks for token in localStorage
- ✅ No token exists → redirect to `/login` page
- ✅ Login page displays with form

#### Test 2: Verify /app redirects
```bash
curl -i http://localhost:5000/app
# Should get 301 permanent redirect to /
```

#### Test 3: Login works
1. Go to login page: `https://focusdeckv1.909436.xyz/login`
2. Enter credentials (or register new account)
3. Submit form
4. Token stored in localStorage
5. Redirect to `/` (dashboard)
6. ProtectedRoute sees token → shows dashboard

#### Test 4: Access other pages
- Try going to `/lectures`, `/focus`, `/notes`, etc.
- Each should require login redirect if no token
- After login, should display the page

### 9. Verify wwwroot is Clean
```bash
ls -la src/FocusDeck.Server/wwwroot/
ls -la src/FocusDeck.Server/wwwroot/assets/
```

You should see:
```
index.html              (new React app)
assets/index-{hash}.js  (minified React + dependencies)
assets/index-{hash}.css (Tailwind CSS)
```

**NOT:** Old files like `old-app.js`, `dashboard.html`, etc.

## Architecture Flow

```
Request: https://focusdeckv1.909436.xyz/

    ↓

Server Routes:
  /v1/*          → API endpoints (protected by [Authorize])
  /hubs/*        → SignalR hubs (WebSocket)
  /swagger       → Swagger UI
  /app/*         → Redirect to /
  /               → Serve index.html (SPA entry point)

    ↓

Browser loads index.html with React + ProtectedRoute

    ↓

App.tsx initializes:
  - BrowserRouter sets up client-side routing
  - Routes defined: /login, /register, /pairing, /app/*
  - ProtectedRoute wraps protected routes

    ↓

ProtectedRoute checks:
  - localStorage for 'focusdeck_access_token'
  - Validates JWT expiry
  
  If NO token → Navigate to /login
  If token valid → Render <Outlet /> (dashboard, lectures, etc.)

    ↓

User sees:
  - LoginPage if not authenticated
  - AppLayout (sidebar + top bar) if authenticated
```

## Troubleshooting

### Issue: Still seeing old UI after restart
```bash
# Force clear everything
sudo systemctl stop focusdeck
rm -rf src/FocusDeck.Server/obj
rm -rf src/FocusDeck.Server/bin
rm -rf src/FocusDeck.Server/wwwroot
rm -rf src/FocusDeck.WebApp/dist

# Rebuild from scratch
dotnet clean
dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained

# Deploy and restart
sudo systemctl start focusdeck
```

### Issue: Login page shows but doesn't redirect
Check browser console:
1. Press F12 (Dev Tools)
2. Go to Console tab
3. Look for messages like:
   - `"No valid authentication token found"` → Good, redirect working
   - Network errors on `/v1/auth/login` → API not responding

### Issue: Login succeeds but still stuck on login page
1. Verify token is in localStorage:
   ```js
   // In browser console
   localStorage.getItem('focusdeck_access_token')
   ```
2. If empty, check API response for token in `/v1/auth/login`
3. Check that token is valid JWT (can decode at jwt.io)

### Issue: "chunk size >500 kB" warning
This is just a warning, not an error. The app still works.

To fix (optional):
- Split React code into separate chunks in vite.config.ts
- Move heavy dependencies to separate bundles

## What Changed

### ProtectedRoute.tsx (Enhanced)
```tsx
// Now:
✓ Checks JWT expiry before granting access
✓ Shows loading spinner while checking
✓ Logs auth failures to console
✓ Redirects to /login with "from" state for return-after-auth
```

### Program.cs (Routes)
```csharp
// Root (/) → serves index.html with SPA
// /app/* → redirects to /
// /v1/* → API endpoints
// /{anything-else} → 404 unless it's an asset
```

### BuildSpa Target (Fixed)
```xml
<!-- Automatically runs during Release build -->
<!-- Executes: npm ci + npm run build -->
<!-- Copies dist/ to wwwroot/ -->
<!-- Reports success/failure -->
```

## Next Steps

1. ✅ Deploy this build to Linux server
2. ✅ Test login redirect flow works
3. ✅ Consider adding toast notifications for login errors
4. ✅ Add Playwright tests for auth flow coverage
5. ⏳ Set up monitoring for login failures

## Commands Summary
```bash
# Full deployment in one command
cd ~/FocusDeck && \
git pull origin phase-1 && \
rm -rf src/FocusDeck.Server/obj src/FocusDeck.Server/bin src/FocusDeck.WebApp/dist src/FocusDeck.Server/wwwroot && \
dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained -o ~/focusdeck-server-build && \
sudo systemctl stop focusdeck && \
sudo cp -r ~/focusdeck-server-build/* /opt/focusdeck/ && \
sudo chown -R focusdeck:focusdeck /opt/focusdeck/ && \
sudo systemctl start focusdeck && \
echo "✓ Deployment complete. Checking status..." && \
sleep 2 && \
curl -s http://localhost:5000/healthz | jq '.'
```

This runs the entire pipeline from pull to deploy to health check!
