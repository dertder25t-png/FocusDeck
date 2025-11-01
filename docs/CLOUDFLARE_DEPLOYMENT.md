# FocusDeck Cloudflare Deployment Guide

## Overview
This guide walks through deploying FocusDeck behind Cloudflare Tunnel with proper proxy header handling, CORS, and JWT configuration.

---

## 🌐 Production URL

**Use this URL everywhere:**
```
https://focusdeck.909436.xyz
```

**DO NOT use:**
- ❌ `http://192.168.1.110:5000` (LAN IP)
- ❌ `http://localhost:5000` (except for local dev)

---

## 📋 Deployment Checklist

### Step 0: Update Client Configuration

In all your client applications (desktop, mobile), set the base URL to:
```
https://focusdeck.909436.xyz
```

### Step 1: Configure Kestrel to Listen on All Interfaces

On your Linux server (192.168.1.110), Kestrel must bind to `0.0.0.0` instead of just localhost.

#### Option A: Environment Variable (Temporary Test)
```bash
export ASPNETCORE_URLS=http://0.0.0.0:5000
cd /path/to/FocusDeck/src/FocusDeck.Server
dotnet run
```

#### Option B: Systemd Service (Permanent - see Step 6)

---

### Step 2: Update Program.cs ✅

**Status: COMPLETED**

The following changes have been made to `src/FocusDeck.Server/Program.cs`:

1. ✅ Added `using Microsoft.AspNetCore.HttpOverrides;`
2. ✅ Configured proper CORS policy with Cloudflare hostname
3. ✅ Added forwarded headers middleware for proxy support
4. ✅ Updated JWT validation to accept multiple issuers/audiences
5. ✅ Added `/healthz` endpoint for health checks
6. ✅ Added HTTP logging for debugging

**Key Changes:**
- CORS now allows `https://focusdeck.909436.xyz` and local dev origins
- Forwarded headers middleware trusts Cloudflare's proxy headers
- JWT validates both production and legacy tokens during transition
- Health check endpoint at `/healthz` (no auth required)

---

### Step 3: Update JWT Configuration ✅

**Status: COMPLETED**

Created `src/FocusDeck.Server/appsettings.Production.json` with production settings:

```json
{
  "Jwt": {
    "Issuer": "https://focusdeck.909436.xyz",
    "Audience": "focusdeck-clients",
    "Key": "super_dev_secret_key_change_me_please_32chars"
  }
}
```

**IMPORTANT:** Generate a secure 256-bit key for production:
```bash
# Generate a secure random key (32 bytes = 256 bits)
openssl rand -base64 32
```

Update the `Key` value in `appsettings.Production.json` with this secure key.

**JWT Validation:**
The server now accepts tokens from multiple issuers/audiences to allow gradual migration:
- ✅ `https://focusdeck.909436.xyz` (production)
- ✅ `FocusDeckDev` (legacy dev)
- ✅ `http://192.168.1.110:5000` (optional - old LAN tokens)

---

### Step 4: Cloudflare Tunnel Configuration

#### Current Setup (Published Routes)
```
Route: focusdeck.909436.xyz → http://192.168.1.110:5000
```

#### Recommended Settings

1. **Edit the route in Cloudflare Zero Trust dashboard:**
   - Navigate to: Networks → Tunnels → [Your Tunnel] → Public Hostname
   - Click "Edit" on `focusdeck.909436.xyz`

2. **Set HTTP Host Header:**
   - Under "Additional application settings"
   - Set **HTTP Host Header** to: `focusdeck.909436.xyz`
   - This ensures ASP.NET Core sees the correct hostname

3. **WebSockets:**
   - ✅ Leave enabled (default)
   - Required if you use SignalR in the future

4. **Access / Zero Trust:**
   - **Option A (Recommended for now):** Disable Access on this route
   - **Option B:** Create a Service Token:
     1. Go to Access → Service Auth → Service Tokens
     2. Create a new token
     3. Add these headers to every API call from your clients:
        ```
        CF-Access-Client-Id: <your-client-id>
        CF-Access-Client-Secret: <your-client-secret>
        ```

**Note:** If Access is enabled but not satisfied, requests won't reach your server (you'll see nothing in logs).

---

### Step 5: Time Synchronization

JWT tokens are time-sensitive. Ensure your server clock is synced:

```bash
# Enable NTP
sudo timedatectl set-ntp true

# Verify
timedatectl

# Expected output shows: "System clock synchronized: yes"
```

---

### Step 6: Update Systemd Service (Permanent Configuration)

**Location:** `/etc/systemd/system/focusdeck.service`

Add these environment variables to the `[Service]` section:

