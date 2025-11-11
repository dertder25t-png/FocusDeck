# FocusDeck Debugging Guide - Login & UI Issues

## Problem Summary
- `/` shows old UI (not new React app)
- `/login` doesn't work
- No redirect to login on first visit

## Root Cause Analysis

### Symptom 1: Old UI at root
**Possible causes:**
1. ❌ wwwroot still has old files from previous build
2. ❌ BuildSpa target didn't run during build
3. ❌ New files weren't copied to wwwroot
4. ❌ Stale file serving (nginx/proxy cache)

### Symptom 2: Login page not working
**Possible causes:**
1. ❌ `/v1/auth/pake/login/start` API not reachable
2. ❌ React app not loaded properly (old UI)
3. ❌ CORS blocking requests
4. ❌ Database not initialized

### Symptom 3: No redirect to login
**Possible causes:**
1. ❌ React app not loaded (see #1)
2. ❌ ProtectedRoute component not working
3. ❌ localStorage issues
4. ❌ routing misconfiguration

## Step-by-Step Debugging

### Level 1: Verify Server is Running

```bash
# Check if service is active
sudo systemctl status focusdeck

# Should show: Active: active (running)

# Check if listening on port 5000
sudo netstat -tlnp | grep 5000

# Should show: tcp  0  0 127.0.0.1:5000
```

**If not running:**
```bash
sudo systemctl start focusdeck
sleep 2
sudo systemctl status focusdeck
```

---

### Level 2: Check wwwroot Files

```bash
# Navigate to server directory
cd ~/FocusDeck/src/FocusDeck.Server

# Check if wwwroot exists
ls -la | grep wwwroot

# Should show: drwxr-xr-x ... wwwroot

# If missing: wwwroot wasn't created by build!
# Solution: Re-run build or emergency deploy script
```

#### Check wwwroot Contents:

```bash
# List all files in wwwroot
ls -lah wwwroot/

# Should show:
# -rw-r--r-- 1 root root   480 Nov 10 12:34 index.html
# drwxr-xr-x 2 root root  4096 Nov 10 12:34 assets
```

**If old files exist (OLD UI):**
```bash
# Examples of OLD files to look for:
ls -la wwwroot/ | grep -E "\.js|\.html|\.css"

# Old UI files might be:
# - old-app.js
# - dashboard.html
# - app.html
# - styles.css
# - app-{hash}.js (from old Vite build)

# If you see these: DELETE wwwroot and rebuild!
rm -rf wwwroot
```

#### Check Assets Directory:

```bash
# List assets
ls -lah wwwroot/assets/

# Should show 2-3 files:
# -rw-r--r-- ... index-{8-char-hash}.js      (700+ KB minified React)
# -rw-r--r-- ... index-{8-char-hash}.css     (35 KB Tailwind)

# Should NOT show:
# - Multiple .js files
# - .map files
# - old-app-{hash}.js
# - Multiple .css files
```

**Check file sizes:**
```bash
du -sh wwwroot/assets/*

# Should show:
# 700K-800K for the JS (minified React + deps)
# 30K-40K for the CSS

# If much smaller or larger: wrong build
```

---

### Level 3: Test Root Endpoint

```bash
# Get root page
curl http://localhost:5000/

# Look for:
✓ Response code 200
✓ Content-Type: text/html
✓ Contains "<html>" and "<script" (React app)
✓ Contains "focusdeck" somewhere
✓ Does NOT contain "old-app.js" or similar

# If contains old JS files: old UI is being served!
```

**Save response to file for inspection:**
```bash
curl http://localhost:5000/ > /tmp/index.html
wc -l /tmp/index.html  # Count lines

# Should be ~20-50 lines for React app
# Old UI might be 500+ lines with HTML structure
```

---

### Level 4: Check API Endpoints

#### Health Check:
```bash
curl http://localhost:5000/healthz | jq '.'

# Should show:
{
  "status": "Healthy",
  "details": {
    "database": "Healthy",
    "filesystem": "Healthy"
  }
}

# If database unhealthy: DB not initialized
# If filesystem unhealthy: disk issues or permissions
```

#### Auth Start Endpoint:
```bash
curl -X POST http://localhost:5000/v1/auth/pake/login/start \
  -H "Content-Type: application/json" \
  -d '{
    "userId":"test@gmail.com",
    "clientPublicEphemeralBase64":"test"
  }' | jq '.'

# Should return 200 with:
{
  "sessionId": "...",
  "serverPublicEphemeralBase64": "...",
  "saltBase64": "...",
  "kdfParametersJson": "{...}"
}

# If returns 404: Server not recognizing API
# If returns 500: Internal server error (check logs)
# If returns 400: Invalid request
```

---

### Level 5: Check Browser Issues

#### Open Browser Console:
1. Visit `https://focusdeckv1.909436.xyz/login`
2. Press `F12` to open Developer Tools
3. Go to "Console" tab
4. Look for errors (red messages)

**Common errors and fixes:**

| Error | Cause | Fix |
|-------|-------|-----|
| `Cannot GET /login` | Old routing | Rebuild and deploy |
| `Failed to fetch` | API unreachable | Check network/CORS |
| `SyntaxError: Unexpected token` | Old UI JS | Delete wwwroot, rebuild |
| `[Security] Blocked by CORS` | CORS misconfigured | Check Program.cs CORS config |
| `localStorage not defined` | Wrong environment | Check browser isn't in private mode |

#### Check Network Requests:
1. Go to "Network" tab
2. Reload page (`F5`)
3. Look for request to `/v1/auth/pake/login/start`
4. Check:
   - Status: Should be 200 or 400 (not 404)
   - Headers: Should have "Content-Type: application/json"
   - Response: Should contain sessionId

**If getting 404 on `/v1/auth/pake/login/start`:**
- API routes not registered
- Server not restarted after code changes
- Wrong build deployed

---

### Level 6: Check Logs

#### Recent Logs (Last 50 lines):
```bash
sudo journalctl -u focusdeck -n 50 --no-pager

# Look for:
✓ "Listening on" messages
✓ "Database migrate" messages
✓ "Starting FocusDeck Server" message

# Look for errors:
✗ "error" (lowercase)
✗ "exception"
✗ "ERROR" (uppercase)
```

#### Stream Logs Live:
```bash
sudo journalctl -u focusdeck -f

# Then:
# 1. Try visiting root in browser
# 2. Try login
# 3. Watch console for errors in real-time

# Example of good log:
# [INFO] GET / responded 200 in 45ms
# [INFO] GET /login responded 200 in 38ms
# [INFO] POST /v1/auth/pake/login/start responded 200 in 120ms

# Example of bad log:
# [ERROR] Failed to read index.html: File not found
# [ERROR] CORS policy violation
# [WARN] Database not initialized
```

#### Find Errors in Full Log:
```bash
sudo journalctl -u focusdeck --no-pager | grep -i error

# Or with context (3 lines before/after):
sudo journalctl -u focusdeck --no-pager | grep -i -B3 -A3 error
```

---

### Level 7: Manually Verify Build Output

```bash
# On Linux server, check what was actually built
cd ~/focusdeck-server-build

# List directories
ls -la

# Should show:
# -rwxr-xr-x ... FocusDeck.Server (executable)
# -rw-r--r-- ... FocusDeck.Server.dll
# -rw-r--r-- ... wwwroot (directory)

# Check wwwroot in the build
ls -la wwwroot/
ls -la wwwroot/assets/ | head -5

# This shows what was deployed
```

---

### Level 8: CORS Debugging

#### Check CORS Configuration in Code:
```bash
# View the CORS setup
grep -A 20 "AddCors" ~/FocusDeck/src/FocusDeck.Server/Program.cs | head -30
```

#### Test CORS from Browser:
```bash
# In browser console, try:
fetch('/v1/auth/pake/login/start', {
  method: 'POST',
  headers: {'Content-Type': 'application/json'},
  body: JSON.stringify({userId: 'test@gmail.com', clientPublicEphemeralBase64: 'test'})
})
.then(r => r.json())
.then(d => console.log('Success:', d))
.catch(e => console.error('Error:', e))

# Check:
1. Response status (should be 200 or 400, not 500/404)
2. Console output (success or error)
3. Network tab for CORS headers
```

---

### Level 9: Database Check

```bash
# Check if database file exists
ls -la ~/FocusDeck/data/focusdeck.db

# Should exist and have size >1MB

# If missing: Database wasn't initialized
# Solution: Restart service or run migrations

# If permission error:
sudo chown focusdeck:focusdeck ~/FocusDeck/data/focusdeck.db
```

---

### Level 10: Complete System Check Script

```bash
#!/bin/bash
# Save as: ~/check-focusdeck.sh
# Run: chmod +x ~/check-focusdeck.sh && ./check-focusdeck.sh

echo "=== FocusDeck System Check ==="
echo ""

echo "1. Service Status:"
sudo systemctl status focusdeck --no-pager | grep "Active:"
echo ""

echo "2. Process Running:"
ps aux | grep -v grep | grep FocusDeck.Server && echo "✓ Running" || echo "✗ NOT running"
echo ""

echo "3. Port 5000 Listening:"
sudo netstat -tlnp 2>/dev/null | grep 5000 && echo "✓ Listening" || echo "✗ NOT listening"
echo ""

echo "4. wwwroot Exists:"
[ -d "src/FocusDeck.Server/wwwroot" ] && echo "✓ Exists" || echo "✗ Missing"
echo ""

echo "5. index.html Present:"
[ -f "src/FocusDeck.Server/wwwroot/index.html" ] && echo "✓ Present" || echo "✗ Missing"
echo ""

echo "6. Assets Directory:"
[ -d "src/FocusDeck.Server/wwwroot/assets" ] && ls -1 src/FocusDeck.Server/wwwroot/assets/ && echo "✓ OK" || echo "✗ Missing"
echo ""

echo "7. Health Check:"
curl -s http://localhost:5000/healthz | jq '.status'
echo ""

echo "8. Recent Logs:"
sudo journalctl -u focusdeck -n 10 --no-pager
echo ""

echo "=== End Check ==="
```

---

## Troubleshooting Decision Tree

```
Is server running?
├─ NO → sudo systemctl start focusdeck
├─ YES → Next

Does /healthz return 200?
├─ NO → Check logs: sudo journalctl -u focusdeck -n 50 --no-pager
├─ YES → Next

Does wwwroot/index.html exist?
├─ NO → Run: rm -rf src/FocusDeck.Server/wwwroot && dotnet build ...
├─ YES → Next

Does wwwroot/ contain OLD files?
├─ YES (dashboard.html, old-app.js) → Run emergency deploy script
├─ NO → Next

Does curl http://localhost:5000/ show React app?
├─ NO (shows old HTML or error) → Delete wwwroot, rebuild
├─ YES → Next

Does browser open https://focusdeckv1.909436.xyz/ ?
├─ Shows old UI → Clear browser cache: Ctrl+Shift+Del
├─ Shows login → SUCCESS! ✓
├─ Shows error → Check console logs (F12)
```

---

## Quick Copy-Paste Debugging Commands

```bash
# Everything in one go:
cd ~/FocusDeck && \
echo "=== Check Service ===" && \
sudo systemctl status focusdeck --no-pager | head -5 && \
echo "" && \
echo "=== Check wwwroot ===" && \
ls -lah src/FocusDeck.Server/wwwroot/ && \
echo "" && \
echo "=== Check Assets ===" && \
ls -lah src/FocusDeck.Server/wwwroot/assets/ && \
echo "" && \
echo "=== Health Check ===" && \
curl -s http://localhost:5000/healthz | jq '.' && \
echo "" && \
echo "=== Recent Logs ===" && \
sudo journalctl -u focusdeck -n 20 --no-pager
```

---

## When All Else Fails

### Nuclear Option: Complete Reset
```bash
# WARNING: This deletes EVERYTHING and rebuilds from scratch

cd ~/FocusDeck

# Stop service
sudo systemctl stop focusdeck

# Delete all builds
rm -rf src/FocusDeck.Server/bin
rm -rf src/FocusDeck.Server/obj
rm -rf src/FocusDeck.Server/wwwroot
rm -rf src/FocusDeck.WebApp/dist
rm -rf src/FocusDeck.WebApp/node_modules/.cache
rm -rf ~/focusdeck-server-build
rm -rf /opt/focusdeck/*

# Pull fresh code
git fetch origin
git checkout phase-1
git pull origin phase-1

# Build
dotnet clean
dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained -o ~/focusdeck-server-build

# Deploy
sudo cp -r ~/focusdeck-server-build/* /opt/focusdeck/
sudo chown -R focusdeck:focusdeck /opt/focusdeck/

# Start
sudo systemctl start focusdeck

# Verify
sleep 2
curl -s http://localhost:5000/healthz | jq '.'
```

---

## When to Escalate

Contact developer if:
1. ❌ Service won't start (crash on startup)
2. ❌ Database migration fails
3. ❌ API endpoints return 500 errors
4. ❌ Build fails with compilation errors
5. ❌ SSL/HTTPS certificate issues
6. ❌ Disk full errors

Provide:
- [ ] Full log output: `sudo journalctl -u focusdeck --no-pager`
- [ ] Health check: `curl http://localhost:5000/healthz | jq '.'`
- [ ] wwwroot listing: `ls -lah src/FocusDeck.Server/wwwroot/`
- [ ] Current git branch: `git log --oneline -5`
- [ ] Output of emergency deploy script
