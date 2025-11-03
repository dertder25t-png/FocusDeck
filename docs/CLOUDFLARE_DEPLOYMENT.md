# FocusDeck Server Deployment Guide

This guide covers advanced setup and deployment using Cloudflare Tunnels. For the fastest setup, please use the `easy-setup.sh` script, which automates most of this.

## Deployment Options

There are two primary ways to expose your FocusDeck server using Cloudflare Tunnels.

---

### Option 1 (Recommended): Install `cloudflared` on the FocusDeck VM

This is the **most secure and reliable** method. By running the `cloudflared` service on the same machine as the FocusDeck server, you enable the server to see the *true client IP address*. This is critical for the authentication system's client fingerprinting security.

The `easy-setup.sh` script will ask if you want to do this automatically. If you are setting up manually, follow these steps *after* running the main install:

**1. Install `cloudflared`**
```bash
# Download the package
wget https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64.deb -O /tmp/cloudflared.deb

# Install
sudo dpkg -i /tmp/cloudflared.deb
sudo apt-get install -f -y # Fix dependencies if needed
rm /tmp/cloudflared.deb
```

**2. Authenticate cloudflared**  
This will open a browser link. You must log in to the Cloudflare account associated with your domain.

```bash
sudo cloudflared tunnel login
```

**3. Create a Tunnel**  
Give your tunnel a memorable name.

```bash
sudo cloudflared tunnel create focusdeck
```

This will output a Tunnel ID and the path to a credentials file. Save these.

**4. Create a Configuration File**  
Create `/etc/cloudflared/config.yml`. Make sure to use the ID and credentials file path from the previous step.

```yaml
# /etc/cloudflared/config.yml

tunnel: YOUR-TUNNEL-ID-GOES-HERE
credentials-file: /root/.cloudflared/YOUR-CREDENTIALS-FILE.json
ingress:
  # Route traffic for your domain to the local FocusDeck server
  - hostname: focusdeck.your-domain.com
    service: http://localhost:5000
  # Catch-all to return 404 for other requests
  - service: http_status:404
```

**Note:** The FocusDeck server is configured to listen on `http://localhost:5000` by default.

**5. Route DNS**  
Link your desired hostname to your tunnel.

```bash
sudo cloudflared tunnel route dns focusdeck focusdeck.your-domain.com
```

**6. Run as a Service**  
Install the cloudflared service and start it.

```bash
sudo cloudflared service install
sudo systemctl start cloudflared
```

Your server is now securely accessible!

---

### Option 2 (Not Recommended): Run `cloudflared` on a Separate VM

This setup involves running `cloudflared` on one VM and FocusDeck on another. 

**⚠️ Security Warning:** This configuration **breaks client IP detection** and weakens authentication security. The FocusDeck server will see all requests as coming from the tunnel VM's IP address instead of the real client's IP. This defeats the purpose of the client fingerprinting security feature used in refresh token validation.

**When to use this:**
- You already have a dedicated VM for Cloudflare Tunnels and cannot change your infrastructure
- You understand and accept the security trade-offs
- You are testing in a development/non-production environment

**Setup Instructions:**

**On the Tunnel VM:**

1. Follow steps 1-3 from Option 1 above to install and authenticate `cloudflared`

2. Create `/etc/cloudflared/config.yml`, pointing to your FocusDeck server's **internal IP address**:

```yaml
# /etc/cloudflared/config.yml

tunnel: YOUR-TUNNEL-ID-GOES-HERE
credentials-file: /root/.cloudflared/YOUR-CREDENTIALS-FILE.json
ingress:
  # Route traffic to FocusDeck server on another VM
  - hostname: focusdeck.your-domain.com
    service: http://192.168.1.X:5000  # Replace with your FocusDeck server's IP
  - service: http_status:404
```

3. Route DNS and start the service:

```bash
sudo cloudflared tunnel route dns focusdeck focusdeck.your-domain.com
sudo cloudflared service install
sudo systemctl start cloudflared
```

**On the FocusDeck VM:**

1. Ensure the server is listening on all interfaces, not just localhost:

Update your systemd service file (`/etc/systemd/system/focusdeck.service`) to include:

```ini
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000
```

2. Restart the FocusDeck service:

```bash
sudo systemctl daemon-reload
sudo systemctl restart focusdeck
```

3. Ensure your firewall allows connections from the tunnel VM to port 5000.

**Security Implications:**

- ❌ All requests appear to come from the tunnel VM's IP
- ❌ Client fingerprinting for refresh tokens is ineffective
- ❌ Cannot distinguish between different clients
- ❌ Increased risk of token theft/replay attacks
- ⚠️ UseForwardedHeaders middleware cannot help because the tunnel VM doesn't set X-Forwarded-For headers

---

## Understanding the Security Difference

### How Client Fingerprinting Works

The FocusDeck authentication system creates a security "fingerprint" for each refresh token based on:
- Client IP address (`HttpContext.Connection.RemoteIpAddress`)
- User agent string
- Other client metadata

This fingerprint is validated on every token refresh. If the IP address changes unexpectedly, the token is rejected, preventing token theft.

### Why Option 1 is Secure

```
[Client] --HTTPS--> [Cloudflare] --Tunnel--> [cloudflared on same VM] --localhost--> [FocusDeck]
                                                      ↓
                                          Sets X-Forwarded-For header
                                                      ↓
                                          UseForwardedHeaders middleware
                                                      ↓
                                          HttpContext.Connection.RemoteIpAddress
                                          = Real Client IP ✓
```

With `cloudflared` on the same VM, the Cloudflare Tunnel correctly sets the `X-Forwarded-For` header, and the ASP.NET Core `UseForwardedHeaders` middleware (already configured in `Program.cs`) extracts the real client IP.

### Why Option 2 is Insecure

