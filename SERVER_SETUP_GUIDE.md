# FocusDeck Server Setup Guide

Complete guide for setting up FocusDeck on Ubuntu/Debian servers with all the latest security features and Cloudflare integration.

## Quick Start

### One-Command Install (New Installation)

```bash
wget https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/complete-setup.sh
sudo bash complete-setup.sh
```

This script will:
- ✅ Install .NET 9.0 SDK
- ✅ Install Cloudflare Tunnel (optional)
- ✅ Clone and build FocusDeck
- ✅ Configure secure JWT authentication (64-char random key)
- ✅ Set up CORS for your domain
- ✅ Create systemd service
- ✅ Initialize database with sync versioning support
- ✅ Enable web-based updates

### Quick Update (Existing Installation)

```bash
cd /opt/focusdeck
wget https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/update-server.sh
sudo bash update-server.sh
```

## What's New in This Version

### Security Improvements
- **JWT Key Validation**: Server enforces 32+ character keys in production
- **CORS Validation**: Strict origin checking with proper error messages
- **Auth Hardening**: Removed "default-user" fallback in sync endpoints
- **Admin-Only Hangfire**: Dashboard requires admin role/claims

### Sync System Improvements
- **Durable Versioning**: Database-backed sync version counter (thread-safe)
- **Atomic Version Allocation**: Prevents race conditions in multi-device sync
- **SyncVersion Table**: New migration for persistent version tracking

### Infrastructure
- **Better Health Checks**: Filesystem write checks with orphan cleanup
- **Telemetry Throttling**: Prevents SignalR flooding with backpressure metrics
- **Improved Error Handling**: Comprehensive error envelopes

## Detailed Setup

### Prerequisites

- Ubuntu 20.04+ or Debian 11+
- Root or sudo access
- (Optional) Cloudflare account for tunnel setup

### Step 1: Download and Run Setup

```bash
# Download the setup script
wget https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/complete-setup.sh

# Make it executable
chmod +x complete-setup.sh

# Run as root
sudo bash complete-setup.sh
```

### Step 2: Follow Interactive Prompts

The script will ask:

1. **Cloudflare Tunnel**: Do you want to install cloudflared?
   - **Recommended: Yes** - Provides secure HTTPS, preserves client IPs for auth
   - **No**: You'll need to configure an external tunnel manually

2. **Domain Name**: Enter your domain (e.g., focusdeck.example.com)
   - Leave blank for localhost-only testing
   - This configures JWT issuer and CORS origins

### Step 3: Cloudflare Tunnel Configuration (If Installed)

After installation, configure your tunnel:

```bash
# 1. Login to Cloudflare
sudo cloudflared tunnel login

# 2. Create a tunnel
sudo cloudflared tunnel create focusdeck

# 3. Note your Tunnel ID and credentials file path
# They will be displayed after creation

# 4. Create config file
sudo nano /etc/cloudflared/config.yml
```

Add this configuration (replace placeholders):

```yaml
tunnel: YOUR-TUNNEL-ID-HERE
credentials-file: /root/.cloudflared/YOUR-CREDENTIALS-FILE.json

ingress:
  - hostname: your-domain.com
    service: http://localhost:5000
    originRequest:
      noTLSVerify: false
  - service: http_status:404
```

```bash
# 5. Route DNS
sudo cloudflared tunnel route dns focusdeck your-domain.com

# 6. Install and start service
sudo cloudflared service install
sudo systemctl start cloudflared
sudo systemctl enable cloudflared

# 7. Verify it's running
sudo systemctl status cloudflared
```

### Step 4: Verify Installation

```bash
# Check service status
sudo systemctl status focusdeck

# View live logs
sudo journalctl -u focusdeck -f

# Test locally
curl http://localhost:5000/healthz
```

Expected response:
```json
{"ok":true,"time":"2024-11-03T..."}
```

## Configuration

### Location

Configuration file: `/opt/focusdeck/publish/appsettings.json`

### Important Settings

#### JWT Configuration
```json
"Jwt": {
  "Key": "your-64-character-random-key-here",
  "Issuer": "https://your-domain.com",
  "Audience": "focusdeck-clients"
}
```

- **Key**: Must be 32+ characters (auto-generated as 64 chars)
- **Issuer**: Your domain with https://
- **Audience**: Usually `focusdeck-clients`

#### CORS Configuration
```json
"Cors": {
  "AllowedOrigins": [
    "https://your-domain.com",
    "http://localhost:5173"
  ]
}
```

Add any additional domains your frontend will use.

#### Database
```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=focusdeck.db"
}
```

For PostgreSQL:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=focusdeck;Username=focusdeck;Password=yourpassword"
}
```

### After Configuration Changes

```bash
sudo systemctl restart focusdeck
```

## Management Commands

### Service Control

```bash
# Start service
sudo systemctl start focusdeck

# Stop service
sudo systemctl stop focusdeck

# Restart service
sudo systemctl restart focusdeck

# Check status
sudo systemctl status focusdeck

# Enable auto-start on boot
sudo systemctl enable focusdeck
```

### Logs

```bash
# View recent logs
sudo journalctl -u focusdeck -n 100

# Follow logs live
sudo journalctl -u focusdeck -f