```ini
[Unit]
Description=FocusDeck Server
After=network.target

[Service]
Type=notify
User=focusdeck
Group=focusdeck
WorkingDirectory=/home/focusdeck/FocusDeck/src/FocusDeck.Server

# CRITICAL: Bind to all interfaces for Cloudflare Tunnel
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000

# Enable forwarded headers support
Environment=ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

# Production environment (uses appsettings.Production.json)
Environment=ASPNETCORE_ENVIRONMENT=Production

# JWT Configuration (Production values)
Environment=Jwt__Issuer=https://focusdeck.909436.xyz
Environment=Jwt__Audience=focusdeck-clients
Environment=Jwt__Key=YOUR_SECURE_256BIT_KEY_HERE

# Optional: Repository path for update system
Environment=FOCUSDECK_REPO=/home/focusdeck/FocusDeck

# Start the application
ExecStart=/usr/bin/dotnet /home/focusdeck/FocusDeck/src/FocusDeck.Server/bin/Release/net9.0/FocusDeck.Server.dll

Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

**Apply the changes:**
```bash
# Reload systemd
sudo systemctl daemon-reload

# Restart the service
sudo systemctl restart focusdeck

# Check status
sudo systemctl status focusdeck

# Follow logs
journalctl -u focusdeck -f
```

---

### Step 7: Build and Deploy

#### On Your Development Machine (Windows)
```powershell
cd C:\Users\Caleb\Desktop\FocusDeck

# Build in Release mode
dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj -c Release

# Commit changes
git add .
git commit -m "feat: Configure Cloudflare proxy support with forwarded headers and CORS"
git push origin master
```

#### On Your Linux Server
```bash
cd /home/focusdeck/FocusDeck

# Pull latest code
git pull origin master

# Build in Release mode
dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj -c Release

# Restart service
sudo systemctl restart focusdeck

# Verify it's running
sudo systemctl status focusdeck
```

---

## 🧪 Testing

### Test 1: Health Check via Cloudflare

From any computer:
```bash
curl -i https://focusdeck.909436.xyz/healthz
```

**Expected Response:**
```
HTTP/2 200
content-type: application/json; charset=utf-8
date: Fri, 01 Nov 2025 10:30:00 GMT

{"ok":true,"time":"2025-11-01T10:30:00.000Z"}
```

✅ **Success:** Server is reachable through Cloudflare  
❌ **Failure:** Check Cloudflare Tunnel status and systemd logs

---

### Test 2: CORS Preflight Check

```bash
curl -i -X OPTIONS https://focusdeck.909436.xyz/api/auth/token \
  -H "Origin: https://focusdeck.909436.xyz" \
  -H "Access-Control-Request-Method: POST"
```

**Expected Response:**
```
HTTP/2 204
access-control-allow-origin: https://focusdeck.909436.xyz
access-control-allow-methods: POST
access-control-allow-headers: *
```

✅ **Success:** CORS is configured correctly  
❌ **Failure:** Check CORS policy in Program.cs

---

### Test 3: Token Generation

```bash
curl -i -X POST https://focusdeck.909436.xyz/api/auth/token \
  -H "Content-Type: application/json" \
  -H "Origin: https://focusdeck.909436.xyz" \
  -d '{"username":"test-user"}'
```

**Expected Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "username": "test-user",
  "expiresAt": "2025-12-01T10:30:00Z"
}
```

✅ **Success:** Token generation works  
❌ **Failure:** Check JWT configuration and logs

---

### Test 4: Monitor Live Requests

In a separate terminal, watch the server logs:
```bash
journalctl -u focusdeck -f
```

Then use your desktop app or web UI to:
1. Generate a token
2. Trigger a sync
3. Check for updates

**What to look for:**
- ✅ **Requests appearing in logs:** Server is receiving traffic
- ❌ **No requests at all:** Cloudflare Access is blocking, or client is using wrong URL
- ✅ **200 responses:** Everything working
- ❌ **OPTIONS requests only:** CORS issue
- ❌ **401/403 responses:** JWT validation issue
- ❌ **500 responses:** Server error (check exception details)

---

## 🔍 Troubleshooting

### Issue: No Requests Appearing in Logs

**Possible Causes:**
1. Cloudflare Access is blocking requests
2. Client is using wrong base URL
3. DNS not resolving correctly
4. Cloudflare Tunnel is down

**Solutions:**
```bash
# 1. Check Cloudflare Tunnel status
cloudflared tunnel list
cloudflared tunnel info focusdeck

# 2. Verify DNS resolution
nslookup focusdeck.909436.xyz

# 3. Test from server itself
curl -i http://localhost:5000/healthz

# 4. Check if service is listening
sudo netstat -tlnp | grep 5000
```

---

### Issue: CORS Errors in Browser

**Symptoms:**
- Browser console shows: `Access to fetch at 'https://focusdeck.909436.xyz/api/...' from origin '...' has been blocked by CORS policy`
- Server logs show `OPTIONS` requests but no `POST/GET`

**Solutions:**
1. Verify CORS policy includes your origin:
   ```csharp
   .WithOrigins("https://focusdeck.909436.xyz")
   ```

