# FocusDeck Self-Hosted Cloud Sync - User Setup Guide

**Last Updated:** October 28, 2025  
**Version:** 1.0  
**For Users Who Want:** Windows + Android sync with their own server

---

## Overview

FocusDeck lets you sync your study sessions across devices in **two ways**:

### Option A: Local Only (No Setup) ✅
- **Windows Desktop:** Works offline, saves to local database
- **Android Mobile:** Works offline, saves to local database
- Sessions **don't sync** between devices
- Perfect if you just use one device

### Option B: Self-Hosted Cloud Sync (This Guide) 🚀
- **Windows Desktop:** Works offline + syncs to your server
- **Android Mobile:** Works offline + syncs to your server
- Sessions **sync between all devices** automatically
- You control the server (privacy)
- Simple one-command setup

---

## Option B: Self-Hosted Setup (Recommended)

### Step 1: Get a Linux Server

You need **any** Linux server you can run commands on:

**Options:**
- **Cloud Providers (Easiest):**
  - DigitalOcean ($5-8/month VPS)
  - Linode ($5/month)
  - AWS EC2 Free Tier (1 year free)
  - Google Cloud Always Free

- **At Home:**
  - Raspberry Pi 4+ (sitting in a closet)
  - Old laptop running Ubuntu
  - Proxmox VM
  - Any Linux computer

**Requirements:**
- Ubuntu 20.04 or newer (or any systemd-based Linux)
- 1 GB RAM minimum (really 512 MB OK)
- 10 GB disk space (50 MB for database)
- Network access (port 443 open)

**Recommended:** DigitalOcean Basic Droplet ($5/month) - takes 2 minutes to set up

---

### Step 2: One-Command Server Deployment

SSH into your Linux server and run:

```bash
curl https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/scripts/setup-pocketbase-simple.sh | sudo bash
```

**What happens:**
1. ✅ Automatically downloads and installs PocketBase
2. ✅ Sets up HTTPS with free SSL certificate
3. ✅ Configures automatic startup
4. ✅ Creates reverse proxy (Nginx)
5. ✅ Shows you your server URL

**Time:** 2 minutes  
**Knowledge needed:** Copy/paste into terminal

---

### Step 3: Complete (Your Server is Ready!)

The script tells you:
```
✅ SETUP COMPLETE!

Admin Dashboard: https://YOUR_SERVER_IP/_/

Next steps:
1. Visit that URL
2. Create admin account (email + password)
3. Write down your credentials
4. That's it!
```

---

## Step 4: Set Up Windows Desktop

### 4a. Install FocusDeck Desktop App

