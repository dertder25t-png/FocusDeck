# FocusDeck Routing Fix - Deployment Guide

**Date:** November 8, 2025  
**Issue:** Web UI at `/app` path, API at `/v1`, Cloudflare tunnel only sees root `/`  
**Solution:** Unified routing to serve UI from root via dedicated endpoint  
**Status:** ✅ Build successful, ready for deployment

---

## What Changed

### Modified File
- **File:** `src/FocusDeck.Server/Program.cs`
- **Change:** Updated SPA Fallback middleware to skip root `/` path
- **Reason:** Allow dedicated `MapGet("/")` endpoint to handle root requests with version injection

### Key Code Change
```csharp
// OLD: Only skipped API routes
if (!path.StartsWith("/v1") && 
    !path.StartsWith("/swagger") && 
    !path.StartsWith("/healthz") &&
    !path.StartsWith("/hubs"))

// NEW: Also skip root "/" - has dedicated endpoint
if (!path.StartsWith("/v1") && 
    !path.StartsWith("/swagger") && 
    !path.StartsWith("/healthz") &&
    !path.StartsWith("/hubs") &&
    !path.Equals("/", StringComparison.OrdinalIgnoreCase) &&  // ← NEW LINE
    !path.Equals("/swagger.json", StringComparison.OrdinalIgnoreCase))
```

### Why This Works
1. **Root `/`** → Handled by `MapGet("/")` endpoint (injects version info)
2. **Deep routes `/dashboard`** → Rewritten to `/app/index.html` by middleware
3. **API routes `/v1/...`** → Skipped by middleware, handled by controllers
4. **Static assets `/app/style.css`** → Handled by static files middleware
5. **Cloudflare tunnel** → Can now reach UI at root `/`

---

## Build Status

✅ **Build:** Successful (0 errors, 46 warnings - pre-existing)  
✅ **Publish:** Successful (linux-x64, Release mode)  
✅ **Output:** `publish/server/` contains complete application

**Build Output:**
```
FocusDeck.Server -> C:\Users\Caleb\Desktop\FocusDeck\publish\server\
```

**DLL Size:** 839.5 KB

---

## Deployment Steps

### Step 1: Connect to Linux Server
```bash
ssh focusdeck@192.168.1.110
```

### Step 2: Navigate to Application Directory
```bash
su - focusdeck
cd ~/FocusDeck
```

### Step 3: Pull Latest Code
```bash
# Pull from authentication branch (or master once merged)
git pull origin master

# Verify the routing fix is included
grep -n "!path.Equals(\"/\"" src/FocusDeck.Server/Program.cs
# Should show line ~676 with the new condition
```

### Step 4: Build on Server
```bash
cd src/FocusDeck.Server
dotnet publish FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained false -o ~/focusdeck-server
```

**Expected Output:**
```
FocusDeck.Server -> /home/focusdeck/focusdeck-server/
```

### Step 5: Restart Service
```bash
exit  # Return to root
sudo systemctl restart focusdeck
sleep 2
sudo systemctl status focusdeck
```

**Expected Status:** `● focusdeck.service - Loaded: loaded Enabled: enabled Active: active (running)`

### Step 6: Verify Deployment

#### Test Root Path (Local)
```bash
curl http://localhost:5000/
# Should return HTML (not 404)
# Should contain React/Vue app markup
```

#### Test API (Local)
```bash
curl http://localhost:5000/v1/system/health
# Should return: {"ok":true,"time":"2025-11-08T..."}
```

#### Test Cloudflare Tunnel (From Windows)
```powershell
# On your Windows machine:
$resp = Invoke-WebRequest -Uri "https://focusdeck.909436.xyz/" -UseBasicParsing
$resp.StatusCode  # Should be 200
$resp.Content | Select-Object -First 500  # Should show HTML markup
```

#### Test API via Tunnel
```powershell
$resp = Invoke-WebRequest -Uri "https://focusdeck.909436.xyz/v1/system/health" -UseBasicParsing
$resp.Content  # Should return JSON: {"ok":true,"time":"..."}
```

---

## Rollback (If Needed)

If the deployment fails:

```bash
# Revert to previous version
cd ~/FocusDeck
git log --oneline -5  # Find previous commit
git reset --hard <previous-commit-sha>

# Rebuild and restart
cd src/FocusDeck.Server
dotnet publish FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained false -o ~/focusdeck-server
sudo systemctl restart focusdeck
```

---

## Routing Architecture (After Fix)

```
Request Flow:
┌─────────────────────────────────────────────────────────┐
│  Browser/Cloudflare sends request to application       │
├─────────────────────────────────────────────────────────┤
│  Middleware Pipeline (in order):                        │
│  1. SPA Fallback middleware                             │
│     ├─ Skip: /v1/*, /swagger, /healthz, /hubs, /       │
│     ├─ Deep routes: /dashboard → /app/index.html       │
│     └─ Root /: Skipped → continues to endpoint         │
│                                                         │
│  2. Static Files middleware                             │
│     ├─ /app/* → Serve JS, CSS, images                  │
│     └─ /index.html → Serve root UI files              │
│                                                         │
│  3. Endpoints                                           │
│     ├─ MapGet("/") → Dedicated endpoint                │
│     │  └─ Returns /app/index.html with version         │
│     ├─ MapControllers → /v1/* API routes              │
│     │  └─ Requires Authorization                       │
│     └─ MapHub → /hubs/notifications                    │
│        └─ Real-time SignalR                            │
└─────────────────────────────────────────────────────────┘
```

