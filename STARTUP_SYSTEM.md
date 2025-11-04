# FocusDeck Server - Unified Startup System

## 🎯 What Changed

All old startup scripts have been removed and consolidated into **ONE unified script**: `start-focusdeck.sh`

### Removed Scripts ❌
- ❌ `install.sh`
- ❌ `fix-jwt.sh`
- ❌ `easy-setup.sh`
- ❌ `deploy-webui.sh`
- ❌ `configure-update-system.sh`
- ❌ `complete-setup.sh`
- ❌ `complete-fix.sh`
- ❌ `cloudflare-tunnel-setup.sh`
- ❌ `setup-sudo-permissions.sh`
- ❌ `update-server.sh`

### New Script ✅
- ✅ **`start-focusdeck.sh`** - One script to rule them all!

---

## 🚀 Quick Start

### First Time Setup (Fresh Installation)

```bash
cd ~/FocusDeck
chmod +x start-focusdeck.sh
./start-focusdeck.sh setup
```

This will:
1. ✅ Install dependencies (.NET, Git)
2. ✅ Clone/update the repository
3. ✅ Build the application
4. ✅ Setup systemd service
5. ✅ Start the server

### Start the Server

```bash
./start-focusdeck.sh start
```

### Stop the Server

```bash
./start-focusdeck.sh stop
```

### Restart the Server

```bash
./start-focusdeck.sh restart
```

### View Server Status

```bash
./start-focusdeck.sh status
```

### View Server Logs

```bash
./start-focusdeck.sh logs
```

### Update and Restart

```bash
./start-focusdeck.sh update
```

### Rebuild Application

```bash
./start-focusdeck.sh build
```

### Get Help

```bash
./start-focusdeck.sh help
```

---

## 📋 Available Commands

| Command | Purpose |
|---------|---------|
| `setup` | Complete fresh installation (first time only) |
| `start` | Start the FocusDeck server |
| `stop` | Stop the FocusDeck server |
| `restart` | Restart the FocusDeck server |
| `status` | Show current server status |
| `logs` | View server logs (last 50 lines) |
| `build` | Rebuild the application |
| `update` | Pull latest code and restart |
| `help` | Show help message |

---

## 🔧 Setup Instructions

### Step 1: SSH into Your Linux Server

```bash
ssh your-user@your-server-ip
```

### Step 2: Navigate to FocusDeck Directory

```bash
cd ~/FocusDeck
```

Or clone if you don't have it yet:
```bash
git clone https://github.com/dertder25t-png/FocusDeck.git
cd FocusDeck
```

### Step 3: Make the Script Executable

```bash
chmod +x start-focusdeck.sh
```

### Step 4: Run Setup

```bash
./start-focusdeck.sh setup
```

That's it! The server will be installed, built, and running.

---

## 🌐 Access the Web UI

Once the server is running:

```
http://your-server-ip:5239
```

---

## 📊 Server Management

### Check if Server is Running

```bash
./start-focusdeck.sh status
```

### View Real-Time Logs

```bash
./start-focusdeck.sh logs
```

### Restart After Code Changes

```bash
./start-focusdeck.sh restart
```

### Update from GitHub

```bash
./start-focusdeck.sh update
```

This will:
1. Pull latest code from GitHub
2. Rebuild the application
3. Restart the service

---

## 🔄 Auto-Start on Boot

The script automatically enables the systemd service, so the server will:
- ✅ Start automatically when the system boots
- ✅ Restart automatically if it crashes
- ✅ Keep running in the background

Check auto-start status:
```bash
sudo systemctl is-enabled focusdeck
```

---

## 🐛 Troubleshooting

### Server Won't Start

```bash
# Check logs
./start-focusdeck.sh logs

# Check status
./start-focusdeck.sh status

# Try manual restart
./start-focusdeck.sh restart
```

### Port Already in Use

```bash
# Find what's using port 5239
sudo lsof -i :5239

# Kill the process (replace PID with actual process ID)
sudo kill -9 PID
```

### Permission Denied

```bash
# Make script executable
chmod +x start-focusdeck.sh
```

### Dependencies Not Installed

```bash
# Reinstall dependencies (part of setup)
./start-focusdeck.sh setup
```

---

## 📝 Systemd Service File

The script automatically creates:
```
/etc/systemd/system/focusdeck.service
```

View service details:
```bash
sudo systemctl cat focusdeck
```

---

## 🔐 Security Notes

The systemd service includes:
- ✅ Runs as your user (not root)
- ✅ Isolated /tmp directory
- ✅ No new privileges
- ✅ Journald logging
- ✅ Automatic restart on failure

---

## 📚 Common Workflows

### Daily Operations

**Morning - Check server is running:**
```bash
./start-focusdeck.sh status
```

**After code changes - Update and restart:**
```bash
./start-focusdeck.sh update
```

**Evening - View what happened:**
```bash
./start-focusdeck.sh logs
```

### Maintenance

**Full restart:**
```bash
./start-focusdeck.sh restart
```

**Rebuild from scratch:**
```bash
./start-focusdeck.sh stop
./start-focusdeck.sh build
./start-focusdeck.sh start
```

**Update everything:**
```bash
./start-focusdeck.sh update
```

---

## 🎉 That's It!

You now have a **single, unified startup system**. No more confusion with 10+ different scripts!

**Remember:** `./start-focusdeck.sh [command]` is all you need!

