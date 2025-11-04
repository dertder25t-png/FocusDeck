# FocusDeck Server - Installation Guide

## 🚀 Quickest Way: One-Line Installer

Copy and paste this ONE line into your Linux terminal:

```bash
curl -sSL https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/install-focusdeck.sh | bash
```

That's it! The script will:
1. ✅ Update system packages
2. ✅ Install Git
3. ✅ Install .NET 8 SDK
4. ✅ Clone the FocusDeck repository
5. ✅ Build the application
6. ✅ Setup systemd service
7. ✅ Start the server

---

## 📖 Step-by-Step Installation

If you prefer to see what's happening step-by-step:

### Step 1: SSH into Your Server

```bash
ssh your-user@your-server-ip
```

### Step 2: Run the One-Line Installer

```bash
curl -sSL https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/install-focusdeck.sh | bash
```

The installer will:
- Ask for password if needed (for sudo commands)
- Download and install dependencies
- Clone the repository
- Build the application
- Start the server

### Step 3: Access the Web UI

Once installation completes, open your browser:

```
http://your-server-ip:5239
```

---

## 🎮 Managing Your Server

After installation, use these commands:

### Start the Server
```bash
~/FocusDeck/start-focusdeck.sh start
```

### Stop the Server
```bash
~/FocusDeck/start-focusdeck.sh stop
```

### Restart the Server
```bash
~/FocusDeck/start-focusdeck.sh restart
```

### Check Server Status
```bash
~/FocusDeck/start-focusdeck.sh status
```

### View Server Logs
```bash
~/FocusDeck/start-focusdeck.sh logs
```

### Update the Server
```bash
~/FocusDeck/start-focusdeck.sh update
```

### Get Help
```bash
~/FocusDeck/start-focusdeck.sh help
```

---

## ✅ What Gets Installed

### Dependencies
- **Git** - Version control
- **.NET 8 SDK** - Runtime environment
- **System packages** - Essential tools

### FocusDeck Files
- Source code cloned to: `~/FocusDeck`
- Database: `~/FocusDeck/focusdeck.db` (SQLite, development)
- Configuration: `~/FocusDeck/appsettings.json`

### Service Setup
- **Systemd service** created: `/etc/systemd/system/focusdeck.service`
- **Auto-start enabled** - Starts automatically on reboot
- **Auto-restart enabled** - Restarts if it crashes
- **Logging** - All output goes to journald

---

## 🌐 Access Your Server

Once running, access it at:

```
http://your-server-ip:5239
```

Or from the server itself:

```
http://localhost:5239
```

---

## 🔧 Troubleshooting

### Installation Fails

Check what went wrong:
```bash
# View the installation output
curl -sSL https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/install-focusdeck.sh | bash
```

### Server Won't Start

Check the logs:
```bash
~/FocusDeck/start-focusdeck.sh logs
```

### Port Already in Use

The script uses port 5239. If it's in use:
```bash
sudo lsof -i :5239
```

### Permission Issues

Make sure the startup script is executable:
```bash
chmod +x ~/FocusDeck/start-focusdeck.sh
```

### Need to Reinstall

```bash
# Stop the server
~/FocusDeck/start-focusdeck.sh stop

# Remove old installation
rm -rf ~/FocusDeck

# Run installer again
curl -sSL https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/install-focusdeck.sh | bash
```

---

## 📊 System Requirements

- **OS**: Linux (Ubuntu 18.04+, Debian 10+, etc.)
- **RAM**: 512MB minimum (1GB recommended)
- **Disk**: 2GB minimum (5GB recommended)
- **Architecture**: x64 or ARM64

---

## 🔐 Security

The installation script:
- ✅ Runs as non-root user (not root)
- ✅ Uses systemd for process management
- ✅ Includes automatic restarts on failure
- ✅ Uses journald for secure logging

---

## 📝 Next Steps After Installation

1. **Access the web UI**: `http://your-server-ip:5239`
2. **Check the logs**: `~/FocusDeck/start-focusdeck.sh logs`
3. **Verify it's running**: `~/FocusDeck/start-focusdeck.sh status`
4. **Read the documentation**: See `STARTUP_SYSTEM.md` for full details

---

## 🆘 Getting Help

For more information:
- **Quick reference**: `~/FocusDeck/start-focusdeck.sh help`
- **Full guide**: Read `STARTUP_SYSTEM.md`
- **Quick start**: Read `QUICK_START.md`

---

## 🎉 That's It!

Your FocusDeck server is now installed and running!

**One-liner to remember:**
```bash
curl -sSL https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/install-focusdeck.sh | bash
```

