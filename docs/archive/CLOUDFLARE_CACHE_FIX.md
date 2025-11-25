# Cloudflare Cache Fix - Deployment Guide

## 🎯 What Was Fixed

**Problem:** Old UI still showing after deploys because Cloudflare was caching HTML responses

**Solution:** Added `Cache-Control: no-cache, no-store, must-revalidate` headers to all HTML responses

## ✅ Changes Made

**Commit:** `747953a` - Add no-cache headers for HTML responses

**File Modified:** `src/FocusDeck.Server/Program.cs`

**Changes:**
```csharp
// All .html static files now get no-cache headers
if (ctx.File.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
{
    ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    ctx.Context.Response.Headers["Pragma"] = "no-cache";
    ctx.Context.Response.Headers["Expires"] = "0";
}

// Non-HTML assets (JS, CSS, SVG) still get 7-day cache
else
{
    ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=604800";
}

// Root (/) endpoint includes no-cache headers
app.MapGet("/", async (HttpContext context, ...) =>
{
    // ...
    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    // ...
});

// SPA fallback includes no-cache headers
spa => spa.Run(async context =>
{
    // ...
    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    // ...
});
```

## 🚀 Deployment Steps

### Step 1: Pull Latest Code
```bash
cd ~/FocusDeck
git pull origin phase-1
```

### Step 2: Clean Build Artifacts
```bash
rm -rf src/FocusDeck.Server/bin
rm -rf src/FocusDeck.Server/obj
rm -rf src/FocusDeck.WebApp/dist
rm -rf ~/focusdeck-server-build
```

### Step 3: Build Release Binary
```bash
dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained -o ~/focusdeck-server-build
```

### Step 4: Deploy
```bash
sudo systemctl stop focusdeck
sudo cp -r ~/focusdeck-server-build/* /opt/focusdeck/
sudo chown -R focusdeck:focusdeck /opt/focusdeck/
sudo systemctl start focusdeck
```

### Step 5: Verify Deployment
```bash
# Check service is running
sudo systemctl status focusdeck

# Check logs
sudo journalctl -u focusdeck -n 20 --no-pager

# Test health
curl http://localhost:5000/healthz | jq '.'

# Test root endpoint
curl -I http://localhost:5000/
```

### Step 6: Purge Cloudflare Cache (ONE TIME ONLY)

You have a few options:

**Option A: Via Cloudflare Dashboard**
1. Go to Cloudflare dashboard
2. Navigate to "Cache" → "Purge"
3. Select "Purge Everything"
4. Wait 30 seconds

**Option B: Via cloudflared CLI**
```bash
# If cloudflared is installed on the server
cloudflared service purge --url https://focusdeckv1.909436.xyz/
```

**Option C: Via curl (hard purge)**
```bash
curl -X POST "https://api.cloudflare.com/client/v4/zones/{zone_id}/purge_cache" \
  -H "Authorization: Bearer {api_token}" \
  -H "Content-Type: application/json" \
  -d '{"purge_everything":true}'
```

**Option D: Visit origin directly**
```bash
# Bypass Cloudflare (if you can access server IP directly)
curl -I http://192.168.1.110:5000/
curl -I http://192.168.1.110:5000/login
```

### Step 7: Test Through Cloudflare
```bash
# Test public URL
curl -I https://focusdeckv1.909436.xyz/
curl -I https://focusdeckv1.909436.xyz/login
curl -I https://focusdeckv1.909436.xyz/app/settings
```

All should return:
- Status: 200 or 301 (redirect)
- Header: `Cache-Control: no-cache, no-store, must-revalidate`

## 📊 How It Works

### Before Fix
```
User requests /login
    ↓
Cloudflare intercepts (has cached old index.html from previous deploy)
    ↓
Cloudflare returns cached old index.html (expires: never)
    ↓
Browser loads old UI
    ↓
Old JavaScript tries to load from /app/settings
    ↓
Error: "No routes matched" (old routing doesn't exist)
```