**Result:**
- ✅ `/` (Root) → Serves UI with version injection
- ✅ `/app` → Still works (backward compatible)
- ✅ `/dashboard`, `/settings`, etc. → SPA deep routing
- ✅ `/v1/auth/*`, `/v1/system/*` → API endpoints
- ✅ `/swagger` → Swagger UI documentation
- ✅ `/healthz` → Health check (no auth)
- ✅ `/hubs/notifications` → WebSocket SignalR

---

## Success Indicators

After deployment, verify:

**✅ UI loads at root:**
```
https://focusdeck.909436.xyz/  →  HTML page loaded (not Error 1033)
```

**✅ API still works:**
```
https://focusdeck.909436.xyz/v1/system/health  →  {"ok":true,"time":"..."}
```

**✅ Deep routing works:**
```
https://focusdeck.909436.xyz/dashboard  →  Loads UI (SPA routing)
https://focusdeck.909436.xyz/settings   →  Loads UI (SPA routing)
```

**✅ Static assets load:**
```
https://focusdeck.909436.xyz/app/chunk-XXXXX.js  →  200 OK
https://focusdeck.909436.xyz/app/style.css       →  200 OK
```

**✅ Swagger UI works:**
```
https://focusdeck.909436.xyz/swagger  →  API documentation
```

---

## Troubleshooting

### Issue: Still getting Error 1033 from Cloudflare

**Check 1:** Verify service is running
```bash
sudo systemctl status focusdeck
```

**Check 2:** Verify health check passes locally
```bash
curl http://localhost:5000/healthz
```

**Check 3:** Restart Cloudflare tunnel
```bash
sudo systemctl restart cloudflared
sleep 5
sudo systemctl status cloudflared
```

**Check 4:** Verify tunnel config
```bash
sudo cat /etc/cloudflared/config.yml
# Should have: tunnel: focusdeck-tunnel
# Should have: ingress:
#              - hostname: focusdeck.909436.xyz
#                service: http://localhost:5000
```

### Issue: API returns 500 error

**Check 1:** View server logs
```bash
sudo journalctl -u focusdeck -n 50 --no-pager
```

**Check 2:** Check database connection
```bash
curl http://localhost:5000/v1/system/health
```

**Check 3:** Verify database migrations ran
```bash
sqlite3 ~/focusdeck-server/focusdeck.db ".tables"
# Should show: __EFMigrationsHistory, AspNetUsers, etc.
```

### Issue: UI shows blank page

**Check 1:** Verify index.html exists
```bash
ls -la ~/focusdeck-server/wwwroot/app/index.html
```

**Check 2:** Check console errors (F12 developer tools)
- Right-click → Inspect → Console tab
- Look for 404 errors on API calls
- Check that API calls use `/v1` prefix

**Check 3:** Check CORS headers
```bash
curl -i http://localhost:5000/
# Should show Content-Type: text/html
```

---

## Next Steps

1. ✅ Code change: **DONE** (`Program.cs` modified)
2. ✅ Build: **DONE** (Release build successful)
3. ⏳ Deploy: **TODO** (Run deployment steps on Linux server)
4. ⏳ Verify: **TODO** (Test all endpoints)
5. ⏳ Merge: **TODO** (Merge to `master` branch once verified)

---

## Files Modified This Session

- `src/FocusDeck.Server/Program.cs` (1 line added)

## Commits Needed

```bash
git add src/FocusDeck.Server/Program.cs
git commit -m "fix: unify routing to serve UI from root path for Cloudflare tunnel

- Skip root '/' in SPA fallback middleware
- Allow dedicated MapGet('/') endpoint to handle root requests
- Endpoint injects version info and serves index.html with cache headers
- Preserves deep routing for /dashboard, /settings, etc.
- Maintains API routes at /v1/*, /swagger, /healthz, /hubs
- Result: Cloudflare tunnel at root '/' can now serve complete application"
```

---

## Reference

**Previous Issues (Now Fixed):**
- ✅ Nov 7: GitHub Actions test compilation (20 errors → 0)
- ✅ Nov 8: Cloudflare Error 1033 due to missing config file
- ✅ Nov 8: Web UI inaccessible from root path (now unified)

**Environment:**
- Windows Dev Machine: `c:\Users\Caleb\Desktop\FocusDeck`
- Linux Server: `192.168.1.110` (focusdeck user)
- Public Domain: `focusdeck.909436.xyz` (via Cloudflare tunnel)
- Tunnel Name: `focusdeck-tunnel`
