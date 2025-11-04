#!/bin/bash

##############################################################################
# Complete FocusDeck Setup Fix
# This script fixes incomplete installations and deploys everything properly
##############################################################################

set -e

APP_DIR="/opt/focusdeck"
PUBLISH_DIR="$APP_DIR/publish"
SERVICE_USER="focusdeck"

echo "=== FocusDeck Complete Setup Fix ==="
echo ""

# 1. Create user if missing
if ! id "$SERVICE_USER" &>/dev/null; then
    echo "Creating service user '$SERVICE_USER'..."
    useradd --system --shell /bin/false --home "$APP_DIR" --create-home "$SERVICE_USER"
    echo "✓ User created"
else
    echo "✓ User '$SERVICE_USER' exists"
fi

# 2. Ensure repository exists
if [ ! -d "$APP_DIR/.git" ]; then
    echo "Cloning repository..."
    rm -rf "$APP_DIR"
    git clone https://github.com/dertder25t-png/FocusDeck.git "$APP_DIR"
    chown -R $SERVICE_USER:$SERVICE_USER "$APP_DIR"
    echo "✓ Repository cloned"
else
    echo "Updating repository..."
    cd "$APP_DIR"
    # Fix ownership issues
    chown -R $SERVICE_USER:$SERVICE_USER "$APP_DIR"
    sudo -u $SERVICE_USER git fetch --all
    sudo -u $SERVICE_USER git reset --hard origin/master
    sudo -u $SERVICE_USER git pull origin master
    echo "✓ Repository updated"
fi

# 3. Build and publish
echo "Building FocusDeck Server..."
cd "$APP_DIR/src/FocusDeck.Server"
sudo -u $SERVICE_USER dotnet build -c Release -v quiet
sudo -u $SERVICE_USER dotnet publish -c Release -o "$PUBLISH_DIR" -v quiet
echo "✓ Server built and published"

# 4. Deploy Web UI
echo "Deploying Web UI..."
mkdir -p "$PUBLISH_DIR/wwwroot"
cp -r "$APP_DIR/src/FocusDeck.Server/wwwroot/"* "$PUBLISH_DIR/wwwroot/"
chown -R $SERVICE_USER:$SERVICE_USER "$PUBLISH_DIR/wwwroot"

# Replace version placeholder
VERSION=$(date +%Y%m%d%H%M%S)
sed -i "s/__VERSION__/$VERSION/g" "$PUBLISH_DIR/wwwroot/index.html" 2>/dev/null || true

echo "✓ Web UI deployed"

# 5. Configure JWT secret
echo "Configuring JWT secret..."
JWT_SECRET=$(head -c 48 /dev/urandom | base64 | tr -d '\n' | head -c 64)

tee "$PUBLISH_DIR/appsettings.json" > /dev/null << EOF
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=focusdeck.db"
  },
  "Jwt": {
    "Key": "${JWT_SECRET}",
    "Issuer": "https://focusdeck.909436.xyz",
    "Audience": "focusdeck-clients"
  },
  "Cors": {
    "AllowedOrigins": [
      "https://focusdeck.909436.xyz",
      "http://localhost:5173"
    ]
  },
  "Storage": {
    "AssetsPath": "./assets",
    "MaxFileSizeBytes": 104857600
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning"
      }
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
EOF

chown $SERVICE_USER:$SERVICE_USER "$PUBLISH_DIR/appsettings.json"
chmod 600 "$PUBLISH_DIR/appsettings.json"
echo "✓ JWT secret configured"

# 6. Setup database permissions
echo "Setting up database..."
mkdir -p "$PUBLISH_DIR"
touch "$PUBLISH_DIR/focusdeck.db" 2>/dev/null || true
chown -R $SERVICE_USER:$SERVICE_USER "$PUBLISH_DIR"
echo "✓ Database permissions set"

# 7. Create systemd service
echo "Creating systemd service..."
tee /etc/systemd/system/focusdeck.service > /dev/null << 'EOF'
[Unit]
Description=FocusDeck Server
After=network.target

[Service]
Type=simple
User=focusdeck
Group=focusdeck
WorkingDirectory=/opt/focusdeck/publish
ExecStart=/usr/bin/dotnet /opt/focusdeck/publish/FocusDeck.Server.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=focusdeck
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000

# Security settings
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/opt/focusdeck/publish

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable focusdeck.service
echo "✓ Systemd service created"

# 8. Setup sudo permissions for updates
echo "Configuring sudo permissions..."
SUDOERS_FILE="/etc/sudoers.d/focusdeck"
tee "$SUDOERS_FILE" > /dev/null << EOF
$SERVICE_USER ALL=(ALL) NOPASSWD: /bin/systemctl restart focusdeck
$SERVICE_USER ALL=(ALL) NOPASSWD: /bin/systemctl stop focusdeck
$SERVICE_USER ALL=(ALL) NOPASSWD: /bin/systemctl start focusdeck
$SERVICE_USER ALL=(ALL) NOPASSWD: /bin/systemctl status focusdeck
EOF
chmod 0440 "$SUDOERS_FILE"
echo "✓ Sudo permissions configured"

# 9. Start the service
echo "Starting FocusDeck service..."
systemctl restart focusdeck.service
sleep 3

echo ""
echo "=== Installation Complete ==="
echo ""
echo "Service Status:"
systemctl status focusdeck --no-pager -l | head -20
echo ""
echo "Web UI files:"
ls -lh "$PUBLISH_DIR/wwwroot/" | head -10
echo ""
echo "✓ FocusDeck is running!"
echo ""
echo "Access URLs:"
echo "  Local:   http://localhost:5000"
echo "  Network: http://192.168.1.110:5000"
echo "  Domain:  https://focusdeck.909436.xyz (after Cloudflare setup)"
echo ""
echo "View logs: sudo journalctl -u focusdeck -f"