2. Check `UseCors()` is called **before** `UseAuthentication()` and `UseAuthorization()`

3. Ensure forwarded headers middleware is first

---

### Issue: 401 Unauthorized on Protected Endpoints

**Symptoms:**
- Health check works, but other endpoints return 401
- Token was generated successfully

**Solutions:**
1. Verify token issuer matches server configuration:
   ```bash
   # Decode token to check issuer
   curl https://focusdeck.909436.xyz/api/auth/validate?token=YOUR_TOKEN
   ```

2. Check server JWT configuration:
   ```bash
   # View environment variables
   sudo systemctl show focusdeck | grep Jwt
   ```

3. Verify clock sync:
   ```bash
   timedatectl
   ```

4. Check token hasn't expired (30-day expiration)

---

### Issue: 502 Bad Gateway

**Symptoms:**
- Cloudflare returns 502 error
- `/healthz` doesn't work

**Solutions:**
1. Server not running:
   ```bash
   sudo systemctl status focusdeck
   sudo systemctl start focusdeck
   ```

2. Server not listening on 0.0.0.0:
   ```bash
   # Check what Kestrel is bound to
   sudo netstat -tlnp | grep 5000
   # Should show: 0.0.0.0:5000, not 127.0.0.1:5000
   ```

3. Firewall blocking connections:
   ```bash
   # Check firewall (if using ufw)
   sudo ufw status
   # Allow port 5000 from Cloudflare Tunnel
   ```

---

### Issue: Update System Not Working

**Symptoms:**
- "Update Server" button doesn't work
- Gets 403 or 500 error

**Solutions:**
1. Ensure user has sudo permissions:
   ```bash
   sudo -l -U focusdeck
   ```

2. Add to `/etc/sudoers.d/focusdeck`:
   ```
   focusdeck ALL=(root) NOPASSWD: /opt/focusdeck/scripts/configure-update-system.sh
   focusdeck ALL=(root) NOPASSWD: /usr/bin/systemctl restart focusdeck
   focusdeck ALL=(root) NOPASSWD: /usr/bin/git
   ```

3. Verify `FOCUSDECK_REPO` environment variable is set

---

## 📊 Success Criteria

When everything is working, you should see:

### ✅ Health Check
```bash
$ curl https://focusdeck.909436.xyz/healthz
{"ok":true,"time":"2025-11-01T10:30:00.000Z"}
```

### ✅ Token Generation
- Web UI: Settings → Authentication Token → Generate Token → Success
- Desktop App: Can paste token and sync works

### ✅ Server Logs Show Requests
```
info: Microsoft.AspNetCore.Hosting.Diagnostics[1]
      Request starting HTTP/2 POST https://focusdeck.909436.xyz/api/auth/token application/json 25
info: Microsoft.AspNetCore.Routing.EndpointMiddleware[0]
      Executing endpoint 'FocusDeck.Server.Controllers.AuthController.GenerateToken'
info: FocusDeck.Server.Controllers.AuthController[0]
      Generated token for user: my-laptop
info: Microsoft.AspNetCore.Hosting.Diagnostics[2]
      Request finished HTTP/2 POST https://focusdeck.909436.xyz/api/auth/token - 200 - application/json 45ms
```

### ✅ Client Sync Working
- Desktop app can push/pull changes
- No CORS errors in browser console
- Server logs show authenticated requests

---

## 🔐 Security Notes

### Generate a Secure JWT Key

**IMPORTANT:** Replace the default JWT key before production use:

```bash
# Generate a secure 256-bit key
openssl rand -base64 32

# Example output:
# 7Xn9pK3mQ8vR2wL5jH4tY6uI1oP0aS9dF8gE7bN6cM5=

# Use this as your Jwt__Key environment variable
```

### Cloudflare Access

If you enable Cloudflare Access:
1. Create a Service Token
2. Add these headers to all client requests:
   ```
   CF-Access-Client-Id: <your-client-id>
   CF-Access-Client-Secret: <your-client-secret>
   ```

### HTTPS Only

Always use `https://focusdeck.909436.xyz` - never HTTP in production.

---

## 📚 Additional Resources

- [ASP.NET Core Forwarded Headers Documentation](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer)
- [Cloudflare Tunnel Documentation](https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/)
- [JWT Best Practices](https://tools.ietf.org/html/rfc8725)

---

## 🎉 Next Steps

Once deployed and tested:

1. Update all client applications to use `https://focusdeck.909436.xyz`
2. Generate new tokens for all devices via web UI
3. Test sync functionality from multiple devices
4. Monitor server logs for any errors
5. Set up automated backups
6. Consider setting up monitoring/alerting

---

**Last Updated:** November 1, 2025  
**Status:** Ready for Deployment  
**Tested:** Local Windows Build ✅ | Linux Deployment Pending ⏳
