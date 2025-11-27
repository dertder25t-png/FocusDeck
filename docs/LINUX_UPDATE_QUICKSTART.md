# FocusDeck Server Update - Quick Start

## For Your Linux Server

### Initial Setup (Run Once)

```bash
# 1. SSH into your Linux server
ssh your-server

# 2. Navigate to your FocusDeck directory (or wherever you want to clone it)
cd ~

# 3. Pull the latest changes
cd FocusDeck
git pull origin master

# 4. Run the configuration script
sudo bash configure-update-system.sh
```

The script will:
- ✅ Verify/clone the repository
- ✅ Set up sudo permissions
- ✅ Create log directory
- ✅ Configure systemd service
- ✅ Restart the FocusDeck service

### Using the Update System

Once configured, updates are **ONE CLICK** from the web UI:

1. Open your FocusDeck web interface (e.g., https://your-server.com)
2. Navigate to **Settings** → **Server Management**
3. Click **🚀 Update Server Now**
4. Wait 30-60 seconds for the update to complete
5. Page will automatically refresh when ready

### What Happens During Update

```
1. Git pull latest code from GitHub
2. Build application (dotnet publish)
3. Deploy to /opt/focusdeck
4. Restart systemd service
5. Server comes back online
```

### Manual Update (if needed)

```bash
# SSH into server
ssh your-server

# Navigate to repository
cd /home/focusdeck/FocusDeck  # or your custom path

# Pull latest changes
git pull origin master

# Build and deploy
cd src/FocusDeck.Server
dotnet publish FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained false -o /opt/focusdeck

# Restart service
sudo systemctl restart focusdeck

# Check status
systemctl status focusdeck
```

### Check Update Logs

```bash
# View update log
tail -f /var/log/focusdeck/update.log

# View service logs
journalctl -u focusdeck -f

# Check service status
systemctl status focusdeck
```

### Troubleshooting

**Update button doesn't work:**
```bash
# Verify configuration
sudo bash configure-update-system.sh

# Check permissions
ls -la /home/focusdeck/FocusDeck
ls -la /opt/focusdeck
ls -la /var/log/focusdeck

# Check sudo permissions
sudo -l -U focusdeck
```

**Service won't start:**
```bash
# Check logs
journalctl -u focusdeck -n 50

# Verify binary exists
ls -la /opt/focusdeck/FocusDeck.Server

# Try manual start
sudo systemctl start focusdeck
```

### Repository Locations

**Default Setup:**
- Repository: `/home/focusdeck/FocusDeck`
- Application: `/opt/focusdeck`
- Logs: `/var/log/focusdeck/`

**Custom Setup:**
- Set `FOCUSDECK_REPO` environment variable in systemd service
- Edit `/etc/systemd/system/focusdeck.service`

### Quick Commands

```bash
# Pull latest code and update
cd ~/FocusDeck && git pull && sudo systemctl restart focusdeck

# View live logs
journalctl -u focusdeck -f

# Restart service
sudo systemctl restart focusdeck

# Check if service is running
systemctl is-active focusdeck

# View update history
cat /var/log/focusdeck/update.log
```

---

**That's it!** Your Linux server now has a fully automated update system. Just push changes to GitHub and click the update button in the web UI! 🚀
