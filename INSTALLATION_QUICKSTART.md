# 🎯 FocusDeck Server Installation & Management

## ⚡ Fastest Way to Get Started

Copy and paste this ONE line:

```bash
curl -sSL https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/install-focusdeck.sh | bash
```

**That's it!** Your server will be installed, built, and running. ✨

---

## 📋 What You Get

After running the one-liner:
- ✅ All dependencies installed (.NET, Git, etc.)
- ✅ Repository cloned from GitHub
- ✅ Application built
- ✅ Systemd service configured
- ✅ Server running on port 5239
- ✅ Auto-start on system reboot
- ✅ Auto-restart if it crashes

---

## 🎮 Managing Your Server

Once installed, manage it with these commands:

```bash
# Start the server
~/FocusDeck/start-focusdeck.sh start

# Stop the server
~/FocusDeck/start-focusdeck.sh stop

# Restart the server
~/FocusDeck/start-focusdeck.sh restart

# Check status
~/FocusDeck/start-focusdeck.sh status

# View logs
~/FocusDeck/start-focusdeck.sh logs

# Update and restart
~/FocusDeck/start-focusdeck.sh update

# Get help
~/FocusDeck/start-focusdeck.sh help
```

---

## 🌐 Access Your Server

Open your browser:

```
http://your-server-ip:5239
```

---

## 📚 Documentation

- **`INSTALLATION_GUIDE.md`** - Detailed installation steps
- **`STARTUP_SYSTEM.md`** - Full server management guide
- **`QUICK_START.md`** - Quick reference card
- **`WEB_UI_FIXES_SUMMARY.md`** - What was fixed in the web UI

---

## 🚀 Installation Steps (If You Prefer Manual)

### Step 1: SSH into Your Server
```bash
ssh your-user@your-server-ip
```

### Step 2: Run the Installer
```bash
curl -sSL https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/install-focusdeck.sh | bash
```

### Step 3: Wait for Completion
The script will show progress and let you know when it's done.

### Step 4: Access the Web UI
Open: `http://your-server-ip:5239`

---

## 💻 What's Inside the Installer

The one-line installer (`install-focusdeck.sh`) does all this automatically:

1. **Updates system** - Ensures latest packages
2. **Installs Git** - For cloning the repository
3. **Installs .NET 8 SDK** - Required runtime
4. **Clones repository** - Gets the FocusDeck code
5. **Runs full setup** - Uses `start-focusdeck.sh setup`
   - Builds the application
   - Creates systemd service
   - Starts the server

---

## 🔧 What's the Difference?

### Old Way ❌
```bash
# 10 confusing scripts, manual installation steps
./install.sh
./complete-setup.sh
./easy-setup.sh
# ... confusion ...
```

### New Way ✅
```bash
# One-liner installs everything
curl -sSL https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/install-focusdeck.sh | bash

# One script manages everything
~/FocusDeck/start-focusdeck.sh [start|stop|restart|status|logs|update]
```

---

## ✅ Verification Checklist

After installation, verify everything works:

- [ ] Open `http://your-server-ip:5239` in browser
- [ ] Dashboard loads without errors
- [ ] Check status: `~/FocusDeck/start-focusdeck.sh status`
- [ ] View logs: `~/FocusDeck/start-focusdeck.sh logs`
- [ ] Server appears in systemd: `sudo systemctl status focusdeck`

---

## 🆘 Troubleshooting

**Server won't start?**
```bash
~/FocusDeck/start-focusdeck.sh logs
```

**Need to reinstall?**
```bash
rm -rf ~/FocusDeck
curl -sSL https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/install-focusdeck.sh | bash
```

**Port already in use?**
```bash
sudo lsof -i :5239
```

---

## 📖 File Structure

```
FocusDeck/
├── install-focusdeck.sh           ← One-line installer
├── start-focusdeck.sh             ← Server management
├── src/
│   └── FocusDeck.Server/
│       └── wwwroot/
│           ├── app.js             ← Web UI (with JWT auth)
│           └── index.html         ← Web UI (with favicon)
├── docs/
│   └── [documentation files]
└── INSTALLATION_GUIDE.md          ← This guide
```

---

## 🎯 Quick Reference

| Task | Command |
|------|---------|
| Fresh install | `curl -sSL https://raw.github.../install-focusdeck.sh \| bash` |
| Start server | `~/FocusDeck/start-focusdeck.sh start` |
| Stop server | `~/FocusDeck/start-focusdeck.sh stop` |
| Restart server | `~/FocusDeck/start-focusdeck.sh restart` |
| Check status | `~/FocusDeck/start-focusdeck.sh status` |
| View logs | `~/FocusDeck/start-focusdeck.sh logs` |
| Update | `~/FocusDeck/start-focusdeck.sh update` |
| Help | `~/FocusDeck/start-focusdeck.sh help` |

---

## 🎉 You're Ready!

Your FocusDeck server is now:
- ✅ **Easy to install** - One command
- ✅ **Easy to manage** - Simple commands
- ✅ **Reliable** - Auto-restart on crash
- ✅ **Automatic** - Starts on reboot
- ✅ **Professional** - Systemd integrated

**Just run the one-liner and you're good to go!** 🚀

