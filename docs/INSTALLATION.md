# Installation Guide - All Platforms

Quick installation instructions for FocusDeck across all platforms.

---

## 🖥️ Desktop Installation (Windows 10+)

### Requirements
- Windows 10 Build 19041 or newer
- 200 MB free disk space
- .NET 8 Runtime (automatically checked by installer)
- Administrator access (for system integration features)

### Installation Methods

#### Method A: Download & Extract (Simplest)
1. Visit https://github.com/dertder25t-png/FocusDeck/releases
2. Download `FocusDeck-Desktop-v*.zip` (latest release)
3. Right-click → Extract All
4. Navigate to extracted folder
5. Double-click `FocusDeck.exe`
6. Grant permissions if prompted by Windows Defender

#### Method B: Command Line
```cmd
# Navigate to desired folder
cd C:\Programs

# Download latest release
powershell -Command "(New-Object System.Net.ServicePointManager).SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocolType -bor 3072; (New-Object System.Net.WebClient).DownloadFile('https://github.com/dertder25t-png/FocusDeck/releases/download/v1.0.0/FocusDeck-Desktop-v1.0.0.zip', 'FocusDeck.zip')"

# Extract
Expand-Archive -Path FocusDeck.zip -DestinationPath .

# Run
.\FocusDeck\FocusDeck.exe
```

#### Method C: Build from Source
```bash
# Clone repository
git clone https://github.com/dertder25t-png/FocusDeck.git
cd FocusDeck

# Build
dotnet publish src/FocusDock.App/FocusDock.App.csproj -c Release -o ./build

# Run
./build/FocusDeck.exe
```

### First Run
1. App will create `%APPDATA%\FocusDeck` directory
2. Initial configuration file created
3. Grant microphone permission (for voice notes)
4. Configure OneDrive/Google Drive if desired

### Uninstallation
1. Close application
2. Delete extraction folder
3. Optional: Delete `%APPDATA%\FocusDeck` folder

---

## 📱 Mobile Installation (Android 8+)

### Requirements
- Android 8.0 or higher
- 150 MB free storage
- Internet connection (for cloud sync features)
- Microphone access (for voice notes)

### Installation Methods

#### Method A: Direct Download to Phone
1. Open browser on Android device
2. Go to https://github.com/dertder25t-png/FocusDeck/releases
3. Tap `FocusDeck-Mobile-v*.apk` (latest release)
4. Tap "Download"
5. Once downloaded, tap notification to install
6. Grant permissions as prompted

#### Method B: ADB (Android Debug Bridge)
```bash
# Connect device via USB (enable Developer Mode first)
adb devices

# Download APK
wget https://github.com/dertder25t-png/FocusDeck/releases/download/v1.0.0/FocusDeck-Mobile-v1.0.0.apk

# Install
adb install FocusDeck-Mobile-v1.0.0.apk
```

#### Method C: Build Locally
```bash
# Clone and setup
git clone https://github.com/dertder25t-png/FocusDeck.git
cd FocusDeck

# Ensure Android device connected
adb devices

# Build and deploy
dotnet build src/FocusDeck.Mobile/FocusDeck.Mobile.csproj -f net8.0-android -c Release
dotnet run --project src/FocusDeck.Mobile/FocusDeck.Mobile.csproj
```

### First Run
1. Grant requested permissions:
   - Microphone (for voice notes)
   - Storage (for local data)
   - Notifications (for timer alerts)
2. Create account or login
3. Configure cloud sync (optional)

### Permissions Required
- **Microphone**: Recording voice notes
- **Storage**: Saving session data and backups
- **Camera**: Optional photo tagging
- **Location**: Optional session location tagging
- **Notifications**: Timer alerts and reminders

### Uninstallation
Settings → Apps → FocusDeck → Uninstall

---

## 🖥️ Server Installation (Linux/Proxmox VM)

### Requirements
- Linux OS (Ubuntu 22.04+ recommended)
- 2+ CPU cores
- 2+ GB RAM
- 10+ GB disk space
- Internet connection
- Root/sudo access

### Installation Methods

#### Method A: Automated Setup (Recommended)
```bash
# Download and run setup script
sudo bash <(curl -fsSL https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/setup-server.sh)
```

The script automatically:
- Installs .NET 8 Runtime
- Creates application user
- Configures Nginx reverse proxy
- Generates SSL certificates
- Sets up systemd service
- Configures logging

#### Method B: Manual Setup

