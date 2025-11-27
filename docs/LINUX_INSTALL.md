# FocusDeck Server - Linux Installation Guide

 **Goal**: Deploy FocusDeck to your Linux server in **one command**.

---

##  Quick Start (Recommended)

SSH into your Linux server and run:

```bash
curl -sSL https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/install-focusdeck.sh | sudo bash
```

**That's it!** The script will automatically:
-  Install .NET 9.0 SDK
-  Install Git
-  Create system user
-  Clone FocusDeck repository
-  Build the server
-  Configure systemd service
-  Start the service
-  Print next steps

**Time**: 5-10 minutes depending on internet speed

---

##  Requirements

- **OS**: Ubuntu 20.04+ OR Debian 11+
- **RAM**: 512MB minimum (1GB recommended)
- **Disk**: 1GB free space minimum
- **Access**: Root or sudo privileges
- **Internet**: Required for .NET SDK download

---

##  After Installation

### Check Service Status

```bash
sudo systemctl status focusdeck
```

### View Live Logs

```bash
sudo journalctl -u focusdeck -f
```

### Test the Server

```bash
curl http://localhost:5000/v1/system/health | jq
```

### Access Web Interface

Open in your browser:
```
http://YOUR_SERVER_IP:5000
```

---

##  Common Commands

### Start Service
```bash
sudo systemctl start focusdeck
```

### Stop Service
```bash
sudo systemctl stop focusdeck
```

### Restart Service
```bash
sudo systemctl restart focusdeck
```

### View Logs (Last 50 lines)
```bash
sudo journalctl -u focusdeck -n 50
```

### View Logs (Follow in real-time)
```bash
sudo journalctl -u focusdeck -f
```

### Service is not starting?
```bash
sudo journalctl -u focusdeck -n 100
```

---

##  Manual Installation (If Needed)

If the script doesn't work for your environment, you can manually install:

### Step 1: Update System
```bash
sudo apt-get update
sudo apt-get upgrade -y
```

### Step 2: Install .NET SDK

```bash
# Download .NET installer
cd /tmp
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh

# Install .NET 9.0
./dotnet-install.sh --channel 9.0 -InstallDir /usr/local/dotnet

# Create symlink
sudo ln -sf /usr/local/dotnet/dotnet /usr/bin/dotnet

# Verify
dotnet --version
```

### Step 3: Install Git
```bash
sudo apt-get install -y git
```

### Step 4: Create System User
```bash
sudo useradd -m -s /bin/bash focusdeck
```

### Step 5: Clone Repository
```bash
sudo -u focusdeck git clone https://github.com/dertder25t-png/FocusDeck.git /home/focusdeck/FocusDeck
```

### Step 6: Build Server
```bash
cd /home/focusdeck/FocusDeck
sudo -u focusdeck dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj -c Release
```

### Step 7: Create Systemd Service
```bash
sudo nano /etc/systemd/system/focusdeck.service
```

Paste this content:
```ini
[Unit]
Description=FocusDeck Server
After=network.target

[Service]
Type=simple
User=focusdeck
WorkingDirectory=/home/focusdeck/FocusDeck/src/FocusDeck.Server
Environment="ASPNETCORE_ENVIRONMENT=Production"
Environment="ASPNETCORE_URLS=http://0.0.0.0:5000"
ExecStart=/usr/bin/dotnet run --no-restore --no-build -c Release
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

Save and exit: `Ctrl+X`, then `Y`, then `Enter`

### Step 8: Enable and Start Service
```bash
sudo systemctl daemon-reload
sudo systemctl enable focusdeck
sudo systemctl start focusdeck
```

---

##  Troubleshooting

### Problem: "curl: command not found"

**Solution**: Download and run the script manually
```bash
wget https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/install-focusdeck.sh
sudo bash install-focusdeck.sh
```

### Problem: Service won't start

**Check logs**:
```bash
sudo journalctl -u focusdeck -n 100
```

**Common causes**:
- Port 5000 already in use: `sudo netstat -tlnp | grep 5000`
- .NET not installed properly: `dotnet --version`
- Repository not cloned: `ls /home/focusdeck/FocusDeck`

### Problem: ".NET command not found"

```bash
# Manually add to PATH
export PATH=$PATH:/usr/local/dotnet
dotnet --version
```

### Problem: "Permission denied" errors

```bash
# Ensure focusdeck user owns the directory
sudo chown -R focusdeck:focusdeck /home/focusdeck/FocusDeck
```

### Problem: Port 5000 already in use

Find what's using it:
```bash
sudo netstat -tlnp | grep 5000
sudo lsof -i :5000
```

Change the port in the systemd service file and restart.

---

##  System Information

Once running, check system info:

```bash
# View OS
lsb_release -a

# View .NET version
dotnet --version

# View Git version
git --version

# View service status
systemctl status focusdeck

# View disk usage
df -h /home/focusdeck/FocusDeck
```

---

##  Security Notes

- The installation runs the service as `focusdeck` (non-root user)
- Database is at `/home/focusdeck/FocusDeck/focusdeck.db`
- Configuration is in `/home/focusdeck/FocusDeck/appsettings.json`
- Logs are stored in journald (use `journalctl` to view)

---

##  Getting Help

- **GitHub Issues**: https://github.com/dertder25t-png/FocusDeck/issues
- **Documentation**: https://github.com/dertder25t-png/FocusDeck#readme
- **Logs**: `sudo journalctl -u focusdeck -n 100`

---

##  Success Criteria

After installation, verify:

1.  Service is running: `sudo systemctl status focusdeck`
2.  Health endpoint works: `curl http://localhost:5000/v1/system/health`
3.  Web UI accessible: Open `http://YOUR_IP:5000` in browser
4.  Logs look good: `sudo journalctl -u focusdeck -n 20`

---

**Status**: Installation system cleaned up and unified
**Last Updated**: November 5, 2025
