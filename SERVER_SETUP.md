# FocusDeck Server - Quick Setup Guide

Deploy your own FocusDeck sync server in minutes! 🚀

## One-Command Installation

```bash
curl -sSL https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/install.sh | sudo bash
```

That's it! The script will:
- ✅ Install .NET 9.0
- ✅ Clone the repository  
- ✅ Build the server
- ✅ Create a systemd service
- ✅ Start the server automatically

## What You Get

- 🌐 **Web Admin Panel** - Beautiful UI to manage your data
- 🔌 **REST API** - Sync with Windows/Mobile apps
- 📊 **Dashboard** - Real-time statistics
- 💾 **Data Management** - Export/import functionality

## Access Your Server

Once installed, open your browser:

```
http://YOUR_SERVER_IP:5000
```

## Requirements

- **OS**: Ubuntu 20.04+ or Debian 10+
- **RAM**: 512MB minimum (1GB recommended)
- **Disk**: 1GB free space
- **Network**: Internet connection

## Manual Installation

If you prefer to install step-by-step:

### 1. Install .NET 9.0
```bash
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 9.0

export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools
```

### 2. Clone Repository
```bash
cd ~
git clone https://github.com/dertder25t-png/FocusDeck.git
```

### 3. Build Server
```bash
cd ~/FocusDeck/src/FocusDeck.Server
dotnet publish FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained -o ~/focusdeck-server
```

### 4. Create Service
```bash
sudo nano /etc/systemd/system/focusdeck.service
```

Paste:
```ini
[Unit]
Description=FocusDeck Server
After=network.target

[Service]
Type=simple
WorkingDirectory=/root/focusdeck-server
ExecStart=/root/focusdeck-server/FocusDeck.Server
Restart=always
User=root
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000

[Install]
WantedBy=multi-user.target
```

### 5. Start Service
```bash
sudo systemctl daemon-reload
sudo systemctl enable focusdeck
sudo systemctl start focusdeck
```

## Management Commands

```bash
# View logs
sudo journalctl -u focusdeck -f

# Restart server
sudo systemctl restart focusdeck

# Check status  
sudo systemctl status focusdeck

# Stop server
sudo systemctl stop focusdeck
```

## Update Server

```bash
cd ~/FocusDeck
git pull
cd src/FocusDeck.Server
dotnet publish FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained -o ~/focusdeck-server
sudo systemctl restart focusdeck
```

## Optional: Setup HTTPS with Nginx

### Install Nginx
```bash
sudo apt install nginx -y
```

### Configure
```bash
sudo nano /etc/nginx/sites-available/focusdeck
```

Paste:
```nginx
server {
    listen 80;
    server_name your-domain.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

### Enable
```bash
sudo ln -s /etc/nginx/sites-available/focusdeck /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### Add SSL (Let's Encrypt)
```bash
sudo apt install certbot python3-certbot-nginx -y
sudo certbot --nginx -d your-domain.com
```

## Firewall Setup

```bash
# Allow port 5000
sudo ufw allow 5000/tcp

# Or if using Nginx
sudo ufw allow 'Nginx Full'

# Enable firewall
sudo ufw enable
```

## Troubleshooting

### Server won't start
```bash
# Check logs
sudo journalctl -u focusdeck -n 50

# Check if port is in use
sudo ss -tlnp | grep :5000

# Test manually
cd ~/focusdeck-server
./FocusDeck.Server
```

### Can't connect from other devices
```bash
# Check if firewall is blocking
sudo ufw status

# Allow port 5000
sudo ufw allow 5000/tcp

# Check server IP
hostname -I
```

### Permission errors
```bash
# Make executable
chmod +x ~/focusdeck-server/FocusDeck.Server

# Fix ownership
sudo chown -R $USER:$USER ~/focusdeck-server
```

## Configure Windows App

1. Open FocusDeck on Windows
2. Go to Settings → Sync
3. Enter: `http://YOUR_SERVER_IP:5000`
4. Click "Test Server" to verify connection

## Features

### Web UI
- Dashboard with statistics
- Create/edit/delete decks
- View all cards
- Export/import data
- Server configuration
- API documentation

### REST API Endpoints
- `GET /api/decks` - List all decks
- `GET /api/decks/{id}` - Get specific deck
- `POST /api/decks` - Create deck
- `PUT /api/decks/{id}` - Update deck
- `DELETE /api/decks/{id}` - Delete deck

## Security Recommendations

⚠️ **Important**: The current setup has no authentication!

For production use:
1. Set up HTTPS (see Nginx section above)
2. Configure a firewall
3. Keep your server updated
4. Add authentication (future feature)
5. Regular backups

## Storage

Currently uses **in-memory storage** - data is lost on restart.

For persistent storage, consider:
- Adding SQLite database
- Setting up PostgreSQL
- Using MySQL

## Support

- **Documentation**: [GitHub Wiki](https://github.com/dertder25t-png/FocusDeck/wiki)
- **Issues**: [GitHub Issues](https://github.com/dertder25t-png/FocusDeck/issues)
- **Discussions**: [GitHub Discussions](https://github.com/dertder25t-png/FocusDeck/discussions)

## Performance

**Recommended specs for different loads:**

| Users | RAM | CPU | Disk |
|-------|-----|-----|------|
| 1-5   | 512MB | 1 core | 1GB |
| 5-20  | 1GB | 2 cores | 2GB |
| 20-50 | 2GB | 2 cores | 5GB |
| 50+   | 4GB+ | 4 cores | 10GB+ |

## License

See [LICENSE](../LICENSE) file for details.

---

Made with ❤️ for productivity enthusiasts