### After Fix
```
User requests /login
    ↓
Cloudflare intercepts
    ↓
Cloudflare sees "Cache-Control: no-cache"
    ↓
Cloudflare passes through to origin (server)
    ↓
Server responds with new index.html
    ↓
Browser loads new React UI
    ↓
ProtectedRoute checks for JWT
    ↓
No JWT → redirects to /login
    ↓
LoginPage displays ✓
```

## 🎯 Caching Strategy

### HTML Files
- **Cache Duration:** 0 (always fresh)
- **Headers:** `no-cache, no-store, must-revalidate`
- **Why:** Must always reflect latest app build

### JavaScript Files
- **Cache Duration:** 7 days
- **Headers:** `public,max-age=604800`
- **Why:** Vite bundles include hash in filename (index-ABC123.js), so safe to cache

### CSS Files
- **Cache Duration:** 7 days
- **Headers:** `public,max-age=604800`
- **Why:** Same as JS - Vite includes hash

### SVG/Images
- **Cache Duration:** 7 days
- **Headers:** `public,max-age=604800`
- **Why:** Unlikely to change; can use CDN edge cache

## ✨ Benefits

### For Development
- **No more manual cache purges** after deploys
- **Automatic propagation** through Cloudflare
- **HTML always fresh**, JS/CSS utilize browser cache

### For Cloudflare
- **Reduced origin requests** (CSS/JS cached for 7 days)
- **HTML never cached** (prevents stale content)
- **Optimal performance** with smart caching

### For Users
- **Instant updates** when new features deploy
- **No stale UI issues**
- **Consistent experience** across all routes

## 🧪 Testing Checklist

After deployment, verify:

- [ ] `curl -I https://focusdeckv1.909436.xyz/` returns 200
- [ ] Response includes `Cache-Control: no-cache, no-store, must-revalidate`
- [ ] Browser shows LoginPage (not old UI)
- [ ] Login with `test@gmail.com` / `123456789` works
- [ ] Access `/lectures` without login redirects to `/login`
- [ ] Access `/app/settings` redirects to `/` then to `/login`
- [ ] No console errors in browser DevTools
- [ ] No "No routes matched" errors

## 🔍 Debug Commands

```bash
# Check cache headers are being sent
curl -I https://focusdeckv1.909436.xyz/ | grep Cache-Control
# Should show: Cache-Control: no-cache, no-store, must-revalidate

# Check JS files still get cached
curl -I https://focusdeckv1.909436.xyz/assets/index-*.js | grep Cache-Control
# Should show: Cache-Control: public,max-age=604800

# Check Cloudflare is not caching
curl -v https://focusdeckv1.909436.xyz/ 2>&1 | grep -i "cf-cache"
# Should show: cf-cache-status: DYNAMIC or MISS (not CACHED)

# Verify origin is serving correct version
curl http://localhost:5000/ | grep -i "<title>"
# Should show: <title>FocusDeck</title>
```

## 🎉 One-Time Purge Steps

**IMPORTANT:** You need to do this once after deploying to clear old cached content.

