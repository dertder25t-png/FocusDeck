#!/bin/bash

##############################################################################
# Deploy Web UI to FocusDeck Server
##############################################################################

set -e

APP_DIR="/opt/focusdeck"
PUBLISH_DIR="$APP_DIR/publish"

echo "Copying web UI files to publish directory..."

# Ensure wwwroot exists in publish
sudo mkdir -p "$PUBLISH_DIR/wwwroot"

# Copy all web files
sudo cp -r "$APP_DIR/src/FocusDeck.Server/wwwroot/"* "$PUBLISH_DIR/wwwroot/" 2>/dev/null || {
    echo "Error: Source wwwroot not found. Pulling latest from git..."
    cd "$APP_DIR"
    sudo -u focusdeck git pull origin master
    sudo cp -r "$APP_DIR/src/FocusDeck.Server/wwwroot/"* "$PUBLISH_DIR/wwwroot/"
}

# Set proper ownership
sudo chown -R focusdeck:focusdeck "$PUBLISH_DIR/wwwroot"

# Replace version placeholder in index.html if it exists
if [ -f "$PUBLISH_DIR/wwwroot/index.html" ]; then
    VERSION=$(date +%Y%m%d%H%M%S)
    sudo sed -i "s/__VERSION__/$VERSION/g" "$PUBLISH_DIR/wwwroot/index.html"
fi

echo "✓ Web UI files deployed"
echo ""
echo "Files in wwwroot:"
ls -lh "$PUBLISH_DIR/wwwroot/" | head -20

echo ""
echo "Restarting FocusDeck service..."
sudo systemctl restart focusdeck

sleep 2

echo ""
echo "Service status:"
sudo systemctl status focusdeck --no-pager -l

echo ""
echo "✓ Web UI should now be accessible at http://192.168.1.110:5000/"
