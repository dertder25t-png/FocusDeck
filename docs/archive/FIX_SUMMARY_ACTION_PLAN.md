# FocusDeck Login & UI Fix - Complete Action Plan

## 🚨 Issues Identified
1. **Old UI persisting at `/`** - New React app not being served
2. **Login page not working** - Can't access `/login`  
3. **No auth redirect** - Users not forced to login

## ✅ Root Cause
**Stale wwwroot files** from previous deployments blocking new React app

## 🎯 Solution: Three-Step Fix

### STEP 1: Pull Latest Code
On your Linux server:
```bash
cd ~/FocusDeck
git pull origin phase-1
```

### STEP 2: Run Emergency Clean Deploy
```bash
# Make script executable
chmod +x EMERGENCY_CLEAN_DEPLOY.sh

# Run it
./EMERGENCY_CLEAN_DEPLOY.sh
```

This script:
- ✅ Deletes ALL old build artifacts
- ✅ **Removes wwwroot completely** (kills old UI)
- ✅ Rebuilds React app fresh
- ✅ Deploys new build
- ✅ Restarts service
- ✅ Verifies health

### STEP 3: Test the Fix
```bash
# Open in browser
https://focusdeckv1.909436.xyz/

# Expected: Redirected to login page
# See: LoginPage with email/password form

# Test login with
# Email: test@gmail.com
# Password: 123456789

# Expected after login: Dashboard with sidebar
```

## 📋 New Documentation Added

### 1. **QUICK_FIX_GUIDE.md**
- Problem/root cause analysis
- Step-by-step fix instructions  
- Testing checklist
- Quick reference commands

### 2. **DEBUGGING_GUIDE.md**
- 10-level debugging procedure
- Common errors and fixes
- Decision tree
- System check script
- Escalation guidelines

### 3. **EMERGENCY_CLEAN_DEPLOY.sh**
- Automated complete cleanup
- Verifies each step
- Tests deployment
- Colored output

### 4. **LINUX_AUTH_REDIRECT_SETUP.md**
- Detailed deployment guide
- Architecture flow diagrams
- Troubleshooting section

### 5. **AUTH_REDIRECT_FIX_SUMMARY.md**
- Technical overview
- What changed and why
- Testing checklist

## 🔧 What You Need to Know

### The Problem
```
Old deployment left files in wwwroot/
    ↓
New build copied on top but didn't clean old files
    ↓
Server serves old HTML/JS instead of React app
    ↓
Users see old UI, not new login page
```

### The Fix
```
EMERGENCY_CLEAN_DEPLOY.sh:
    ↓
Deletes wwwroot completely
    ↓
Rebuilds React app from scratch
    ↓
Copies new clean files only
    ↓
Service restarts with new UI
    ↓
Users redirected to login ✓
```

### What's New in Code

**ProtectedRoute.tsx** - Enhanced with:
- JWT expiry validation
- Better loading UI with spinner
- Console logging for debugging
- Proper redirect with state

**FocusDeck.Server.csproj** - Fixed with:
- Disabled problematic compression
- Better error handling in BuildSpa
- Improved logging