### Option 1: Cloudflare Dashboard (Easiest)
1. Log in to [Cloudflare Dashboard](https://dash.cloudflare.com)
2. Select your zone (`909436.xyz`)
3. Go to **Caching** → **Configuration** → **Purge**
4. Click **Purge Everything**
5. Wait for confirmation (30 seconds)
6. Done! ✓

### Option 2: Via Cloudflare API
```bash
# Get your zone ID
ZONE_ID="your-zone-id"
API_TOKEN="your-api-token"

# Purge everything
curl -X POST "https://api.cloudflare.com/client/v4/zones/${ZONE_ID}/purge_cache" \
  -H "Authorization: Bearer ${API_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{"purge_everything":true}'
```

### Option 3: Test with Origin Direct
If you can SSH into the server, test without going through Cloudflare:
```bash
# Direct to origin
curl -H "Host: focusdeckv1.909436.xyz" http://localhost:5000/

# Should show new HTML immediately
```

## 📋 Complete Deployment Script

Save this as `~/deploy-with-cache-fix.sh`:

```bash
#!/bin/bash
set -e

echo "=== FocusDeck Deployment with Cache Fix ==="
echo ""

cd ~/FocusDeck

echo "1. Pulling latest code..."
git pull origin phase-1

echo "2. Cleaning build artifacts..."
rm -rf src/FocusDeck.Server/bin
rm -rf src/FocusDeck.Server/obj
rm -rf src/FocusDeck.WebApp/dist
rm -rf ~/focusdeck-server-build

echo "3. Building release binary..."
dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained -o ~/focusdeck-server-build

echo "4. Stopping service..."
sudo systemctl stop focusdeck

echo "5. Deploying..."
sudo cp -r ~/focusdeck-server-build/* /opt/focusdeck/
sudo chown -R focusdeck:focusdeck /opt/focusdeck/

echo "6. Starting service..."
sudo systemctl start focusdeck

echo "7. Verifying..."
sleep 2
curl -s http://localhost:5000/healthz | jq '.'

echo ""
echo "✓ Deployment complete!"
echo ""
echo "NEXT STEPS:"
echo "1. Purge Cloudflare cache (one time only):"
echo "   - Dashboard: Caching → Purge → Purge Everything"
echo "   - Or: cloudflared service purge --url https://focusdeckv1.909436.xyz/"
echo ""
echo "2. Test via Cloudflare:"
echo "   curl -I https://focusdeckv1.909436.xyz/"
echo ""
echo "3. Verify cache headers:"
echo "   curl -I https://focusdeckv1.909436.xyz/ | grep Cache-Control"
```

Usage:
```bash
chmod +x ~/deploy-with-cache-fix.sh
~/deploy-with-cache-fix.sh
```

## 📞 Troubleshooting

### Still seeing old UI?
1. Check you purged Cloudflare: `curl -v https://focusdeckv1.909436.xyz/ 2>&1 | grep cf-cache`
2. Try hard refresh in browser: `Ctrl+Shift+Del` (clear cache) then `Ctrl+Shift+R` (reload)
3. Check origin directly: `curl http://localhost:5000/ | head -20` (should show new HTML)

### Getting Cache-Control errors?
1. Verify server is running: `sudo systemctl status focusdeck`
2. Check logs: `sudo journalctl -u focusdeck -n 50 --no-pager`
3. Restart: `sudo systemctl restart focusdeck`

### CSS/JS not loading?
1. This means they ARE being cached correctly!
2. Check Network tab in DevTools for 304 responses (cached)
3. To force refresh: `Ctrl+Shift+R` in browser

## ✅ Success Indicators

After deployment and Cloudflare purge, you should see:

1. **HTML responses** - Always `Cache-Control: no-cache, no-store, must-revalidate`
2. **JS/CSS responses** - `Cache-Control: public,max-age=604800`
3. **Cloudflare cf-cache-status** - MISS or DYNAMIC (for HTML), HIT (for assets)
4. **First visit** - Shows LoginPage (not old UI)
5. **Subsequent visits** - JS/CSS load from cache, HTML always fresh

## 📚 Related Files

- `src/FocusDeck.Server/Program.cs` - Cache header configuration
- `src/FocusDeck.WebApp/src/pages/Auth/ProtectedRoute.tsx` - Auth logic
- `src/FocusDeck.WebApp/src/App.tsx` - Route definitions

## 🎯 Summary

**What to do on Linux server:**
1. ✅ Pull latest code: `git pull origin phase-1`
2. ✅ Rebuild: `dotnet build ... && deploy`
3. ✅ Purge Cloudflare cache (ONE TIME)
4. ✅ Test: `curl -I https://focusdeckv1.909436.xyz/`

After this, future deployments will automatically propagate without manual cache purges!