**Step 1: Install Dependencies**
```bash
sudo apt-get update
sudo apt-get upgrade -y
sudo apt-get install -y curl wget git build-essential libssl-dev nginx supervisor
```

**Step 2: Install .NET 8 Runtime**
```bash
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0
```

**Step 3: Create Application Directory**
```bash
sudo mkdir -p /opt/focusdeck
sudo useradd -m -s /bin/false -d /opt/focusdeck focusdeck
```

**Step 4: Download Release**
```bash
cd /opt/focusdeck
sudo wget https://github.com/dertder25t-png/FocusDeck/releases/download/v1.0.0/focusdeck-server-v1.0.0.tar.gz
sudo tar -xzf focusdeck-server-*.tar.gz
sudo chown -R focusdeck:focusdeck /opt/focusdeck
```

**Step 5: Configure Environment**
```bash
sudo nano /etc/focusdeck/appsettings.json
# Update OAuth credentials, domain settings, etc.
```

**Step 6: Start Service**
```bash
sudo systemctl enable focusdeck
sudo systemctl start focusdeck
sudo systemctl status focusdeck
```

#### Method C: Docker (Optional)
```bash
# Coming soon - Docker image available
docker run -d \
  --name focusdeck \
  -p 443:443 \
  -v /etc/focusdeck:/config \
  -e ASPNETCORE_ENVIRONMENT=Production \
  focusdeck-server:latest
```

### Post-Installation Configuration

**Update OAuth Credentials:**
```bash
sudo nano /etc/focusdeck/appsettings.json
```
Add your Google Drive and OneDrive credentials

**Update Domain:**
```bash
# Edit Nginx config
sudo nano /etc/nginx/sites-available/focusdeck
# Change server_name and SSL paths
sudo systemctl reload nginx
```

**Setup Let's Encrypt SSL (Recommended):**
```bash
sudo apt-get install -y certbot python3-certbot-nginx
sudo certbot certonly --nginx -d your-domain.com
# Update Nginx config with certificate paths
sudo systemctl reload nginx
```

**Check Service Status:**
```bash
sudo systemctl status focusdeck
sudo journalctl -u focusdeck -f  # View live logs
```

---

## 🔒 Security Best Practices

### Desktop
- Run as regular user (not Administrator unless needed)
- Keep Windows updated
- Use strong credentials for cloud sync
- Review file access permissions

### Mobile
- Install only from trusted sources
- Keep Android OS updated
- Review app permissions before granting
- Use device lock screen/biometric

### Server
- Use Let's Encrypt for HTTPS (not self-signed in production)
- Keep .NET runtime updated
- Configure firewall rules (only open 80/443)
- Use strong OAuth credentials
- Enable audit logging
- Regular backups of `/var/lib/focusdeck/`

---

## 🆘 Troubleshooting

### Desktop Won't Start
```powershell
# Check .NET installation
dotnet --version

# Try with verbose logging
.\FocusDeck.exe --verbose

# Check event logs
Get-EventLog -LogName Application -Source .NET | Select-Object -Last 5
```

### Mobile Won't Install
- Enable "Unknown Sources" in Settings → Security
- Verify device has Android 8.0+
- Try via ADB: `adb install *.apk`

### Server Connection Issues
```bash
# Check service
sudo systemctl status focusdeck

# Check logs
sudo journalctl -u focusdeck -n 50

# Test connectivity
curl -k https://localhost

# Check firewall
sudo ufw status
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
```

---

## 📝 Version Management

Check installed versions:

**Desktop:**
- About menu shows version
- Or: `.\FocusDeck.exe --version`

**Mobile:**
- Settings → About → Version

**Server:**
- `curl https://your-domain/api/version`
- Check release notes: https://github.com/dertder25t-png/FocusDeck/releases

---

## 🔄 Updating

### Desktop
- Download latest release
- Extract to new folder or overwrite old one
- Settings are preserved

### Mobile
- Download latest APK
- Install (will update existing app)
- Settings preserved

### Server
```bash
# Download latest release
cd /opt/focusdeck
sudo systemctl stop focusdeck
sudo rm -rf /opt/focusdeck/*
sudo tar -xzf focusdeck-server-v*.tar.gz
sudo systemctl start focusdeck
```

---

## 📧 Support

For installation issues:
1. Check troubleshooting section above
2. Review logs (see commands above)
3. Open issue on GitHub with:
   - OS version
   - Installation method
   - Error messages
   - Relevant logs
