# 🔄 Update Your FocusDeck Server

## Quick Update (Recommended)

Run this single command to update to the latest version:

```bash
cd ~/FocusDeck && \
git pull origin master && \
cd src/FocusDeck.Server && \
dotnet publish FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained -o ~/focusdeck-server && \
sudo systemctl restart focusdeck
```

That's it! Your server is now updated with the new web UI! 🎉

---

## Step-by-Step Update (If you prefer)

### 1. Pull Latest Changes
```bash
cd ~/FocusDeck
git pull origin master
```

### 2. Rebuild the Server
```bash
cd src/FocusDeck.Server
dotnet publish FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained -o ~/focusdeck-server
```

### 3. Restart the Service
```bash
sudo systemctl restart focusdeck
```

### 4. Verify It's Running
```bash
sudo systemctl status focusdeck
```

You should see "Active: active (running)" in green.

---

## What's New in This Version? 🌟

After updating, open your browser to `http://YOUR_SERVER_IP:5000` and you'll see:

### ✨ Complete Dark Theme
- Matches the Windows app design perfectly
- Dark background (#0F0F10) with purple accents (#7B5FFF)
- Professional, modern interface

### 📱 7 Full-Featured Sections
1. **Dashboard** - Overview with stats and quick actions
2. **My Day** - Task management with priorities, categories, due dates
3. **Study Timer** - Pomodoro timer with circular progress display
4. **Decks** - Manage your study decks
5. **Analytics** - View your productivity stats
6. **Calendar** - Month view of your schedule
7. **Settings** - Customize your experience

### 🎯 New Features
- ✅ Create and manage tasks with full details
- ✅ Run Pomodoro timers (15/25/45/60 min presets)
- ✅ Track study sessions with notes
- ✅ View today's statistics in sidebar
- ✅ Export all your data as JSON
- ✅ Works offline with LocalStorage
- ✅ Responsive design for mobile/tablet

---

## Troubleshooting

### Update Failed - Conflicts
If `git pull` shows conflicts:
```bash
# Save your changes
cd ~/FocusDeck
git stash

# Pull updates
git pull origin master

# Rebuild
cd src/FocusDeck.Server
dotnet publish FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained -o ~/focusdeck-server

# Restart
sudo systemctl restart focusdeck
```

### Service Won't Start
Check the logs:
```bash
sudo journalctl -u focusdeck -n 50
```

Common fixes:
```bash
# Fix permissions
sudo chown -R $USER:$USER ~/focusdeck-server
chmod +x ~/focusdeck-server/FocusDeck.Server

# Restart service
sudo systemctl restart focusdeck
```

### Old UI Still Showing
Clear your browser cache:
- **Chrome/Edge**: Ctrl+Shift+Del → Clear cache
- **Firefox**: Ctrl+Shift+Del → Clear cache
- **Safari**: Cmd+Option+E

Or do a hard refresh:
- **Windows**: Ctrl+F5
- **Mac**: Cmd+Shift+R

### Port Already in Use
If you get "port 5000 is already in use":
```bash
# Stop the old service
sudo systemctl stop focusdeck

# Check what's using port 5000
sudo ss -tlnp | grep :5000

# Kill the process if needed (replace PID)
sudo kill -9 <PID>

# Start service
sudo systemctl start focusdeck
```

---

## Verify the Update

### 1. Check Service Status
```bash
sudo systemctl status focusdeck
```

Should show:
```
● focusdeck.service - FocusDeck Server
   Loaded: loaded
   Active: active (running)
```

### 2. Test the Web UI
Open your browser to: `http://YOUR_SERVER_IP:5000`

You should see:
- Dark theme background
- Sidebar with 7 menu items
- Dashboard with stats cards
- Purple accent colors

### 3. Check Version
Look at the bottom of the Settings page - it should show version 1.0.0 with the new dark design.

---

## Rollback (If Needed)

If something goes wrong and you need to go back:

```bash
cd ~/FocusDeck
git log --oneline -n 5  # See recent commits
git checkout <previous-commit-hash>  # Use commit before update
cd src/FocusDeck.Server
dotnet publish FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained -o ~/focusdeck-server
sudo systemctl restart focusdeck
```

To return to latest:
```bash
cd ~/FocusDeck
git checkout master
git pull origin master
# Rebuild and restart as above
```

---

## Automatic Updates (Optional)

Want automatic updates? Create a cron job:

```bash
# Edit crontab
crontab -e

# Add this line (updates daily at 3 AM)
0 3 * * * cd ~/FocusDeck && git pull origin master && cd src/FocusDeck.Server && dotnet publish FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained -o ~/focusdeck-server && sudo systemctl restart focusdeck
```

---

## Server Maintenance Commands

```bash
# View live logs
sudo journalctl -u focusdeck -f

# Restart server
sudo systemctl restart focusdeck

# Stop server
sudo systemctl stop focusdeck

# Start server
sudo systemctl start focusdeck

# Check status
sudo systemctl status focusdeck

# View last 100 log lines
sudo journalctl -u focusdeck -n 100
```

---

## Performance Tips

After updating, optimize your server:

### 1. Clear Old Files
```bash
# Remove old build artifacts
cd ~/FocusDeck/src/FocusDeck.Server
dotnet clean
```

### 2. Optimize Memory (if low memory)
Edit service file:
```bash
sudo nano /etc/systemd/system/focusdeck.service
```

Add under `[Service]`:
```ini
Environment=DOTNET_GCHeapCount=1
Environment=DOTNET_GCServer=0
```

Reload and restart:
```bash
sudo systemctl daemon-reload
sudo systemctl restart focusdeck
```

### 3. Monitor Resource Usage
```bash
# Check memory usage
free -h

# Check CPU usage
top

# Check disk space
df -h
```

---

## Need Help?

- **Logs**: `sudo journalctl -u focusdeck -n 100`
- **Documentation**: [SERVER_SETUP.md](SERVER_SETUP.md)
- **Issues**: [GitHub Issues](https://github.com/dertder25t-png/FocusDeck/issues)

---

## Summary

✅ **Quick Update**: One command to update everything  
✅ **New Features**: Complete dark theme web app with all desktop features  
✅ **Backwards Compatible**: All your existing data is preserved  
✅ **Easy Rollback**: Can revert if needed  
✅ **Zero Downtime**: Service restarts in seconds  

**Enjoy your updated FocusDeck server!** 🚀

---

**Last Updated**: October 31, 2025  
**Current Version**: 1.0.0 (Dark Theme Release)
