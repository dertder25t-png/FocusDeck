#!/bin/bash

##############################################################################
# Quick Fix: Generate proper JWT secret for FocusDeck
##############################################################################

set -e

echo "Stopping FocusDeck service (if running)..."
sudo systemctl stop focusdeck 2>/dev/null || echo "Service not running, continuing..."

echo "Generating secure JWT secret..."
JWT_SECRET=$(head -c 48 /dev/urandom | base64 | tr -d '\n' | head -c 64)

echo "Backing up current config..."
sudo cp /opt/focusdeck/publish/appsettings.json /opt/focusdeck/publish/appsettings.json.backup

echo "Updating JWT key in config..."
sudo tee /opt/focusdeck/publish/appsettings.json > /dev/null << EOF
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
      "https://focusdeck.909436.xyz"
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

sudo chown focusdeck:focusdeck /opt/focusdeck/publish/appsettings.json
sudo chmod 600 /opt/focusdeck/publish/appsettings.json

echo "Verifying JWT key was set..."
if grep -q "super_dev_secret\|change_me\|your-" /opt/focusdeck/publish/appsettings.json; then
    echo "ERROR: JWT key still contains placeholder!"
    exit 1
fi

echo "✓ JWT secret configured: ${JWT_SECRET:0:20}..."

echo "Restarting FocusDeck service..."
sudo systemctl restart focusdeck 2>/dev/null || echo "Service will be started by systemd..."

sleep 3

echo ""
echo "Checking service status..."
sudo systemctl status focusdeck --no-pager -l

echo ""
echo "Recent logs:"
sudo journalctl -u focusdeck -n 10 --no-pager
