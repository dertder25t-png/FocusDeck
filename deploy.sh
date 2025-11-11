#!/bin/bash
set -e

echo "🚀 FocusDeck Authentication System Deployment"
echo "=============================================="
echo ""
echo "📅 Date: $(date)"
echo "🌳 Branch: $(git branch --show-current)"
echo ""

# Step 1: Stop the service
echo "⏹️  Stopping FocusDeck service..."
sudo systemctl stop focusdeck
sleep 2
echo "✅ Service stopped"
echo ""

# Step 2: Backup current deployment
echo "💾 Backing up current deployment..."
BACKUP_DIR="/home/focusdeck/FocusDeck/backup-$(date +%Y%m%d-%H%M%S)"
sudo cp -r /home/focusdeck/FocusDeck/publish "$BACKUP_DIR"
echo "✅ Backup created at: $BACKUP_DIR"
echo ""

# Step 3: Build React SPA
echo "🏗️  Building React SPA..."
cd /root/FocusDeck/src/FocusDeck.WebApp
npm install --production
npm run build
echo "✅ React build complete"
echo ""

# Step 4: Clean wwwroot and copy new build
echo "📦 Deploying React build..."
sudo rm -rf /home/focusdeck/FocusDeck/publish/wwwroot/*
sudo cp -r /root/FocusDeck/src/FocusDeck.WebApp/dist/* /home/focusdeck/FocusDeck/publish/wwwroot/
sudo chown -R focusdeck:focusdeck /home/focusdeck/FocusDeck/publish/wwwroot/
echo "✅ React build deployed"
echo ""

# Step 5: Build .NET Server
echo "🔨 Building .NET Server..."
cd /root/FocusDeck/src/FocusDeck.Server
dotnet build -c Release
dotnet publish -c Release -o /tmp/focusdeck-publish
echo "✅ .NET build complete"
echo ""

# Step 6: Deploy .NET binaries (preserve wwwroot)
echo "📦 Deploying .NET binaries..."
# Preserve wwwroot directory
sudo mkdir -p /tmp/wwwroot-backup
sudo cp -r /home/focusdeck/FocusDeck/publish/wwwroot /tmp/wwwroot-backup/
# Remove old publish directory
sudo rm -rf /home/focusdeck/FocusDeck/publish/*
# Copy new binaries
sudo cp -r /tmp/focusdeck-publish/* /home/focusdeck/FocusDeck/publish/
# Restore wwwroot
sudo cp -r /tmp/wwwroot-backup/wwwroot /home/focusdeck/FocusDeck/publish/
# Fix permissions
sudo chown -R focusdeck:focusdeck /home/focusdeck/FocusDeck/publish/
echo "✅ .NET binaries deployed"
echo ""

# Step 7: Start the service
echo "▶️  Starting FocusDeck service..."
sudo systemctl start focusdeck
sleep 3
echo "✅ Service started"
echo ""

# Step 8: Verify deployment
echo "🔍 Verifying deployment..."
echo ""

# Check service status
echo "  Service status:"
sudo systemctl status focusdeck --no-pager | grep -E "Active|Main PID" || true
echo ""

# Check health endpoint
echo "  Health check:"
sleep 2
HEALTH_RESPONSE=$(curl -s -w "\n%{http_code}" https://focusdeck.909436.xyz/healthz 2>&1 || echo "Error connecting")
echo "$HEALTH_RESPONSE" | tail -1
echo ""

# Check login page
echo "  Login page:"
LOGIN_RESPONSE=$(curl -s -w "\n%{http_code}" https://focusdeck.909436.xyz/login 2>&1 | tail -1 || echo "Error connecting")
echo "Response: $LOGIN_RESPONSE"
echo ""

# Check API
echo "  API status:"
API_RESPONSE=$(curl -s -w "\n%{http_code}" https://focusdeck.909436.xyz/v1/health 2>&1 | tail -1 || echo "Error connecting")
echo "Response: $API_RESPONSE"
echo ""

echo "✅ Deployment complete!"
echo ""
echo "📋 Deployment Summary:"
echo "  - React SPA built and deployed"
echo "  - .NET server built and deployed"
echo "  - AuthenticationMiddleware activated"
echo "  - New login page live"
echo "  - Service restarted successfully"
echo ""
echo "🎯 Next steps:"
echo "  1. Test login at https://focusdeck.909436.xyz/login"
echo "  2. Verify redirect: https://focusdeck.909436.xyz/ → should redirect to /login"
echo "  3. Monitor logs: journalctl -u focusdeck -f"
echo ""
echo "💾 Rollback available at: $BACKUP_DIR"
