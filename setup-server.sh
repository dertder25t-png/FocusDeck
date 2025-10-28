#!/bin/bash

##############################################################################
# FocusDeck Server Setup Script
# Linux (Proxmox VM) - Automated deployment for FocusDeck Web Server
#
# This script sets up:
# - .NET 8 Runtime
# - Database (SQL Server or SQLite)
# - Web Server Infrastructure
# - SSL/TLS Configuration
# - Systemd Service
# - Nginx Reverse Proxy
#
# Usage: sudo bash setup-server.sh
##############################################################################

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
APP_NAME="FocusDeck"
APP_USER="focusdeck"
APP_GROUP="focusdeck"
APP_DIR="/opt/focusdeck"
DB_DIR="/var/lib/focusdeck"
CONFIG_DIR="/etc/focusdeck"
LOG_DIR="/var/log/focusdeck"
DOMAIN="${DOMAIN:-focusdeck.local}"
PORT_HTTP=80
PORT_HTTPS=443
PORT_APP=5000

echo -e "${BLUE}╔════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║        FocusDeck Server Setup - Linux/Proxmox VM          ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   echo -e "${RED}This script must be run as root${NC}"
   exit 1
fi

# Detect OS
if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    if [[ -f /etc/os-release ]]; then
        . /etc/os-release
        OS=$NAME
        VER=$VERSION_ID
    elif type lsb_release >/dev/null 2>&1; then
        OS=$(lsb_release -si)
        VER=$(lsb_release -sr)
    fi
fi

echo -e "${GREEN}✓ Detected OS: $OS $VER${NC}"
echo ""

# Function to print section headers
print_section() {
    echo -e "${YELLOW}═══════════════════════════════════════════════════════════${NC}"
    echo -e "${YELLOW}  $1${NC}"
    echo -e "${YELLOW}═══════════════════════════════════════════════════════════${NC}"
}

# Function to print status
print_status() {
    echo -e "${GREEN}✓ $1${NC}"
}

# Function to print error
print_error() {
    echo -e "${RED}✗ $1${NC}"
}

# Step 1: Update system packages
print_section "Step 1: Update System Packages"
apt-get update -qq
apt-get upgrade -y -qq
print_status "System packages updated"
echo ""

# Step 2: Install dependencies
print_section "Step 2: Install Dependencies"
apt-get install -y -qq curl wget git build-essential libssl-dev nginx supervisor
print_status "Dependencies installed (curl, wget, git, build-essential, libssl-dev, nginx, supervisor)"
echo ""

# Step 3: Install .NET 8 Runtime
print_section "Step 3: Install .NET 8 Runtime"
if ! command -v dotnet &> /dev/null; then
    # Add Microsoft package repository
    wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
    chmod +x dotnet-install.sh
    ./dotnet-install.sh --channel 8.0
    rm dotnet-install.sh
    
    # Add to PATH
    echo 'export PATH=$PATH:/root/.dotnet' >> ~/.bashrc
    source ~/.bashrc
    print_status ".NET 8 Runtime installed"
else
    print_status ".NET Runtime already installed ($(dotnet --version))"
fi
echo ""

# Step 4: Create application user and directories
print_section "Step 4: Setup Application User & Directories"
if ! id "$APP_USER" &>/dev/null; then
    useradd -m -s /bin/false -d "$APP_DIR" "$APP_USER"
    print_status "Created app user: $APP_USER"
else
    print_status "App user already exists: $APP_USER"
fi

# Create directories
mkdir -p "$APP_DIR" "$DB_DIR" "$CONFIG_DIR" "$LOG_DIR"
chown -R "$APP_USER:$APP_GROUP" "$APP_DIR" "$DB_DIR" "$CONFIG_DIR" "$LOG_DIR"
chmod 750 "$APP_DIR" "$DB_DIR" "$CONFIG_DIR" "$LOG_DIR"
print_status "Directories created and permissions set"
echo ""

# Step 5: Configure environment file
print_section "Step 5: Configure Environment"
cat > "$CONFIG_DIR/appsettings.json" << 'EOF'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/var/lib/focusdeck/focusdeck.db"
  },
  "AppSettings": {
    "Environment": "Production",
    "AllowedHosts": "*",
    "JwtSecret": "your-secret-key-change-in-production",
    "RefreshTokenExpiryDays": 7,
    "AccessTokenExpiryMinutes": 60
  },
  "OAuth": {
    "GoogleDrive": {
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret",
      "Enabled": false
    },
    "OneDrive": {
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret",
      "Enabled": false
    }
  },
  "AllowedOrigins": [
    "http://localhost",
    "https://focusdeck.local"
  ]
}
EOF
chown "$APP_USER:$APP_GROUP" "$CONFIG_DIR/appsettings.json"
chmod 640 "$CONFIG_DIR/appsettings.json"
print_status "Environment configuration created"
echo ""

# Step 6: Setup Nginx reverse proxy
print_section "Step 6: Configure Nginx Reverse Proxy"
cat > /etc/nginx/sites-available/focusdeck << EOF
upstream focusdeck_app {
    server localhost:$PORT_APP;
}

