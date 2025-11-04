#!/bin/bash
###############################################################################
# FocusDeck Server - One-Line Installer
# Usage: curl -sSL https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/install-focusdeck.sh | bash
# This script downloads and runs the complete FocusDeck setup
###############################################################################

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}"
echo "╔════════════════════════════════════════╗"
echo "║  FocusDeck Server - One-Line Installer ║"
echo "╚════════════════════════════════════════╝"
echo -e "${NC}"

# Check if running with proper permissions for some operations
if [ "$EUID" -ne 0 ]; then 
    echo -e "${YELLOW}Note: Some commands may require sudo password${NC}"
    SUDO="sudo"
else 
    SUDO=""
fi

FOCUSDECK_HOME="${HOME}/FocusDeck"

echo -e "${YELLOW}Starting FocusDeck installation...${NC}"
echo ""

# Step 1: Update system
echo -e "${BLUE}[1/5] Updating system packages...${NC}"
$SUDO apt-get update -qq > /dev/null 2>&1
$SUDO apt-get upgrade -y -qq > /dev/null 2>&1
echo -e "${GREEN}✓ System updated${NC}"

# Step 2: Install Git
echo -e "${BLUE}[2/5] Installing Git...${NC}"
if ! command -v git &> /dev/null; then
    $SUDO apt-get install -y git > /dev/null 2>&1
    echo -e "${GREEN}✓ Git installed${NC}"
else
    echo -e "${GREEN}✓ Git already installed${NC}"
fi

# Step 3: Install .NET SDK
echo -e "${BLUE}[3/5] Installing .NET 8 SDK...${NC}"
if ! command -v dotnet &> /dev/null; then
    cd /tmp
    wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh 2>/dev/null
    chmod +x dotnet-install.sh
    ./dotnet-install.sh --channel 8.0 > /dev/null 2>&1
    rm dotnet-install.sh
    cd -
    
    export PATH=$PATH:$HOME/.dotnet
    echo 'export PATH=$PATH:$HOME/.dotnet' >> ~/.bashrc
    source ~/.bashrc
    echo -e "${GREEN}✓ .NET SDK installed${NC}"
else
    echo -e "${GREEN}✓ .NET SDK already installed$(dotnet --version)${NC}"
fi

# Step 4: Clone repository or update
echo -e "${BLUE}[4/5] Setting up FocusDeck repository...${NC}"
if [ ! -d "$FOCUSDECK_HOME" ]; then
    cd ~
    git clone https://github.com/dertder25t-png/FocusDeck.git > /dev/null 2>&1
    echo -e "${GREEN}✓ Repository cloned${NC}"
else
    cd "$FOCUSDECK_HOME"
    git pull origin master > /dev/null 2>&1
    echo -e "${GREEN}✓ Repository updated${NC}"
fi

cd "$FOCUSDECK_HOME"

# Step 5: Make startup script executable and run setup
echo -e "${BLUE}[5/5] Running FocusDeck setup...${NC}"
chmod +x start-focusdeck.sh

# Run the setup through the new unified script
./start-focusdeck.sh setup

echo ""
echo -e "${GREEN}╔════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║  Installation Complete! ✓              ║${NC}"
echo -e "${GREEN}╚════════════════════════════════════════╝${NC}"
echo ""
echo -e "${YELLOW}Next steps:${NC}"
echo ""
echo "  View server status:"
echo "    ${BLUE}~/FocusDeck/start-focusdeck.sh status${NC}"
echo ""
echo "  View server logs:"
echo "    ${BLUE}~/FocusDeck/start-focusdeck.sh logs${NC}"
echo ""
echo "  Access the web UI:"
echo "    ${BLUE}http://localhost:5239${NC}"
echo ""
echo "  For more commands:"
echo "    ${BLUE}~/FocusDeck/start-focusdeck.sh help${NC}"
echo ""
