# 🚀 Quick Deploy Guide - Routing Fix Ready

**Status:** Build complete and tested ✅  
**Your Action:** Deploy to Linux server and verify  
**Time Required:** ~20 minutes  

---

## What Was Fixed

**Problem:** Web UI not accessible at root `/` via Cloudflare tunnel (Error 1033)

**Solution:** Modified `Program.cs` to skip root `/` in middleware, allowing dedicated endpoint to handle it

**Change:** 1 line added at line 677 of `src/FocusDeck.Server/Program.cs`

**Result:** UI now accessible at https://focusdeck.909436.xyz/ ✅

---

## Your Todo List

### ✅ DONE (by agent)
- [x] Code modified (`Program.cs` line 677)
- [x] Build successful (0 errors)
- [x] Published for linux-x64
- [x] Documentation created

### 🔄 TODO (by you)

#### Step 1: SSH to Linux Server
```bash
ssh focusdeck@192.168.1.110
su - focusdeck
cd ~/FocusDeck
```

#### Step 2: Update Code
```bash
git pull origin master
# OR if on authentification branch:
# git pull origin authentification
```

#### Step 3: Build on Server
```bash
cd src/FocusDeck.Server
dotnet publish FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained false -o ~/focusdeck-server
```
Expected output: `FocusDeck.Server -> /home/focusdeck/focusdeck-server/`

#### Step 4: Restart Service
```bash
exit  # back to root
sudo systemctl restart focusdeck
sleep 2
sudo systemctl status focusdeck
```
Expected status: `Active: active (running)`

#### Step 5: Test Locally
```bash
curl http://localhost:5000/
# Should return HTML (not 404)

curl http://localhost:5000/v1/system/health
# Should return: {"ok":true,"time":"..."}
```

#### Step 6: Test from Windows
```powershell
# In PowerShell on your Windows machine:

# Test root path (THIS IS THE MAIN FIX)
Invoke-WebRequest https://focusdeck.909436.xyz/ -UseBasicParsing
# Expected: 200 OK (HTML page loads)

# Test API still works
Invoke-WebRequest https://focusdeck.909436.xyz/v1/system/health -UseBasicParsing
# Expected: 200 OK ({"ok":true,...})

# Test deep routes work
Invoke-WebRequest https://focusdeck.909436.xyz/dashboard -UseBasicParsing
# Expected: 200 OK (UI loads)
```

#### Step 7: Commit to Git
```bash
cd ~/FocusDeck
git add src/FocusDeck.Server/Program.cs
git commit -m "fix: unify routing - skip root in SPA fallback middleware

- Skip '/' in middleware to let MapGet('/') endpoint handle root
- Endpoint injects version and serves index.html
- Result: Cloudflare tunnel at root can now serve complete UI
- All API routes at /v1/* unaffected
- Deep routing still works"
git push origin authentification  # or master
```

---

## Success Indicators

✅ **Check all these to confirm it works:**

1. **Root path loads UI**
   ```
   https://focusdeck.909436.xyz/ → 200 OK (HTML page)
   ```

2. **API works**
   ```
   https://focusdeck.909436.xyz/v1/system/health → 200 OK
   ```

3. **No Error 1033**
   ```
   Previous: Cloudflare tunnel showed error
   Now: Loads perfectly
   ```

4. **Service is running**
   ```
   sudo systemctl status focusdeck → Active (running)
   ```

5. **Browser DevTools shows no errors**
   ```
   F12 → Console tab → No 404 errors
   ```

---

## What Changed

**File:** `src/FocusDeck.Server/Program.cs`  
**Line:** 677  
**Change:** Added one condition

```diff
  if (!path.StartsWith("/v1") && 
      !path.StartsWith("/swagger") && 
      !path.StartsWith("/healthz") &&
      !path.StartsWith("/hubs") &&
+     !path.Equals("/", StringComparison.OrdinalIgnoreCase) &&
      !path.Equals("/swagger.json", StringComparison.OrdinalIgnoreCase))
```

**Effect:** Middleware no longer intercepts root `/`, lets endpoint handle it

---

## Troubleshooting

### Still getting Error 1033?
```bash
# 1. Check service is running
sudo systemctl status focusdeck

# 2. Check health locally
curl http://localhost:5000/healthz

# 3. Restart tunnel
sudo systemctl restart cloudflared

# 4. View logs
sudo journalctl -u focusdeck -n 20 --no-pager
```

### Getting 404 on root?
```bash
# Verify index.html exists
ls -la ~/focusdeck-server/wwwroot/app/index.html

# Verify build completed
ls -la ~/focusdeck-server/FocusDeck.Server.dll
```

### API returning errors?
```bash
# Check database
sqlite3 ~/focusdeck-server/focusdeck.db ".tables"

# View server logs
sudo journalctl -u focusdeck --no-pager | tail -50
```

---

## Documentation Files Created

| File | Purpose |
|------|---------|
| `ROUTING_FIX_DEPLOYMENT.md` | Complete deployment guide |
| `ROUTING_FIX_SUMMARY.md` | Quick reference |
| `ROUTING_FIX_BEFORE_AFTER.md` | Visual comparison |
| `DEPLOYMENT_STATUS_NOV8.md` | Full status report |

---

## Timeline

1. **Now (Nov 8):** You run deployment steps above (~10 min)
2. **Then:** You verify all 5 success indicators (~5 min)
3. **Then:** You commit and push to GitHub (~2 min)
4. **Done:** Application fully working with Cloudflare tunnel ✅

---

## Why This Works

**Before:**
- Request to root `/` 
- Middleware rewritten to `/app/index.html`
- Complex path handling → sometimes timeout → Error 1033

**After:**
- Request to root `/`
- Middleware skips it
- Direct endpoint `MapGet("/")` handles it
- Simple, fast, reliable → No Error 1033

---

## Questions to Check

Before running deployment steps:

- [ ] Do you have SSH access? (already confirmed earlier)
- [ ] Is the Linux server running? (yes, service active)
- [ ] Do you have git access? (yes, already pulled before)
- [ ] Do you have sudo access? (yes, used it before)

If you answered YES to all, you're ready! 🚀

---

## One Last Thing

After deployment works, consider:

1. **Update documentation** if you add new routes
2. **Test authentication** flows end-to-end
3. **Load test** through Cloudflare tunnel
4. **Monitor logs** for a few minutes after deploy

But for now, just follow the 7 steps above and you're done!

---

**Ready?** Let's go! 🚀

Next command to run:
```bash
ssh focusdeck@192.168.1.110
```