server {
    listen $PORT_HTTP;
    listen [$::]:$PORT_HTTP;
    server_name $DOMAIN;
    return 301 https://\$server_name\$request_uri;
}

server {
    listen $PORT_HTTPS ssl http2;
    listen [$::]:$PORT_HTTPS ssl http2;
    server_name $DOMAIN;
    
    # SSL certificates (self-signed by default)
    ssl_certificate /etc/nginx/certs/focusdeck.crt;
    ssl_certificate_key /etc/nginx/certs/focusdeck.key;
    
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;
    
    client_max_body_size 100M;
    
    access_log $LOG_DIR/nginx_access.log combined;
    error_log $LOG_DIR/nginx_error.log;
    
    location / {
        proxy_pass http://focusdeck_app;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
        
        # WebSocket support
        proxy_read_timeout 86400;
    }
}
EOF

# Enable site
ln -sf /etc/nginx/sites-available/focusdeck /etc/nginx/sites-enabled/focusdeck
print_status "Nginx configuration created"
echo ""

# Step 7: Generate self-signed SSL certificates (if not present)
print_section "Step 7: Generate SSL Certificates"
mkdir -p /etc/nginx/certs
if [[ ! -f /etc/nginx/certs/focusdeck.key ]]; then
    openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
        -keyout /etc/nginx/certs/focusdeck.key \
        -out /etc/nginx/certs/focusdeck.crt \
        -subj "/C=US/ST=State/L=City/O=Organization/CN=$DOMAIN"
    chmod 600 /etc/nginx/certs/focusdeck.key
    print_status "Self-signed SSL certificate generated (valid for 1 year)"
    echo -e "${YELLOW}  For production, replace with a valid certificate from Let's Encrypt or your CA${NC}"
else
    print_status "SSL certificates already exist"
fi
echo ""

# Step 8: Configure systemd service
print_section "Step 8: Configure Systemd Service"
cat > /etc/systemd/system/focusdeck.service << EOF
[Unit]
Description=FocusDeck Web Server
After=network-online.target
Wants=network-online.target

[Service]
Type=notify
User=$APP_USER
Group=$APP_GROUP
WorkingDirectory=$APP_DIR

# Environment variables
Environment="DOTNET_ENVIRONMENT=Production"
Environment="ASPNETCORE_URLS=http://localhost:$PORT_APP"
EnvironmentFile=$CONFIG_DIR/.env

# Restart policy
Restart=always
RestartSec=10

# Security
NoNewPrivileges=true
PrivateTmp=true

# Resource limits
LimitNOFILE=65535
LimitNPROC=65535

# Start command (update with actual binary name)
ExecStart=$APP_DIR/FocusDeck.Server

# Logging
StandardOutput=journal
StandardError=journal
SyslogIdentifier=focusdeck

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
print_status "Systemd service configured"
echo ""

# Step 9: Test Nginx configuration
print_section "Step 9: Verify Nginx Configuration"
if nginx -t 2>/dev/null; then
    print_status "Nginx configuration is valid"
    systemctl restart nginx
    print_status "Nginx restarted"
else
    print_error "Nginx configuration test failed"
fi
echo ""

# Step 10: Create startup instructions
print_section "Step 10: Setup Complete!"
echo ""
echo -e "${BLUE}Next Steps:${NC}"
echo -e "  1. ${YELLOW}Download the server release from GitHub${NC}"
echo -e "     URL: https://github.com/dertder25t-png/FocusDeck/releases"
echo -e "     Download: focusdeck-server-*.tar.gz"
echo ""
echo -e "  2. ${YELLOW}Extract to application directory${NC}"
echo -e "     tar -xzf focusdeck-server-*.tar.gz -C $APP_DIR"
echo ""
echo -e "  3. ${YELLOW}Set permissions${NC}"
echo -e "     chown -R $APP_USER:$APP_GROUP $APP_DIR"
echo -e "     chmod +x $APP_DIR/FocusDeck.Server"
echo ""
echo -e "  4. ${YELLOW}Update environment configuration${NC}"
echo -e "     Edit: $CONFIG_DIR/appsettings.json"
echo -e "     Set OAuth credentials if needed"
echo ""
echo -e "  5. ${YELLOW}Start the service${NC}"
echo -e "     systemctl enable focusdeck"
echo -e "     systemctl start focusdeck"
echo ""
echo -e "  6. ${YELLOW}Check service status${NC}"
echo -e "     systemctl status focusdeck"
echo -e "     journalctl -u focusdeck -f"
echo ""
echo -e "${BLUE}Configuration Files:${NC}"
echo -e "  Application Settings: $CONFIG_DIR/appsettings.json"
echo -e "  Database Location:    $DB_DIR/focusdeck.db"
echo -e "  Log Files:            $LOG_DIR/"
echo -e "  SSL Certificates:     /etc/nginx/certs/"
echo ""
echo -e "${BLUE}Access Points:${NC}"
echo -e "  HTTP  (redirects to HTTPS): http://$DOMAIN"
echo -e "  HTTPS:                      https://$DOMAIN"
echo -e "  Local API:                  http://localhost:$PORT_APP"
echo ""
echo -e "${GREEN}✓ Server setup completed successfully!${NC}"
