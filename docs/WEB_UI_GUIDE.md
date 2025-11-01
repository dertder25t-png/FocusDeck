# FocusDeck Web UI Guide

## Overview
The FocusDeck web interface provides a user-friendly way to manage your server, generate authentication tokens, and configure the update system - all without needing to use the terminal.

## Accessing the Web Interface

1. **Start the server:**
   ```bash
   cd FocusDeck/src/FocusDeck.Server
   dotnet run
   ```

2. **Open in browser:**
   - Local: `http://localhost:5239`
   - Network: `http://YOUR_SERVER_IP:5239`

3. **Navigate to Settings:**
   - Click the ⚙️ Settings icon in the left sidebar

---

## 🔑 JWT Token Generation

### What is it?
JWT tokens authenticate your desktop apps and Linux agents with the server. No more terminal commands needed!

### How to Generate a Token

1. **Go to Settings → Authentication Token section**

2. **Enter your username:**
   - Type any username (e.g., "john", "my-laptop", "office-pc")
   - This identifies which device/user is syncing

3. **Click "Generate Token"**
   - Token is created instantly
   - Valid for 30 days

4. **Copy the token:**
   - Click on the token text to copy
   - Or use the "📋 Copy Token" button

5. **Use the token:**
   - **Windows App:** Open Settings → Sync tab → Paste token in "Auth Token (JWT)" field
   - **Linux Agent:** Set environment variable: `FOCUSDECK_JWT="your-token-here"`

### Token Details
- **Expiration:** 30 days from generation
- **Security:** Uses HS256 signing algorithm
- **Claims:** Contains username and unique identifier
- **Validation:** Check token validity at Settings → Authentication Token

---

## 🔄 Server Update System

### Overview
The update system allows one-click server updates on Linux. Windows requires manual updates.

### Platform Support

#### ✅ Linux (Fully Automated)
- Pull latest code from GitHub
- Rebuild the server
- Restart the service
- Complete in 30-60 seconds

#### ⚠️ Windows (Manual)
- Must pull code from GitHub manually
- Rebuild using Visual Studio or `dotnet build`
- Restart the application

---

## Linux Update System Setup

### Step 1: Run Configuration Script

**On your Linux server**, run:
```bash
cd /path/to/FocusDeck
sudo bash configure-update-system.sh
```

This script will:
- ✅ Configure repository location
- ✅ Set up environment variables
- ✅ Configure sudo permissions
- ✅ Create log directory
- ✅ Restart the service

### Step 2: Verify Configuration

1. **In the web UI, go to Settings → Server Management**

2. **Click "⚙️ Check Configuration"**

3. **Review the checks:**
   - ✅ Repository exists
   - ✅ Git available
   - ✅ Dotnet SDK available

4. **Status indicators:**
   - 🟢 **Ready** = Fully configured
   - 🟡 **Incomplete** = Missing dependencies
   - 🔴 **Not Configured** = Setup required

### Step 3: Update Your Server

1. **Click "🔍 Check for Updates"**
   - Compares your version with GitHub
   - Shows available updates

2. **Click "🔄 Update Server Now"**
   - Confirm the update dialog
   - Server updates automatically
   - Page reloads when complete

3. **Monitor progress:**
   - Status box shows update progress
   - Typical time: 30-60 seconds
   - Page auto-reloads when done

---

## Configuration Details

### Environment Variables

The update system uses these environment variables:

```bash
# Repository location (optional - defaults to /home/focusdeck/FocusDeck)
FOCUSDECK_REPO="/custom/path/to/FocusDeck"
```

Set in `/etc/systemd/system/focusdeck.service`:
```ini
[Service]
Environment="FOCUSDECK_REPO=/home/focusdeck/FocusDeck"
```

### Sudo Permissions

The `configure-update-system.sh` script creates `/etc/sudoers.d/focusdeck`:

```bash
# FocusDeck update system permissions
focusdeck ALL=(ALL) NOPASSWD: /bin/systemctl restart focusdeck
focusdeck ALL=(ALL) NOPASSWD: /bin/systemctl status focusdeck
focusdeck ALL=(ALL) NOPASSWD: /bin/systemctl is-active focusdeck
focusdeck ALL=(ALL) NOPASSWD: /bin/mkdir
focusdeck ALL=(ALL) NOPASSWD: /bin/chown
```

### Update Logs

Updates are logged to:
```bash
/var/log/focusdeck/update.log
```

View logs:
```bash
cat /var/log/focusdeck/update.log
```

---

## Troubleshooting

### Token Issues

**Problem:** Token not working in desktop app
- ✅ **Solution:** Make sure you copied the entire token (starts with `eyJ`)
- ✅ **Solution:** Check expiration date - generate new token if expired
- ✅ **Solution:** Verify server URL in desktop app matches token server

**Problem:** "Failed to generate token"
- ✅ **Solution:** Check server logs: `journalctl -u focusdeck -f`
- ✅ **Solution:** Verify JWT configuration in `appsettings.json`

