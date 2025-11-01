# 🚀 FocusDeck Linux Server Deployment Steps

## Quick Reference for Deploying to 192.168.1.110

---

## ✅ Code is Ready

All code changes have been committed and pushed to GitHub (commit: 8abda87).

The following has been configured:
- ✅ Forwarded headers middleware for Cloudflare proxy
- ✅ CORS policy for `https://focusdeck.909436.xyz`
- ✅ JWT validation for production issuer/audience
- ✅ `/healthz` endpoint for health checks
- ✅ Production configuration in `appsettings.Production.json`

---

## 📋 Server Deployment Checklist

### Step 1: Pull Latest Code

SSH into your server and pull the latest changes:

```bash
ssh user@192.168.1.110

cd /home/focusdeck/FocusDeck
git pull origin master
```

---

### Step 2: Generate Secure JWT Key

**CRITICAL:** Generate a secure 256-bit key for production:

```bash
# Generate secure key
openssl rand -base64 32

# Example output:
# 7Xn9pK3mQ8vR2wL5jH4tY6uI1oP0aS9dF8gE7bN6cM5=

# Save this - you'll need it in Step 4
```

---

### Step 3: Build Release Version

```bash
cd /home/focusdeck/FocusDeck

# Build in Release mode
dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj -c Release

# Verify build succeeded
echo $?  # Should output: 0
```

---

### Step 4: Update Systemd Service

Edit your systemd service file:

```bash
sudo nano /etc/systemd/system/focusdeck.service
```

**Replace with this configuration:**

```ini
[Unit]
Description=FocusDeck Server
After=network.target

[Service]
Type=notify
User=focusdeck
Group=focusdeck
WorkingDirectory=/home/focusdeck/FocusDeck/src/FocusDeck.Server

# CRITICAL: Bind to all interfaces (not just localhost)
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000

# Enable forwarded headers support for Cloudflare
Environment=ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

# Use Production environment
Environment=ASPNETCORE_ENVIRONMENT=Production

# JWT Configuration - REPLACE KEY WITH YOUR GENERATED KEY FROM STEP 2
Environment=Jwt__Issuer=https://focusdeck.909436.xyz
Environment=Jwt__Audience=focusdeck-clients
Environment=Jwt__Key=YOUR_SECURE_256BIT_KEY_FROM_STEP_2_HERE

# Repository path for update system
Environment=FOCUSDECK_REPO=/home/focusdeck/FocusDeck

# Start the application
ExecStart=/usr/bin/dotnet /home/focusdeck/FocusDeck/src/FocusDeck.Server/bin/Release/net9.0/FocusDeck.Server.dll

Restart=always
RestartSec=10
KillSignal=SIGINT

[Install]
WantedBy=multi-user.target
```

**Save and exit:** `Ctrl+X`, then `Y`, then `Enter`

---

### Step 5: Setup Sudo Permissions

Run the sudo permissions setup script:

```bash
cd /home/focusdeck/FocusDeck
sudo bash setup-sudo-permissions.sh

# Follow the prompts:
# - FocusDeck user: focusdeck (or your username)
# - Repository path: /home/focusdeck/FocusDeck (or your path)
```

Or manually create `/etc/sudoers.d/focusdeck`:

```bash
sudo nano /etc/sudoers.d/focusdeck
```

**Add:**
```
# FocusDeck update system permissions
focusdeck ALL=(root) NOPASSWD: /usr/bin/systemctl restart focusdeck
focusdeck ALL=(root) NOPASSWD: /usr/bin/systemctl status focusdeck
focusdeck ALL=(root) NOPASSWD: /usr/bin/systemctl is-active focusdeck
focusdeck ALL=(root) NOPASSWD: /home/focusdeck/FocusDeck/configure-update-system.sh
focusdeck ALL=(root) NOPASSWD: /usr/bin/git
focusdeck ALL=(root) NOPASSWD: /usr/bin/mkdir
focusdeck ALL=(root) NOPASSWD: /usr/bin/chown
```

**Set permissions:**
```bash
sudo chmod 0440 /etc/sudoers.d/focusdeck
```

---

### Step 6: Enable Time Synchronization

JWT tokens are time-sensitive. Enable NTP:

```bash
# Enable NTP
sudo timedatectl set-ntp true

# Verify
timedatectl

# Should show: "System clock synchronized: yes"
```

---

### Step 7: Reload and Restart Service

```bash
# Reload systemd configuration
sudo systemctl daemon-reload

# Restart FocusDeck service
sudo systemctl restart focusdeck

# Check status
sudo systemctl status focusdeck

# Should show: "Active: active (running)"
```

---

### Step 8: Verify Server is Running