- Download from: [GitHub Releases](https://github.com/dertder25t-png/FocusDeck/releases)
- Extract and run `FocusDeck.exe`

### 4b. Configure Cloud Sync

1. Open Settings (gear icon)
2. Go to **Cloud Sync** tab
3. Enter your server details:
   - **Server URL:** `https://YOUR_SERVER_IP` (from step 3)
   - **Email:** `admin@focusdeck.local` (from step 3)
   - **Password:** (your password from step 3)
4. Click **Test Connection** ✅
5. Click **Save** and you're done!

### 4c. Start Syncing

- Every time you complete a study session, it automatically:
  - Saves locally (instant)
  - Uploads to server (in background)
  - You see a ☁️ icon when synced

---

## Step 5: Set Up Android Mobile

### 5a. Install FocusDeck Mobile App

- Download from: [GitHub Releases](https://github.com/dertder25t-png/FocusDeck/releases)
- Install: `FocusDeck-Mobile-vX.Y.Z.apk`

### 5b. Configure Cloud Sync

1. Open Settings (⚙️ icon)
2. Scroll to **Cloud Sync**
3. Turn on **Enable Cloud Sync**
4. Enter your server details:
   - **Server URL:** `https://YOUR_SERVER_IP`
   - **Email:** `admin@focusdeck.local`
   - **Password:** (same as desktop)
5. Tap **Test Connection** ✅
6. Tap **Save**

### 5c. Start Syncing

- Every study session syncs automatically
- Sessions from desktop appear on phone
- Sessions from phone appear on desktop

---

## That's It! You're Done! 🎉

Your Windows desktop and Android phone now:
- ✅ Save sessions locally (works offline)
- ✅ Sync to your private server
- ✅ Sync between devices automatically
- ✅ Keep your data private (you control the server)

---

## Common Questions

### Q: Is my data private?
**A:** Yes! Your server is yours. No data goes to cloud companies. You control everything.

### Q: What if my server goes down?
**A:** Your apps still work offline. Sessions save locally. When server is back up, they auto-sync.

### Q: Can I change my admin password?
**A:** Yes! Visit `https://YOUR_SERVER_IP/_/` and change it in Settings.

### Q: What if I forget my password?
**A:** SSH into your server and run:
```bash
sudo systemctl restart pocketbase
# Then re-run the setup script to create a new admin
```

### Q: Can multiple people use the same server?
**A:** Currently: No (each person gets their own admin account on the shared server).  
Coming soon: Multi-user support where you can invite friends/family.

### Q: How much does this cost?
**A:** 
- DigitalOcean: $5/month
- Linode: $5/month
- Home server (Raspberry Pi): $50-100 one-time
- AWS Free Tier: FREE for 1 year

### Q: Do I need to do anything to maintain it?
**A:** Nope! It auto-updates. You can check health with:
```bash
sudo systemctl status pocketbase
```

### Q: Can I access my server from anywhere?
**A:** Yes! As long as it has a public IP or you set up port forwarding.

### Q: What if I'm not technical?
**A:** Don't worry! The setup is literally one line of code. The hardest part is:
1. Getting a VPS ($5/month)
2. Copy/pasting one command
3. Creating a password

That's it. No Docker, no coding, no complexity.

---

## Troubleshooting

### Server shows as "Not Responding"

**Check 1:** Is the server running?
```bash
sudo systemctl status pocketbase
```

**Check 2:** Can you reach it?
```bash
curl https://YOUR_SERVER_IP/api/health
```

**Check 3:** Check logs:
```bash
sudo journalctl -u pocketbase -n 20
```

### Sync not working on phone

1. Check internet connection
2. Verify Server URL is correct (with `https://`)
3. Check credentials are correct
4. Try "Test Connection" button

### Sessions not appearing on other device

- Give it 10 seconds (it might still be syncing)
- Restart the app
- Check server is reachable

### SSL Certificate Warnings

This is normal on first setup. The cert is self-signed but secure. Click "Advanced" → "Accept" in your browser.

(Coming soon: automatic Let's Encrypt setup for proper certificates)

---

## What You're Getting

With your own FocusDeck server:

✅ **Privacy** - Your data stays yours  
✅ **Reliability** - Your server, your control  
✅ **Simplicity** - One command to deploy  
✅ **Cost** - $5-50/month or free at home  
✅ **Peace of Mind** - No vendor lock-in  

---

## Next Steps

1. ✅ Choose your Linux server (or use DigitalOcean)
2. ✅ Run the one-command setup
3. ✅ Configure Windows desktop
4. ✅ Configure Android mobile
5. ✅ Start studying and syncing!

---

## Support

**Issues?**
- Check [GitHub Issues](https://github.com/dertder25t-png/FocusDeck/issues)
- Email: support@focusdeck.local
- Discord: [Join community](https://discord.gg/focusdeck)

**Want to contribute?**
- GitHub: https://github.com/dertder25t-png/FocusDeck
- We love pull requests!

---

## That's All!

You now have a private, self-hosted study session sync system that's:
- ✨ Simple to set up
- 🔒 Private and secure
- 📱 Works on desktop + mobile
- 🚀 Ready to use

Happy studying! 🎓

---

**Questions?** That's what we're here for. Open an issue on GitHub or reach out!
