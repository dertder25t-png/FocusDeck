#!/bin/bash

##############################################################################
# FocusDeck Server - One-Command Setup Script
# 
# This script automatically installs and configures FocusDeck Server on
# Ubuntu/Debian Linux systems.
#
# Usage: 
#   curl -sSL https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/install.sh | sudo bash
#
# Or download and run:
#   wget https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/install.sh
#   sudo bash install.sh
##############################################################################

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m'

# Configuration
INSTALL_DIR="$HOME/focusdeck-server"
REPO_URL="https://github.com/dertder25t-png/FocusDeck.git"
SERVICE_USER="$USER"

echo -e "${PURPLE}"
echo "╔═══════════════════════════════════════════════════════╗"
echo "║                                                       ║"
echo "║       🎯 FocusDeck Server - Quick Install 🎯         ║"
echo "║                                                       ║"
echo "╚═══════════════════════════════════════════════════════╝"
echo -e "${NC}"
echo ""

# Check if running as root (for systemd service creation)
if [[ $EUID -eq 0 ]]; then
    SERVICE_USER="root"
    echo -e "${YELLOW}⚠️  Running as root. Service will run as root user.${NC}"
else
    echo -e "${CYAN}ℹ️  Running as $USER. You'll need sudo for some steps.${NC}"
fi

echo ""
echo -e "${BLUE}[1/6]${NC} Installing dependencies..."
sleep 1

# Update system
sudo apt update -qq

# Install required packages
sudo apt install -y git curl wget > /dev/null 2>&1

echo -e "${GREEN}✓${NC} Dependencies installed"
echo ""
echo -e "${BLUE}[2/6]${NC} Installing .NET 9.0..."
sleep 1

# Check if .NET is already installed
if command -v dotnet &> /dev/null; then
    echo -e "${GREEN}✓${NC} .NET already installed ($(dotnet --version))"
else
    # Install .NET 9.0
    wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh -q
    chmod +x /tmp/dotnet-install.sh
    /tmp/dotnet-install.sh --channel 9.0 > /dev/null 2>&1
    
    # Add to PATH
    export DOTNET_ROOT=$HOME/.dotnet
    export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools
    
    # Make permanent
    grep -qxF 'export DOTNET_ROOT=$HOME/.dotnet' ~/.bashrc || echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.bashrc
    grep -qxF 'export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools' ~/.bashrc || echo 'export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools' >> ~/.bashrc
    
    echo -e "${GREEN}✓${NC} .NET 9.0 installed"
fi

echo ""
echo -e "${BLUE}[3/6]${NC} Cloning FocusDeck repository..."
sleep 1

# Clone or update repository
if [ -d "$HOME/FocusDeck" ]; then
    echo -e "${YELLOW}Repository already exists, updating...${NC}"
    cd "$HOME/FocusDeck"
    git pull -q
else
    git clone "$REPO_URL" "$HOME/FocusDeck" -q
fi

echo -e "${GREEN}✓${NC} Repository ready"
echo ""
echo -e "${BLUE}[4/6]${NC} Building server application..."
sleep 1

# Navigate and build
cd "$HOME/FocusDeck/src/FocusDeck.Server"

# Remove any nested folders
[ -d "FocusDeck" ] && rm -rf FocusDeck

# Publish
dotnet publish FocusDeck.Server.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained \
    -o "$INSTALL_DIR" \
    > /dev/null 2>&1

echo -e "${GREEN}✓${NC} Server built successfully"
echo ""
echo -e "${BLUE}[5/6]${NC} Creating systemd service..."
sleep 1

# Create service file
sudo tee /etc/systemd/system/focusdeck.service > /dev/null <<EOF
[Unit]
Description=FocusDeck Server
After=network.target

[Service]
Type=simple
User=$SERVICE_USER
WorkingDirectory=$INSTALL_DIR
ExecStart=$INSTALL_DIR/FocusDeck.Server
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=focusdeck
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000

[Install]
WantedBy=multi-user.target
EOF

# Reload systemd and enable service
sudo systemctl daemon-reload
sudo systemctl enable focusdeck > /dev/null 2>&1

echo -e "${GREEN}✓${NC} Service created and enabled"
echo ""
echo -e "${BLUE}[6/6]${NC} Starting server..."
sleep 1

# Start the service
sudo systemctl start focusdeck

# Wait a moment for startup
sleep 2

# Check if it's running
if sudo systemctl is-active --quiet focusdeck; then
    echo -e "${GREEN}✓${NC} Server started successfully!"
else
    echo -e "${RED}✗${NC} Server failed to start. Checking logs..."
    sudo journalctl -u focusdeck -n 20 --no-pager
    exit 1
fi

echo ""
echo -e "${GREEN}╔═══════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║                                                       ║${NC}"
echo -e "${GREEN}║          🎉 Installation Complete! 🎉                ║${NC}"
echo -e "${GREEN}║                                                       ║${NC}"
echo -e "${GREEN}╚═══════════════════════════════════════════════════════╝${NC}"
echo ""

# Get server IP
SERVER_IP=$(hostname -I | awk '{print $1}')

echo -e "${CYAN}📊 Server Information:${NC}"
echo -e "   Status: ${GREEN}Running${NC}"
echo -e "   Location: $INSTALL_DIR"
echo -e "   Service: focusdeck.service"
echo ""
echo -e "${CYAN}🌐 Access Your Server:${NC}"
echo -e "   Web UI:  ${BLUE}http://$SERVER_IP:5000${NC}"
echo -e "   API:     ${BLUE}http://$SERVER_IP:5000/api/decks${NC}"
echo ""
echo -e "${CYAN}📝 Useful Commands:${NC}"
echo -e "   View logs:    ${YELLOW}sudo journalctl -u focusdeck -f${NC}"
echo -e "   Restart:      ${YELLOW}sudo systemctl restart focusdeck${NC}"
echo -e "   Stop:         ${YELLOW}sudo systemctl stop focusdeck${NC}"
echo -e "   Status:       ${YELLOW}sudo systemctl status focusdeck${NC}"
echo ""
echo -e "${CYAN}🔐 Security Note:${NC}"
echo -e "   ${YELLOW}⚠️  No authentication is currently enabled.${NC}"
echo -e "   ${YELLOW}⚠️  Consider setting up a firewall and HTTPS.${NC}"
echo ""
echo -e "${GREEN}✨ Open your browser to http://$SERVER_IP:5000 to get started!${NC}"
echo ""