**Program.cs** - Already has:
- Correct routing (/app → /)
- Root serves SPA with JWT check
- API endpoints at /v1/*

## 🧪 Testing After Fix

### Test 1: No UI issues
```bash
curl http://localhost:5000/ | head -20
# Should show React app code, NOT old HTML
```

### Test 2: Redirect works
```
Visit: https://focusdeckv1.909436.xyz/
Expected: Redirect to /login
Actual: ?
```

### Test 3: Login page displays
```
Visit: https://focusdeckv1.909436.xyz/login
Expected: LoginPage form showing
Actual: ?
```

### Test 4: Old UI gone
```bash
ls -la src/FocusDeck.Server/wwwroot/
# Should show: index.html + assets/ folder
# Should NOT show: old files with confusing names
```

### Test 5: API works
```bash
curl -X POST http://localhost:5000/v1/auth/pake/login/start \
  -H "Content-Type: application/json" \
  -d '{"userId":"test@gmail.com","clientPublicEphemeralBase64":"test"}'
# Should return 200 with sessionId
```

## 📞 If You're Stuck

### Quick Checklist
- [ ] Did you run `chmod +x EMERGENCY_CLEAN_DEPLOY.sh`?
- [ ] Did the script complete without errors?
- [ ] Did you wait 2-3 seconds after restart?
- [ ] Did you clear browser cache (Ctrl+Shift+Del)?
- [ ] Did you check logs: `sudo journalctl -u focusdeck -f`?

### Manual Fix If Script Fails
```bash
cd ~/FocusDeck
sudo systemctl stop focusdeck
rm -rf src/FocusDeck.Server/wwwroot
dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained -o ~/focusdeck-server-build
sudo cp -r ~/focusdeck-server-build/* /opt/focusdeck/
sudo systemctl start focusdeck
```

### Debug Commands
```bash
# View status
sudo systemctl status focusdeck

# View logs
sudo journalctl -u focusdeck -n 50 -f

# Check wwwroot
ls -la src/FocusDeck.Server/wwwroot/assets/

# Test root
curl -I http://localhost:5000/

# Test health
curl http://localhost:5000/healthz | jq '.'
```

## 📊 Expected Behavior After Fix

| Action | Before | After |
|--------|--------|-------|
| Visit `/` | Old UI shows | Redirected to `/login` |
| Visit `/login` | Doesn't work | LoginPage displays |
| Try login with test/123456789 | Can't login | Redirects to dashboard |
| Try accessing `/lectures` without login | Shows old UI | Redirected to `/login` |
| After login | Still old UI | New dashboard shows |

## 🎓 What This Teaches Us

### Root Cause
Old build artifacts weren't cleaned before deploying new build

### Prevention
- Always clean wwwroot before building
- The emergency script does this automatically
- Use it for all future Release builds

### Architecture Lesson
```
Server (Program.cs)
  ├─ GET /           → serve index.html (React app)
  ├─ GET /login      → React app (public route)
  ├─ GET /dashboard  → React app → ProtectedRoute checks JWT
  ├─ GET /v1/auth/*  → API endpoints
  └─ GET /app/*      → redirect to /

Client (React)
  ├─ App.tsx         → defines routes
  ├─ ProtectedRoute  → checks localStorage for JWT
  │  ├─ valid JWT    → render dashboard
  │  └─ no JWT       → redirect to /login
  └─ LoginPage       → calls /v1/auth/pake/login/*
```

## 🚀 Next Steps

1. **Immediate**: Run EMERGENCY_CLEAN_DEPLOY.sh
2. **Verify**: Test all the testing checklist items
3. **Document**: Note any errors in logs
4. **Share**: Provide logs if issues persist

## 📚 Documentation Files

**All saved to GitHub (phase-1 branch):**

1. `QUICK_FIX_GUIDE.md` - Start here for quick fix
2. `DEBUGGING_GUIDE.md` - If something goes wrong
3. `EMERGENCY_CLEAN_DEPLOY.sh` - Automated fix script
4. `LINUX_AUTH_REDIRECT_SETUP.md` - Detailed deployment
5. `AUTH_REDIRECT_FIX_SUMMARY.md` - Technical overview

## 🎯 Success Criteria

✅ System is working when:
1. Visit `/` → see loading spinner → redirect to `/login`
2. Visit `/login` → see LoginPage form
3. Login with test credentials → redirected to `/dashboard`
4. Logout → try to access `/dashboard` → redirected to `/login`
5. No old UI files visible anywhere
6. Health check: `curl http://localhost:5000/healthz` returns 200
7. Logs show no errors: `sudo journalctl -u focusdeck` is clean

## 🎉 That's It!

You now have:
- ✅ Emergency deployment script
- ✅ Quick fix guide
- ✅ Comprehensive debugging guide
- ✅ Clean code on GitHub
- ✅ Clear next steps

**Go run EMERGENCY_CLEAN_DEPLOY.sh and your system should work!**
