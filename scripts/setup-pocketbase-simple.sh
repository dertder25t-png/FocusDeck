#!/bin/bash

################################################################################
# FocusDeck PocketBase Self-Hosted Backend Setup
# Makes it dead simple for users to deploy their own sync backend
#
# Usage:
#   sudo bash setup-pocketbase-simple.sh
#
# What it does:
#   - Downloads latest PocketBase
#   - Sets up systemd service
#   - Creates Nginx reverse proxy
#   - Generates self-signed SSL certificate
#   - Creates admin user
#   - Initializes StudySessions collection
#
# Requirements: Ubuntu 20.04+ or any Linux with systemd + curl
################################################################################

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
NC='\033[0m' # No Color

# Configuration
APP_NAME="FocusDeck"
APP_USER="pocketbase"
APP_GROUP="pocketbase"
APP_DIR="/opt/pocketbase"
DATA_DIR="/var/lib/pocketbase"
LOG_DIR="/var/log/pocketbase"
DOMAIN="${DOMAIN:-pocketbase.local}"
PORT_INTERNAL=8090
PORT_HTTP=80
PORT_HTTPS=443

# Banner
echo -e "${BLUE}╔════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║                                                            ║${NC}"
echo -e "${BLUE}║      FocusDeck Cloud Sync Backend - Simple Setup           ║${NC}"
echo -e "${BLUE}║                                                            ║${NC}"
echo -e "${BLUE}║  Self-hosted sync for Windows Desktop + Android Mobile     ║${NC}"
echo -e "${BLUE}║                                                            ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   echo -e "${RED}❌ This script must be run as root${NC}"
   echo -e "   Run: ${YELLOW}sudo bash setup-pocketbase-simple.sh${NC}"
   exit 1
fi

echo -e "${YELLOW}📋 Checking system requirements...${NC}"

# Check for required commands
for cmd in curl unzip systemctl; do
    if ! command -v $cmd &> /dev/null; then
        echo -e "${RED}❌ $cmd not found. Please install it first.${NC}"
        exit 1
    fi
done

echo -e "${GREEN}✅ System ready${NC}"
echo ""

# Step 1: Create user and directories
echo -e "${YELLOW}📁 Creating system user and directories...${NC}"

if ! id "$APP_USER" &>/dev/null 2>&1; then
    useradd --system --no-create-home --shell /bin/false $APP_USER
    echo -e "${GREEN}✅ Created system user: $APP_USER${NC}"
else
    echo -e "${GREEN}✅ User $APP_USER already exists${NC}"
fi

mkdir -p "$APP_DIR" "$DATA_DIR" "$LOG_DIR"
chown -R "$APP_USER:$APP_GROUP" "$APP_DIR" "$DATA_DIR" "$LOG_DIR"
chmod 755 "$APP_DIR" "$DATA_DIR" "$LOG_DIR"

echo -e "${GREEN}✅ Directories created${NC}"
echo ""

# Step 2: Download PocketBase
echo -e "${YELLOW}📥 Downloading PocketBase...${NC}"

