#!/bin/bash

# FocusDeck Emergency Clean Deployment Script
# This script completely cleans and rebuilds from the phase-1 branch
# Use this when you have stale files causing issues

set -e  # Exit on error

echo "=========================================="
echo "FocusDeck Emergency Clean Deployment"
echo "=========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}Step 1: Pulling latest code from phase-1 branch...${NC}"
cd ~/FocusDeck
git fetch origin
git checkout phase-1
git pull origin phase-1

echo -e "${GREEN}✓ Code pulled${NC}"
echo ""

echo -e "${YELLOW}Step 2: COMPLETE CLEAN - Removing ALL build artifacts...${NC}"
echo "Removing old binaries..."
rm -rf src/FocusDeck.Server/bin
rm -rf src/FocusDeck.Server/obj
echo "Removing old wwwroot (the OLD UI is here!)..."
rm -rf src/FocusDeck.Server/wwwroot
echo "Removing WebApp build..."
rm -rf src/FocusDeck.WebApp/dist
echo "Removing build directory..."
rm -rf ~/focusdeck-server-build
echo "Removing node_modules cache..."
rm -rf src/FocusDeck.WebApp/node_modules/.cache

echo -e "${GREEN}✓ All old files removed${NC}"
echo ""

echo -e "${YELLOW}Step 3: Verifying wwwroot is GONE...${NC}"
if [ -d "src/FocusDeck.Server/wwwroot" ]; then
    echo -e "${RED}ERROR: wwwroot still exists! Manual cleanup needed.${NC}"
    exit 1
fi
echo -e "${GREEN}✓ wwwroot completely deleted${NC}"
echo ""

echo -e "${YELLOW}Step 4: Building FocusDeck.Server in Release mode...${NC}"
echo "(This will build WebApp first via BuildSpa target)"
cd ~/FocusDeck
dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained -o ~/focusdeck-server-build

echo -e "${GREEN}✓ Build complete${NC}"
echo ""

echo -e "${YELLOW}Step 5: Verifying new wwwroot has ONLY new React UI...${NC}"
echo "Checking wwwroot structure:"
if [ ! -d "src/FocusDeck.Server/wwwroot" ]; then
    echo -e "${RED}ERROR: wwwroot not created by build!${NC}"
    exit 1
fi

# List what's in wwwroot
echo "Files in wwwroot:"
ls -lah src/FocusDeck.Server/wwwroot/

echo ""
echo "Files in wwwroot/assets:"
ls -lah src/FocusDeck.Server/wwwroot/assets/ | head -20

echo ""
echo -e "${GREEN}✓ New UI files present${NC}"
echo ""

echo -e "${YELLOW}Step 6: Stopping FocusDeck service...${NC}"
sudo systemctl stop focusdeck || echo "Service already stopped"
sleep 2

echo -e "${GREEN}✓ Service stopped${NC}"
echo ""

echo -e "${YELLOW}Step 7: Deploying new build...${NC}"
sudo cp -r ~/focusdeck-server-build/* /opt/focusdeck/
sudo chown -R focusdeck:focusdeck /opt/focusdeck/

echo -e "${GREEN}✓ New build deployed${NC}"
echo ""

echo -e "${YELLOW}Step 8: Starting FocusDeck service...${NC}"
sudo systemctl start focusdeck
sleep 2

echo -e "${GREEN}✓ Service started${NC}"
echo ""

echo -e "${YELLOW}Step 9: Checking service status...${NC}"
sudo systemctl status focusdeck --no-pager

echo ""
echo -e "${YELLOW}Step 10: Health check...${NC}"
sleep 2
HEALTH=$(curl -s http://localhost:5000/healthz)
echo "Health check response:"
echo "$HEALTH" | jq '.' 2>/dev/null || echo "$HEALTH"

echo ""
echo -e "${YELLOW}Step 11: Testing root (/) endpoint...${NC}"
curl -I http://localhost:5000/ 2>/dev/null | head -5

echo ""
echo -e "${GREEN}=========================================="
echo "✓ Deployment complete!"
echo "==========================================${NC}"
echo ""
echo "Next steps:"
echo "1. Check the logs: sudo journalctl -u focusdeck -n 50 -f"
echo "2. Visit https://focusdeckv1.909436.xyz/"
echo "3. You should be redirected to /login"
echo "4. If not, check browser console (F12) for errors"
echo ""