# View logs since boot
sudo journalctl -u focusdeck -b
```

### Updates

**Option 1: Web UI (Recommended)**
1. Go to Settings → Server Management
2. Click "Update Server Now"
3. Wait for automatic restart

**Option 2: Manual Script**
```bash
cd /opt/focusdeck
sudo bash update-server.sh
```

**Option 3: Manual Steps**
```bash
cd /opt/focusdeck
sudo systemctl stop focusdeck
sudo -u focusdeck git pull origin master
cd src/FocusDeck.Server
sudo -u focusdeck dotnet publish -c Release -o /opt/focusdeck/publish
sudo systemctl start focusdeck
```

## Troubleshooting

### Service Won't Start

```bash
# Check detailed logs
sudo journalctl -u focusdeck -n 100 --no-pager

# Common issues:
# 1. Port 5000 already in use
sudo lsof -i :5000

# 2. Database permission issues
sudo chown -R focusdeck:focusdeck /opt/focusdeck/publish

# 3. Configuration errors
sudo -u focusdeck dotnet /opt/focusdeck/publish/FocusDeck.Server.dll
```

### Can't Access from Browser

```bash
# Check if service is running
sudo systemctl status focusdeck

# Check firewall
sudo ufw status
sudo ufw allow 5000/tcp  # If using UFW

# Check if listening
sudo netstat -tlnp | grep 5000
```

### Cloudflare Tunnel Issues

```bash
# Check tunnel status
sudo systemctl status cloudflared

# View tunnel logs
sudo journalctl -u cloudflared -n 100

# Test tunnel connection
sudo cloudflared tunnel info focusdeck

# Restart tunnel
sudo systemctl restart cloudflared
```

### Database Migration Issues

The server uses SQLite by default with auto-migration on startup. If you see errors:

```bash
# Check database permissions
ls -la /opt/focusdeck/publish/focusdeck.db

# Reset database (WARNING: deletes all data)
sudo systemctl stop focusdeck
sudo rm /opt/focusdeck/publish/focusdeck.db
sudo systemctl start focusdeck
```

### JWT/Auth Errors

If you see "JWT key too short" or similar:

```bash
# Edit config
sudo nano /opt/focusdeck/publish/appsettings.json

# Ensure JWT.Key is at least 32 characters
# Generate a new secure key:
head -c 48 /dev/urandom | base64 | tr -d '\n' | head -c 64

# Restart service
sudo systemctl restart focusdeck
```

## Security Best Practices

1. **Strong JWT Key**: Always use a random 64+ character key
2. **CORS Configuration**: Only list domains you control
3. **Firewall**: Block port 5000 if using Cloudflare Tunnel
4. **HTTPS Only**: Never expose port 5000 directly to internet
5. **Regular Updates**: Use web UI or run update script weekly
6. **Database Backups**: 
   ```bash
   cp /opt/focusdeck/publish/focusdeck.db ~/backup-$(date +%s).db
   ```

## Architecture

### File Structure
```
/opt/focusdeck/
├── src/                    # Source code
│   └── FocusDeck.Server/
├── publish/                # Compiled application
│   ├── FocusDeck.Server.dll
│   ├── appsettings.json   # Configuration
│   ├── focusdeck.db       # SQLite database
│   └── wwwroot/           # Web UI files
└── .git/                  # Git repository
```

### Service User
- User: `focusdeck`
- Home: `/opt/focusdeck`
- Shell: `/bin/false` (no login)
- Purpose: Runs the application with minimal privileges

### Network
- Listens on: `0.0.0.0:5000`
- Cloudflare Tunnel connects to: `localhost:5000`
- No direct internet exposure needed

## Monitoring

### Health Check Endpoint

```bash
curl http://localhost:5000/healthz
```

Returns: `{"ok":true,"time":"..."}`

### Detailed Health

```bash
curl http://localhost:5000/v1/system/health
```

Returns database, filesystem, and other subsystem health.

### Metrics

View in web UI: Settings → Server Management → Server Status

Or via API: `GET /api/v1/system/metrics`

## Advanced Configuration

### PostgreSQL Setup

1. Install PostgreSQL:
```bash
sudo apt install postgresql
```

2. Create database and user:
```bash
sudo -u postgres psql
CREATE DATABASE focusdeck;
CREATE USER focusdeck WITH ENCRYPTED PASSWORD 'yourpassword';
GRANT ALL PRIVILEGES ON DATABASE focusdeck TO focusdeck;
\q
```

3. Update connection string in appsettings.json
4. Restart service

### Hangfire Dashboard (PostgreSQL only)

Access at: `http://localhost:5000/hangfire`

Requires admin authentication (configured in web UI).

### Custom Domain with Let's Encrypt

If not using Cloudflare Tunnel, you can use nginx + certbot:

```bash
# Install nginx
sudo apt install nginx certbot python3-certbot-nginx

# Create nginx config
sudo nano /etc/nginx/sites-available/focusdeck

# Add:
server {
    listen 80;
    server_name your-domain.com;
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}

# Enable site
sudo ln -s /etc/nginx/sites-available/focusdeck /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx

# Get SSL certificate
sudo certbot --nginx -d your-domain.com
```

## Support

- GitHub Issues: https://github.com/dertder25t-png/FocusDeck/issues
- Documentation: Check `/docs` folder in repository
- Logs: `sudo journalctl -u focusdeck -f`

## License

See LICENSE file in repository.