### Update System Issues

**Problem:** "Update system is only available on Linux"
- ℹ️ **This is expected on Windows** - update manually via GitHub

**Problem:** "Repository not found"
- ✅ **Solution:** Run `configure-update-system.sh`
- ✅ **Solution:** Set `FOCUSDECK_REPO` environment variable
- ✅ **Solution:** Clone repository to default location: `/home/focusdeck/FocusDeck`

**Problem:** "Git not found" or ".NET SDK not found"
- ✅ **Solution:** Install missing dependencies:
  ```bash
  # Install Git
  sudo apt install git
  
  # Install .NET SDK 9.0
  wget https://dot.net/v1/dotnet-install.sh
  bash dotnet-install.sh --channel 9.0
  ```

**Problem:** Update times out or fails
- ✅ **Solution:** Check network connectivity to GitHub
- ✅ **Solution:** View update logs: `cat /var/log/focusdeck/update.log`
- ✅ **Solution:** Manually run: `cd $FOCUSDECK_REPO && git pull && dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj -c Release`

**Problem:** Server doesn't restart after update
- ✅ **Solution:** Check service status: `systemctl status focusdeck`
- ✅ **Solution:** Manually restart: `sudo systemctl restart focusdeck`
- ✅ **Solution:** Check logs: `journalctl -u focusdeck -n 50`

---

## Security Best Practices

### JWT Tokens
- 🔐 **Don't share tokens** - each device should have its own
- 🔐 **Store securely** - tokens grant full sync access
- 🔐 **Rotate regularly** - generate new tokens every 30 days
- 🔐 **Revoke compromised tokens** - generate new ones immediately

### Update System
- 🔐 **Review changes** - check GitHub commits before updating
- 🔐 **Backup data** - before major updates
- 🔐 **Test updates** - in development environment first
- 🔐 **Monitor logs** - after updates for any issues

### Server Access
- 🔐 **Use firewall** - restrict port 5239 to trusted networks
- 🔐 **Enable HTTPS** - use reverse proxy (nginx/Apache) with SSL
- 🔐 **Strong credentials** - if adding authentication later
- 🔐 **Keep updated** - check for updates weekly

---

## Advanced Usage

### Custom Repository Location

If your repository is not in the default location:

1. **Set environment variable:**
   ```bash
   sudo nano /etc/systemd/system/focusdeck.service
   ```

2. **Add or modify:**
   ```ini
   Environment="FOCUSDECK_REPO=/your/custom/path"
   ```

3. **Reload systemd:**
   ```bash
   sudo systemctl daemon-reload
   sudo systemctl restart focusdeck
   ```

4. **Verify in web UI:**
   - Settings → Server Management → Check Configuration
   - Should show your custom path

### Viewing Update History

Check when updates were last performed:

```bash
# View update log
cat /var/log/focusdeck/update.log

# View service restarts
journalctl -u focusdeck | grep "Started FocusDeck"
```

### Automating Updates

For automatic updates (use with caution):

```bash
# Create cron job to update daily at 3 AM
sudo crontab -e
```

Add:
```cron
0 3 * * * curl -X POST http://localhost:5239/api/update/trigger
```

**⚠️ Warning:** Automatic updates can break your server if there are issues. Test thoroughly first.

---

## Quick Reference

### Web UI Sections

| Section | Purpose |
|---------|---------|
| 🔑 Authentication Token | Generate JWT tokens for desktop apps |
| 🔄 Server Management | Check for updates and update server |
| 🗂️ Data Management | Export, backup, or reset data |

### API Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/auth/token` | POST | Generate JWT token |
| `/api/auth/validate` | GET | Validate token |
| `/api/update/trigger` | POST | Start server update |
| `/api/update/status` | GET | Check update status |
| `/api/update/check-config` | GET | Verify configuration |

### File Locations

| File/Directory | Purpose |
|----------------|---------|
| `/etc/systemd/system/focusdeck.service` | Service configuration |
| `/etc/sudoers.d/focusdeck` | Sudo permissions |
| `/var/log/focusdeck/update.log` | Update logs |
| `/home/focusdeck/FocusDeck` | Default repository path |
| `src/FocusDeck.Server/appsettings.json` | JWT configuration |

---

## Next Steps

After setting up the web UI:

1. ✅ **Generate a token** for your desktop app
2. ✅ **Configure update system** on Linux server (if applicable)
3. ✅ **Test sync** - create a note in desktop app, verify it syncs
4. ✅ **Check for updates** regularly
5. ✅ **Review documentation** - see `API_SETUP_GUIDE.md` for more

---

## Support

- 📚 **Documentation:** See `docs/` folder
- 🐛 **Issues:** Report on GitHub
- 💬 **Discussions:** GitHub Discussions
- 📖 **Full Guide:** See `QUICKSTART.md`

---

**Created:** 2025-11-01  
**Version:** 1.0  
**Status:** ✅ Complete
