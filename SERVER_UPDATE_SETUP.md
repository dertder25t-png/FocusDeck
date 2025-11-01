# FocusDeck Server Update System - Setup Guide

## Overview
The FocusDeck server includes a one-click update system that automatically:
1. Pulls the latest code from GitHub
2. Rebuilds the application
3. Restarts the service

This guide explains how to set up the update system on your Linux server.

## Prerequisites
- FocusDeck server installed and running
- Git repository cloned on the server
- `focusdeck` systemd service configured
- User has sudo privileges for systemctl commands

## Setup Instructions

### 1. Clone the Repository
The update system needs access to the Git repository. Clone it to a location accessible by the `focusdeck` user:

```bash
# Option A: Clone to home directory (recommended)
cd /home/focusdeck
git clone https://github.com/dertder25t-png/FocusDeck.git
chown -R focusdeck:focusdeck /home/focusdeck/FocusDeck

# Option B: Clone to a custom location
sudo mkdir -p /opt/focusdeck-repo
cd /opt/focusdeck-repo
git clone https://github.com/dertder25t-png/FocusDeck.git
sudo chown -R focusdeck:focusdeck /opt/focusdeck-repo/FocusDeck
```

### 2. Configure Repository Path (Optional)
By default, the update system looks for the repository at `/home/focusdeck/FocusDeck`.

If you cloned it to a different location, set the `FOCUSDECK_REPO` environment variable:

```bash
# Edit the systemd service file
sudo nano /etc/systemd/system/focusdeck.service

# Add this line in the [Service] section:
Environment="FOCUSDECK_REPO=/your/custom/path/FocusDeck"

# Reload systemd and restart
sudo systemctl daemon-reload
sudo systemctl restart focusdeck
```

### 3. Configure Sudo Permissions
The update system needs to restart the `focusdeck` service. Grant passwordless sudo for systemctl:

```bash
# Create sudoers file for focusdeck user
sudo visudo -f /etc/sudoers.d/focusdeck

# Add this line:
focusdeck ALL=(ALL) NOPASSWD: /bin/systemctl restart focusdeck, /bin/systemctl status focusdeck, /bin/systemctl is-active focusdeck, /bin/mkdir, /bin/chown

# Save and exit (Ctrl+X, Y, Enter in nano)

# Set proper permissions
sudo chmod 0440 /etc/sudoers.d/focusdeck
```

### 4. Create Log Directory
The update system logs to `/var/log/focusdeck/update.log`:

```bash
sudo mkdir -p /var/log/focusdeck
sudo chown focusdeck:focusdeck /var/log/focusdeck
sudo chmod 755 /var/log/focusdeck
```

### 5. Test the Update System
1. Open your FocusDeck web interface
2. Navigate to **Settings** → **Server Management**
3. Click **🚀 Update Server Now**
4. The server will:
   - Pull latest code from GitHub
   - Rebuild the application
   - Restart automatically (30-60 seconds)
   - The page will offer to refresh when complete

### 6. Monitor Updates
View update logs in real-time:

```bash
# Watch the update log
tail -f /var/log/focusdeck/update.log

# View service logs
journalctl -u focusdeck -f

# Check service status
systemctl status focusdeck
```

## Troubleshooting

### Update Button Not Working
1. **Check if running on Linux**: The update button only works on Linux servers
2. **Verify repository path**: Ensure `/home/focusdeck/FocusDeck` exists (or custom path is set)
3. **Check permissions**: Ensure `focusdeck` user can access the repository
4. **View logs**: Check `/var/log/focusdeck/update.log` for errors

### Permission Errors
```bash
# Fix repository permissions
sudo chown -R focusdeck:focusdeck /home/focusdeck/FocusDeck

# Fix application directory permissions
sudo chown -R focusdeck:focusdeck /opt/focusdeck

# Fix log directory permissions
sudo chown -R focusdeck:focusdeck /var/log/focusdeck
```

### Git Pull Errors
```bash
# Reset repository to clean state
cd /home/focusdeck/FocusDeck
git fetch origin
git reset --hard origin/master
git clean -fdx

# If you have local changes you want to keep
git stash
git pull origin master
git stash pop
```

### Service Not Restarting
```bash
# Check sudo permissions
sudo -l -U focusdeck

# Test manual restart
sudo systemctl restart focusdeck

# Check service logs
journalctl -u focusdeck -n 50
```

### Build Errors
```bash
# Check .NET installation
dotnet --version

# Manually build to see errors
cd /home/focusdeck/FocusDeck/src/FocusDeck.Server
dotnet publish FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained false -o /opt/focusdeck
```

## Architecture

### Update Flow
1. User clicks "Update Server" button in web UI
2. Frontend sends POST request to `/api/server/update`
3. Backend creates a bash script in `/tmp/focusdeck-update-*.sh`
4. Script runs in background using `nohup`
5. Script performs:
   - Git fetch and reset to origin/master
   - dotnet publish to `/opt/focusdeck`
   - systemctl restart focusdeck
6. Frontend polls for server to come back online
7. Automatically refreshes page when server is ready

### File Locations
- **Repository**: `/home/focusdeck/FocusDeck` (default)
- **Application**: `/opt/focusdeck` (published binaries)
- **Logs**: `/var/log/focusdeck/update.log`
- **Update Scripts**: `/tmp/focusdeck-update-*.sh` (auto-deleted)
- **Service**: `/etc/systemd/system/focusdeck.service`

### Environment Variables
- `FOCUSDECK_REPO`: Custom repository path (optional)

## Security Notes

1. **Sudo Access**: The `focusdeck` user has limited sudo access (only systemctl restart)
2. **Git Repository**: Should be owned by `focusdeck` user to prevent unauthorized changes
3. **Update Scripts**: Generated dynamically and deleted after use
4. **Logs**: Stored in `/var/log/focusdeck` with proper permissions

## Advanced Configuration

### Custom Update Script
If you need custom build steps, you can modify the update script in `ServerController.cs`:

```csharp
// Add custom steps in the scriptContent variable
var scriptContent = $@"#!/bin/bash
# ... existing steps ...

# Custom step: Run database migrations
log ""Running database migrations...""
dotnet ef database update --project $REPO_PATH/src/FocusDeck.Server

# Custom step: Clear cache
log ""Clearing cache...""
rm -rf /var/cache/focusdeck/*

# ... rest of script ...
";
```

### Notification on Update
Add a webhook or notification service to alert when updates complete:

```bash
# Add to the end of the update script
curl -X POST https://your-webhook-url.com/notify \
  -H "Content-Type: application/json" \
  -d '{"message":"FocusDeck server updated successfully"}'
```

## FAQ

**Q: Can I update from the web UI on Windows?**  
A: No, the update system only works on Linux. On Windows, use Git and Visual Studio to update.

**Q: Will I lose data during an update?**  
A: No, the update only replaces application files. Your database remains intact.

**Q: How long does an update take?**  
A: Typically 30-60 seconds (git pull + build + restart).

**Q: Can I rollback an update?**  
A: Yes, SSH into the server and run:
```bash
cd /home/focusdeck/FocusDeck
git reset --hard HEAD~1
sudo systemctl restart focusdeck
```

**Q: What if the update fails mid-process?**  
A: The old application remains running until the new one is successfully built. If the build fails, the service restart is skipped.

## Support

If you encounter issues:
1. Check `/var/log/focusdeck/update.log`
2. Check `journalctl -u focusdeck -n 100`
3. Verify all setup steps were completed
4. Open an issue on GitHub with logs

---

**Last Updated**: October 31, 2025  
**Version**: 1.0.0