```
[Client] --HTTPS--> [Cloudflare] --Tunnel--> [cloudflared on VM1] --HTTP--> [FocusDeck on VM2]
                                                                                      ↓
                                                          HttpContext.Connection.RemoteIpAddress
                                                          = Tunnel VM's IP (e.g., 192.168.1.5)
                                                          ❌ NOT the real client IP
```

With `cloudflared` on a separate VM, the HTTP request from the tunnel VM to FocusDeck appears to originate from the tunnel VM's IP. The `X-Forwarded-For` header from Cloudflare doesn't make it through the second hop.

---

## Automated Setup with `easy-setup.sh`

The recommended way to deploy FocusDeck is using the interactive setup script:

```bash
curl -sSL https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/easy-setup.sh | sudo bash
```

The script will:
1. Check prerequisites (root access, Debian-based OS)
2. **Prompt you to install `cloudflared` locally** (Option 1 - Recommended)
3. Install .NET 9 SDK and Git
4. Create a dedicated system user
5. Clone the FocusDeck repository
6. Build the server in Release mode
7. Generate a secure JWT secret
8. Create and start a systemd service
9. Provide next steps based on your choice

**If you choose Yes (recommended):**
- `cloudflared` will be installed automatically
- You'll receive instructions on how to authenticate and configure the tunnel
- Your server will have proper client IP detection

**If you choose No:**
- You'll need to manually configure your existing tunnel
- The script will warn you about security implications
- Instructions for pointing your tunnel to the server will be provided

---

## Testing Your Deployment

After completing the setup, verify everything is working:

### 1. Health Check

```bash
curl https://your-domain.com/healthz
```

Expected response:
```json
{"ok":true,"time":"2025-11-03T15:00:00.000Z"}
```

### 2. Check Client IP Detection

Generate a token from two different networks (e.g., home WiFi and mobile data). The server logs should show different IP addresses:

```bash
# On the server
sudo journalctl -u focusdeck -f
```

Look for lines like:
```
Generated token for user: my-device (IP: 203.0.113.45)
Token refresh for user: my-device (IP: 203.0.113.45) ✓
```

If you see the same IP for all requests (likely your tunnel VM's internal IP like `192.168.1.5`), you're using Option 2 and client fingerprinting is not working.

---

## Troubleshooting

### Cloudflared Not Connecting

```bash
# Check cloudflared status
sudo systemctl status cloudflared

# View cloudflared logs
sudo journalctl -u cloudflared -f

# Test tunnel connectivity
sudo cloudflared tunnel info focusdeck
```

### FocusDeck Not Starting

```bash
# Check service status
sudo systemctl status focusdeck

# View detailed logs
sudo journalctl -u focusdeck -n 100 --no-pager

# Verify .NET is installed
dotnet --version
```

### 502 Bad Gateway

This usually means FocusDeck isn't running or isn't accessible from the tunnel:

```bash
# Check if server is listening
sudo netstat -tlnp | grep 5000

# Test local connection
curl http://localhost:5000/healthz

# If using Option 2, test from tunnel VM
curl http://192.168.1.X:5000/healthz
```

---

## Migration from Option 2 to Option 1

If you're currently using Option 2 and want to migrate to Option 1 for better security:

1. **On the FocusDeck VM**, install cloudflared:
   ```bash
   wget https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64.deb -O /tmp/cloudflared.deb
   sudo dpkg -i /tmp/cloudflared.deb
   ```

2. Authenticate and create a new tunnel (or move the existing tunnel):
   ```bash
   sudo cloudflared tunnel login
   sudo cloudflared tunnel create focusdeck
   ```

3. Copy the credentials file from the old tunnel VM to the new location (or use the new tunnel ID)

4. Create `/etc/cloudflared/config.yml` pointing to `localhost:5000`

5. Update DNS routing to the new tunnel

6. Install and start the service:
   ```bash
   sudo cloudflared service install
   sudo systemctl start cloudflared
   ```

7. Update the FocusDeck systemd service to listen on localhost only:
   ```ini
   # /etc/systemd/system/focusdeck.service
   Environment=ASPNETCORE_URLS=http://localhost:5000
   ```

8. Restart FocusDeck:
   ```bash
   sudo systemctl daemon-reload
   sudo systemctl restart focusdeck
   ```

9. Test that client IPs are now correctly detected

10. Decommission the old tunnel VM

---

## Additional Security Recommendations

Even with Option 1, consider these additional security measures:

1. **Enable HTTPS Only**: Cloudflare automatically provides HTTPS, but ensure your application never serves HTTP directly

2. **Rate Limiting**: Configure Cloudflare rate limiting rules to prevent brute force attacks

3. **Firewall Rules**: If using UFW or iptables, only allow connections to port 5000 from localhost:
   ```bash
   sudo ufw allow from 127.0.0.1 to any port 5000
   sudo ufw deny 5000
   ```

4. **Regular Updates**: Keep FocusDeck, .NET, and the OS updated

5. **Monitoring**: Set up log monitoring and alerting for suspicious activity

6. **Backup**: Regularly backup your database and configuration files

---

## Summary

| Aspect | Option 1 (Same VM) | Option 2 (Separate VM) |
|--------|-------------------|----------------------|
| **Security** | ✅ Excellent | ❌ Weak |
| **Client IP Detection** | ✅ Works | ❌ Broken |
| **Setup Complexity** | ✅ Simple | ⚠️ Moderate |
| **Maintenance** | ✅ Easy | ⚠️ More Complex |
| **Recommended** | ✅ Yes | ❌ No |

**Bottom line:** Use Option 1 unless you have a very specific reason not to. The security benefits far outweigh any perceived advantages of separating the tunnel.

---

**Need help?** Check the [main documentation](../README.md) or open an issue on GitHub.