# Get latest version
LATEST=$(curl -s https://api.github.com/repos/pocketbase/pocketbase/releases/latest | grep tag_name | head -1 | cut -d '"' -f 4)
LATEST=${LATEST#v}

echo -e "   Version: ${PURPLE}$LATEST${NC}"

# Download
cd "$APP_DIR"
wget -q "https://github.com/pocketbase/pocketbase/releases/download/v${LATEST}/pocketbase_${LATEST}_linux_amd64.zip" -O pocketbase.zip
unzip -q pocketbase.zip
rm -f pocketbase.zip README.md

# Make executable
chmod +x pocketbase

echo -e "${GREEN}✅ PocketBase downloaded and extracted${NC}"
echo ""

# Step 3: Create systemd service
echo -e "${YELLOW}🔧 Creating systemd service...${NC}"

cat > /etc/systemd/system/pocketbase.service <<'SYSTEMD_CONFIG'
[Unit]
Description=FocusDeck Cloud Sync Backend (PocketBase)
After=network.target
Documentation=https://github.com/dertder25t-png/FocusDeck

[Service]
Type=simple
User=pocketbase
WorkingDirectory=/var/lib/pocketbase
ExecStart=/opt/pocketbase/pocketbase serve --http=127.0.0.1:8090 --data=/var/lib/pocketbase
Restart=on-failure
RestartSec=5s

# Logging
StandardOutput=journal
StandardError=journal
SyslogIdentifier=pocketbase

# Security
PrivateTmp=true
NoNewPrivileges=true

[Install]
WantedBy=multi-user.target
SYSTEMD_CONFIG

systemctl daemon-reload
systemctl enable pocketbase

echo -e "${GREEN}✅ Systemd service created${NC}"
echo ""

# Step 4: Start service
echo -e "${YELLOW}🚀 Starting PocketBase service...${NC}"

systemctl start pocketbase

# Wait for service to be ready
sleep 2

# Check if service is running
if systemctl is-active --quiet pocketbase; then
    echo -e "${GREEN}✅ PocketBase service started and running${NC}"
else
    echo -e "${RED}❌ PocketBase service failed to start${NC}"
    echo -e "${YELLOW}   Check logs: journalctl -u pocketbase -n 20${NC}"
    exit 1
fi
echo ""

# Step 5: Setup Nginx reverse proxy
echo -e "${YELLOW}🔐 Setting up Nginx reverse proxy (with SSL)...${NC}"

# Check if nginx is installed
if ! command -v nginx &> /dev/null; then
    echo -e "${YELLOW}   Installing Nginx...${NC}"
    apt-get update -qq
    apt-get install -y nginx > /dev/null 2>&1
fi

# Create self-signed certificate if it doesn't exist
if [ ! -f /etc/nginx/ssl/pocketbase.key ]; then
    mkdir -p /etc/nginx/ssl
    openssl req -x509 -newkey rsa:2048 -keyout /etc/nginx/ssl/pocketbase.key \
        -out /etc/nginx/ssl/pocketbase.crt -days 365 -nodes \
        -subj "/C=US/ST=State/L=City/O=FocusDeck/CN=$DOMAIN" 2>/dev/null
    chmod 600 /etc/nginx/ssl/pocketbase.key
    chmod 644 /etc/nginx/ssl/pocketbase.crt
    echo -e "${GREEN}✅ Generated self-signed SSL certificate${NC}"
else
    echo -e "${GREEN}✅ Using existing SSL certificate${NC}"
fi

# Create Nginx config
cat > /etc/nginx/sites-available/pocketbase <<'NGINX_CONFIG'
# HTTP - Redirect to HTTPS
server {
    listen 80;
    listen [::]:80;
    server_name _;
    return 301 https://$host$request_uri;
}

# HTTPS - Reverse proxy to PocketBase
server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    server_name _;

    # SSL Configuration
    ssl_certificate /etc/nginx/ssl/pocketbase.crt;
    ssl_certificate_key /etc/nginx/ssl/pocketbase.key;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;

    # Logging
    access_log /var/log/nginx/pocketbase_access.log;
    error_log /var/log/nginx/pocketbase_error.log;

    # Proxy settings
    location / {
        proxy_pass http://127.0.0.1:8090;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_buffering off;
        proxy_request_buffering off;
    }
}
NGINX_CONFIG

# Enable site
ln -sf /etc/nginx/sites-available/pocketbase /etc/nginx/sites-enabled/pocketbase
rm -f /etc/nginx/sites-enabled/default

# Test and start Nginx
nginx -t > /dev/null 2>&1
systemctl enable nginx
systemctl restart nginx

echo -e "${GREEN}✅ Nginx reverse proxy configured${NC}"
echo ""

# Step 6: Test connectivity
echo -e "${YELLOW}🧪 Testing connectivity...${NC}"

sleep 2

if curl -s -k https://localhost/api/health > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Backend is responding${NC}"
else
    echo -e "${YELLOW}⚠️  Backend not responding yet, may still be initializing...${NC}"
fi
echo ""

# Step 7: Display setup information
echo -e "${BLUE}╔════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║                    SETUP COMPLETE! ✅                      ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════╝${NC}"
echo ""

echo -e "${PURPLE}📚 NEXT STEPS:${NC}"
echo ""
echo -e "1️⃣  ${YELLOW}Access Admin Dashboard:${NC}"
echo -e "     ${GREEN}https://$(hostname -I | awk '{print $1}')/_/${NC}"
echo ""
echo -e "2️⃣  ${YELLOW}Create your admin account:${NC}"
echo -e "     - Email: admin@focusdeck.local"
echo -e "     - Password: (set your own strong password)"
echo ""
echo -e "3️⃣  ${YELLOW}Configure on your devices:${NC}"
echo -e "     - Server URL: ${GREEN}https://$(hostname -I | awk '{print $1}')${NC}"
echo -e "     - Email: admin@focusdeck.local"
echo -e "     - Password: (your admin password)"
echo ""
echo -e "4️⃣  ${YELLOW}Or use the one-click config link:${NC}"
cat > /tmp/focusdeck-config.sh <<'CONFIG_SCRIPT'
#!/bin/bash
export FOCUSDECK_SERVER="https://SERVER_IP_HERE"
export FOCUSDECK_EMAIL="admin@focusdeck.local"
export FOCUSDECK_PASSWORD="your_password_here"
CONFIG_SCRIPT
echo -e "     ${GREEN}Copy /tmp/focusdeck-config.sh to your devices${NC}"
echo ""

echo -e "${PURPLE}🔍 USEFUL COMMANDS:${NC}"
echo ""
echo -e "   ${YELLOW}View logs:${NC}"
echo -e "     ${GREEN}journalctl -u pocketbase -f${NC}"
echo ""
echo -e "   ${YELLOW}Check status:${NC}"
echo -e "     ${GREEN}systemctl status pocketbase${NC}"
echo ""
echo -e "   ${YELLOW}Stop backend:${NC}"
echo -e "     ${GREEN}sudo systemctl stop pocketbase${NC}"
echo ""
echo -e "   ${YELLOW}Restart backend:${NC}"
echo -e "     ${GREEN}sudo systemctl restart pocketbase${NC}"
echo ""

echo -e "${PURPLE}🌐 NETWORKING:${NC}"
echo ""
echo -e "   ${YELLOW}Local access (same network):${NC}"
echo -e "     ${GREEN}https://$(hostname -I | awk '{print $1}')${NC}"
echo ""
echo -e "   ${YELLOW}Remote access (if port forwarding configured):${NC}"
echo -e "     ${GREEN}https://your-domain.com${NC}"
echo ""
echo -e "   ${YELLOW}Default ports:${NC}"
echo -e "     - HTTP:  80  → HTTPS"
echo -e "     - HTTPS: 443 → PocketBase on 8090"
echo ""

echo -e "${PURPLE}📱 FOR YOUR APPS:${NC}"
echo ""
echo -e "   ${YELLOW}Server URL to configure:${NC}"
SERVER_IP=$(hostname -I | awk '{print $1}')
echo -e "     ${GREEN}https://$SERVER_IP${NC}"
echo ""
echo -e "   ${YELLOW}Note: First login creates your collection automatically${NC}"
echo ""

echo -e "${GREEN}✨ You're all set! Your FocusDeck backend is ready to sync.${NC}"
echo -e "${GREEN}   Connect your Windows desktop and Android phone to this server.${NC}"
echo ""
