# 🚀 FocusDeck Quick Start

Deploy FocusDeck to your Linux server in 2 minutes!

---

## One-Command Installation

SSH into your Linux server and run:

```bash
curl -sSL https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/easy-setup.sh | sudo bash
```

**That's it!** The script will:
- ✅ Install all dependencies (.NET 9.0, Git)
- ✅ Clone the repository
- ✅ Generate secure keys
- ✅ Configure everything automatically
- ✅ Start FocusDeck

---

## What You Need

Before running the script, you'll need:

1. **A Linux server** (Ubuntu 20.04+ or similar)
2. **Your Cloudflare domain** 
3. **Cloudflare Tunnel** already set up pointing to your server

---

## Manual Installation (If You Prefer)

If you want to run the script locally instead of piping from curl:

```bash
# Download the script
wget https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/easy-setup.sh

# Make it executable
chmod +x easy-setup.sh

# Run it
sudo ./easy-setup.sh
```

The script will ask you for:
- Your Cloudflare domain name
- Username for the service (default: `focusdeck`)

Everything else is automatic!

---

## After Installation

Once the script completes:

1. **Open your domain in a browser:**
   ```
   https://your-domain.com
   ```

2. **Generate a token:**
   - Go to Settings (⚙️ icon)
   - Click "Authentication Token"
   - Enter a username
   - Click "Generate Token"
   - Copy the token

3. **Use the token in your desktop app:**
   - Open FocusDeck desktop app
   - Go to Settings → Sync
   - Paste the token
   - Enter your domain: `https://your-domain.com`

---

## Useful Commands

After installation, you can manage FocusDeck with:

```bash
# View live logs
journalctl -u focusdeck -f

# Check status
systemctl status focusdeck

# Restart service
sudo systemctl restart focusdeck

# Stop service
sudo systemctl stop focusdeck

# Start service
sudo systemctl start focusdeck
```

---

## Updating FocusDeck

You can update FocusDeck in two ways:

### Option 1: From the Web UI (Easiest)
1. Open your FocusDeck web interface
2. Go to Settings
3. Click "Check for Updates"
4. If updates are available, click "Update Server Now"
5. Wait 30-60 seconds - the page will reload automatically

### Option 2: Manually via SSH
```bash
cd /home/focusdeck/FocusDeck
git pull origin master
dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj -c Release
sudo systemctl restart focusdeck
```

---

## Troubleshooting

### Service won't start
```bash
# Check the logs for errors
journalctl -u focusdeck -n 50
```

### Can't access via domain
```bash
# Check if service is running
systemctl status focusdeck

# Check if it's listening on port 5000
sudo netstat -tlnp | grep 5000

# Should show: 0.0.0.0:5000
```

### Health check fails
```bash
# Test locally
curl http://localhost:5000/healthz

# Should return: {"ok":true,"time":"..."}
```

---

## What Gets Installed

The setup script installs and configures:

- **Dependencies:**
  - .NET 9.0 SDK
  - Git
  - OpenSSL

- **FocusDeck:**
  - Repository cloned to `/home/focusdeck/FocusDeck`
  - Systemd service configured
  - Secure JWT key generated
  - Sudo permissions for updates
  - Log directory at `/var/log/focusdeck`

- **Configuration:**
  - Service listens on `0.0.0.0:5000`
  - Forwarded headers enabled for Cloudflare
  - CORS configured for your domain
  - Production environment enabled
  - Time synchronization enabled

---

## Security Notes

- The script generates a secure random JWT key automatically
- All files are owned by the `focusdeck` user
- The service runs as a non-root user
- Sudo permissions are scoped only to required commands

---

## Need Help?

- **Documentation:** See the `docs/` folder in the repository
- **Issues:** [GitHub Issues](https://github.com/dertder25t-png/FocusDeck/issues)
- **Logs:** `journalctl -u focusdeck -f`

---

## Advanced Configuration

If you need to customize the installation:

1. **Change the install directory:**
   Edit the script and modify `INSTALL_DIR`

2. **Use a different port:**
   Edit `/etc/systemd/system/focusdeck.service`
   Change `ASPNETCORE_URLS=http://0.0.0.0:5000` to your port

3. **Custom JWT settings:**
   Edit the service file and modify the `Jwt__*` environment variables

After any changes:
```bash
sudo systemctl daemon-reload
sudo systemctl restart focusdeck
```

---

**That's it!** FocusDeck should now be running and accessible via your Cloudflare domain. 🎉
