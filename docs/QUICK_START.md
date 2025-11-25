# FocusDeck Startup System - Quick Reference

## ✨ One Script to Rule Them All

**OLD:** 10 different confusing scripts ❌  
**NEW:** One unified `start-focusdeck.sh` ✅

---

## 🚀 Quick Commands

```bash
# First time setup (installs everything)
./start-focusdeck.sh setup

# Start the server
./start-focusdeck.sh start

# Stop the server
./start-focusdeck.sh stop

# Restart the server
./start-focusDeck.sh restart

# Check server status
./start-focusdeck.sh status

# View server logs
./start-focusdeck.sh logs

# Update from GitHub and restart
./start-focusdeck.sh update

# Rebuild the application
./start-focusdeck.sh build

# Get help
./start-focusdeck.sh help
```

---

## 📖 Full Help Output

Run this to see all options:
```bash
./start-focusdeck.sh help
```

Or just run the script with no arguments:
```bash
./start-focusdeck.sh
```

---

## 🔧 Linux Server Setup

### Step 1: Clone Repository
```bash
cd ~
git clone https://github.com/dertder25t-png/FocusDeck.git
cd FocusDeck
```

### Step 2: Make Script Executable
```bash
chmod +x start-focusdeck.sh
```

### Step 3: Run Setup (First Time)
```bash
./start-focusdeck.sh setup
```

### Done! 🎉
Server is installed, built, and running!

---

## 📍 Access Your Server

```
http://your-server-ip:5239
```

---

## 🎯 What Each Command Does

| Command | What It Does |
|---------|------------|
| `setup` | Complete install: dependencies + build + systemd setup + start |
| `start` | Start the server |
| `stop` | Stop the server |
| `restart` | Restart (useful after config changes) |
| `status` | Show if server is running and port status |
| `logs` | View last 50 lines of logs |
| `build` | Rebuild the .NET application |
| `update` | Git pull + rebuild + restart |

---

## ✅ That's It!

No more confusion. Just use:
```bash
./start-focusdeck.sh [command]
```

For full details, see: `STARTUP_SYSTEM.md`