```bash
# Check if listening on 0.0.0.0:5000
sudo netstat -tlnp | grep 5000

# Should show: 0.0.0.0:5000 (NOT 127.0.0.1:5000)

# Test health check locally
curl http://localhost:5000/healthz

# Should return: {"ok":true,"time":"..."}
```

---

### Step 9: Follow Logs

Watch the logs for any errors:

```bash
journalctl -u focusdeck -f

# Press Ctrl+C to stop following
```

**What to look for:**
- ✅ `Now listening on: http://0.0.0.0:5000`
- ✅ `Application started`
- ❌ Any error messages or exceptions

---

## 🧪 Testing from Your PC

Once the server is running, test from your Windows PC:

### Test 1: Health Check via Cloudflare

```powershell
curl.exe -i https://focusdeck.909436.xyz/healthz
```

**Expected:**
```
HTTP/2 200
{"ok":true,"time":"2025-11-01T..."}
```

---

### Test 2: Generate Token via Web UI

1. Open browser: `https://focusdeck.909436.xyz`
2. Go to Settings (⚙️ icon)
3. Scroll to "🔑 Authentication Token"
4. Enter username: `test-user`
5. Click "Generate Token"
6. Should see token displayed with copy button

---

### Test 3: Check Server Logs

On the Linux server, watch logs while testing:

```bash
journalctl -u focusdeck -f
```

You should see requests appearing:
```
info: Microsoft.AspNetCore.Hosting.Diagnostics[1]
      Request starting HTTP/2 GET https://focusdeck.909436.xyz/healthz
info: Microsoft.AspNetCore.Hosting.Diagnostics[2]
      Request finished HTTP/2 GET https://focusdeck.909436.xyz/healthz - 200
```

---

## 🔍 Troubleshooting

### Problem: Server won't start

**Check logs:**
```bash
journalctl -u focusdeck -n 50
```

**Common issues:**
- Port 5000 already in use
- .NET runtime not found
- Permission issues with database file
- Invalid JWT key format

---

### Problem: 502 Bad Gateway from Cloudflare

**Verify server is listening:**
```bash
sudo netstat -tlnp | grep 5000
```

**Should show:** `0.0.0.0:5000` (not `127.0.0.1:5000`)

**If showing 127.0.0.1:**
- Check `ASPNETCORE_URLS` environment variable in service file
- Run: `sudo systemctl daemon-reload && sudo systemctl restart focusdeck`

---

### Problem: No requests appearing in logs

**Possible causes:**
1. Cloudflare Tunnel is down
2. Cloudflare Access is blocking requests
3. DNS not resolving

**Check Cloudflare Tunnel:**
```bash
# If cloudflared is installed as a service
sudo systemctl status cloudflared

# Check tunnel status in Cloudflare dashboard
```

---

### Problem: CORS errors in browser

**Verify CORS headers:**
```powershell
curl.exe -i -X OPTIONS https://focusdeck.909436.xyz/api/auth/token `
  -H "Origin: https://focusdeck.909436.xyz" `
  -H "Access-Control-Request-Method: POST"
```

**Expected:**
```
access-control-allow-origin: https://focusdeck.909436.xyz
access-control-allow-methods: POST
```

**If missing:**
- Check `appsettings.Production.json` was deployed
- Verify `ASPNETCORE_ENVIRONMENT=Production` in service file
- Check logs for CORS errors

---

## ✅ Success Criteria

When everything is working:

1. ✅ Health check returns 200: `curl https://focusdeck.909436.xyz/healthz`
2. ✅ Web UI loads: `https://focusdeck.909436.xyz`
3. ✅ Token generation works in Settings
4. ✅ Server logs show HTTPS requests
5. ✅ No CORS errors in browser console
6. ✅ Server listening on `0.0.0.0:5000`

---

## 📞 If You Need Help

1. **Check the logs first:**
   ```bash
   journalctl -u focusdeck -n 100
   ```

2. **Check the detailed guide:**
   - `docs/CLOUDFLARE_DEPLOYMENT.md` - Full deployment guide
   - `docs/WEB_UI_GUIDE.md` - Web UI usage guide

3. **Common log patterns:**
   - `Request starting HTTP/2 GET https://focusdeck.909436.xyz` ✅ Working
   - `Request starting HTTP/1.1 GET http://192.168.1.110:5000` ⚠️ Not using Cloudflare
   - No requests at all ❌ Cloudflare Tunnel issue

---

## 🎯 Next Steps After Deployment

1. Update all client apps to use `https://focusdeck.909436.xyz`
2. Generate new tokens for all devices
3. Test sync from Windows desktop app
4. Test sync from mobile app (if applicable)
5. Monitor logs for 24 hours for any issues
6. Set up automated backups of `focusdeck.db`

---

**Created:** November 1, 2025  
**Commit:** 8abda87  
**Status:** Ready for Deployment 🚀
