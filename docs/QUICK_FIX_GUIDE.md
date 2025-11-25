# FocusDeck Login & UI Issues - Emergency Fix

## Problem
- Old UI still showing at `/`
- `/login` page doesn't work
- Users not redirected to login on first visit

## Root Cause
The wwwroot folder contains old/stale files from a previous deployment. When the new build was copied, the old files weren't fully cleaned, so the old UI is being served instead of the new React app.

## Solution: Run Emergency Clean Deploy Script

### On Your Linux Server:

```bash
# SSH into your Linux server
ssh -i ~/.ssh/deploy_key focusdeck@192.168.1.110

# Navigate to FocusDeck directory
cd ~/FocusDeck

# Make script executable
chmod +x EMERGENCY_CLEAN_DEPLOY.sh

# Run the emergency deployment
./EMERGENCY_CLEAN_DEPLOY.sh
```

### What This Script Does:
1. ✅ Pulls latest `phase-1` code from GitHub
2. ✅ **Completely deletes** old build artifacts
3. ✅ **Deletes wwwroot** (this removes the old UI)
4. ✅ Rebuilds from scratch:
   - Runs `npm ci` for fresh node_modules
   - Runs `npm run build` for new React app
   - Builds .NET server
5. ✅ Deploys new build
6. ✅ Restarts systemd service
7. ✅ Verifies health check passes
8. ✅ Tests endpoints

### Expected Output:
```
FocusDeck Emergency Clean Deployment
==========================================
✓ Code pulled
✓ All old files removed
✓ wwwroot completely deleted
✓ Build complete
✓ New UI files present
✓ Service stopped
✓ New build deployed
✓ Service started
✓ Deployment complete!
```

## If Script Succeeds

### Test 1: Check the UI
```bash
# Open browser to your server
curl http://localhost:5000/

# Should see index.html with React app (not old UI)
```

### Test 2: Try accessing root
Visit: `https://focusdeckv1.909436.xyz/`

**Expected behavior:**
- Page loads with new UI
- Immediately redirected to `/login`
- See LoginPage with email/password form

### Test 3: Try login page directly
Visit: `https://focusdeckv1.909436.xyz/login`

**Expected behavior:**
- LoginPage shows
- Test credentials: `test@gmail.com` / `123456789`
- After login → redirected to dashboard

### Test 4: Logout and try protected pages
```bash
# In browser console
localStorage.removeItem('focusdeck_access_token')

# Try visiting https://focusdeckv1.909436.xyz/lectures
# Should redirect to /login
```

## If Script Fails

### Check logs:
```bash
# View recent logs
sudo journalctl -u focusdeck -n 100 -f

# Or check full log
sudo journalctl -u focusdeck --no-pager | tail -50
```

### Manual cleanup and retry:
```bash
# Stop service
sudo systemctl stop focusdeck

# Aggressive cleanup
cd ~/FocusDeck
rm -rf src/FocusDeck.Server/bin
rm -rf src/FocusDeck.Server/obj
rm -rf src/FocusDeck.Server/wwwroot
rm -rf src/FocusDeck.WebApp/dist
rm -rf ~/focusdeck-server-build

# Verify clean
ls -la src/FocusDeck.Server/ | grep -E "^d" # Should NOT show bin, obj, wwwroot

# Try build again
dotnet clean
dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained -o ~/focusdeck-server-build

# Verify wwwroot was created
ls -la src/FocusDeck.Server/wwwroot/
ls -la src/FocusDeck.Server/wwwroot/assets/ | head -5

# Deploy
sudo cp -r ~/focusdeck-server-build/* /opt/focusdeck/
sudo chown -R focusdeck:focusdeck /opt/focusdeck/

# Start
sudo systemctl start focusdeck
```

## Verify Old UI is Gone

### Check wwwroot structure:
```bash
# On Linux server, check what files are being served
ls -lah src/FocusDeck.Server/wwwroot/

# Should show:
# -rw-r--r-- ... index.html
# drwxr-xr-x ... assets/

# Should NOT show old files like:
# old-app.js, dashboard.html, app.html, etc.
```

### Check assets:
```bash
ls -lah src/FocusDeck.Server/wwwroot/assets/

# Should show files with hashes like:
# index-{hash}.js
# index-{hash}.css

# Should NOT show multiple JS/CSS files
```

## Architecture Check

### /v1/auth/pake/login/start endpoint
```bash
curl -X POST http://localhost:5000/v1/auth/pake/login/start \
  -H "Content-Type: application/json" \
  -d '{"userId":"test@gmail.com","clientPublicEphemeralBase64":"test"}'
```

**Should return 200 with:**
```json
{
  "sessionId": "...",
  "serverPublicEphemeralBase64": "...",
  "saltBase64": "...",
  "kdfParametersJson": "..."
}
```

### Root endpoint
```bash
curl -I http://localhost:5000/

# Should return 200 OK with Content-Type: text/html
# Should NOT return 404 or redirect to old UI
```

### Test login works (if db has test user)
```bash
# First, verify test user exists in database
# If not, you may need to create one via API or manual DB entry

# Or try register first
curl -X POST http://localhost:5000/v1/auth/pake/register/start \
  -H "Content-Type: application/json" \
  -d '{"userId":"testuser@example.com","clientPublicEphemeralBase64":"test"}'
```

## What Changed Since Last Deployment

**From phase-1 branch:**
1. ✅ Enhanced ProtectedRoute with JWT validation
2. ✅ Fixed static asset compression
3. ✅ Added emergency clean deploy script

**Files to verify are present in wwwroot:**
- `index.html` - React app entry point
- `assets/index-{hash}.js` - Minified React + dependencies
- `assets/index-{hash}.css` - Tailwind CSS

**Files that should NOT exist:**
- `old-app.js`
- `app.html`  
- `dashboard.html`
- `styles.css` (old)
- Any `.map` files
- Any files not in assets/ except index.html

## Still Having Issues?

### 1. Check that phase-1 branch is fully deployed
```bash
cd ~/FocusDeck
git log --oneline -5
# Should show recent commits from Nov 10, 2025
# Latest should be "Add: Emergency clean deployment script"
```

### 2. Check service is actually running with new binary
```bash
ps aux | grep focusdeck
# Should show process pointing to /opt/focusdeck/ not old path

sudo systemctl show -p ExecStart focusdeck
# Should show current binary path
```

### 3. Check CORS isn't blocking requests
```bash
# In browser console on https://focusdeckv1.909436.xyz/login
# Try login and check Network tab
# Look for /v1/auth/pake/login/start request
# Check response headers for CORS errors
```

### 4. Check database connectivity
```bash
# On Linux server
curl -s http://localhost:5000/healthz | jq '.details'

# Should show "database": "Healthy"
# If not, DB might not be initialized
```

## Quick Reference Commands

```bash
# Pull latest code
cd ~/FocusDeck && git pull origin phase-1

# Run emergency deploy
./EMERGENCY_CLEAN_DEPLOY.sh

# Restart service
sudo systemctl restart focusdeck

# View logs (last 50 lines)
sudo journalctl -u focusdeck -n 50 --no-pager

# Live logs
sudo journalctl -u focusdeck -f

# Check process
ps aux | grep focusdeck

# Check port 5000
sudo netstat -tlnp | grep 5000

# Curl root
curl -I http://localhost:5000/

# Curl health
curl http://localhost:5000/healthz | jq '.'
```

---

**Next Steps After Fix:**
1. ✅ Run EMERGENCY_CLEAN_DEPLOY.sh on Linux server
2. ✅ Test login redirect works
3. ✅ Verify old UI is completely gone
4. ✅ Document any issues and share logs
