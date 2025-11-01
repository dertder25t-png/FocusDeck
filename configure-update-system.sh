#!/bin/bash
##############################################################################
# FocusDeck Update System Configuration Script
# Run this on your Linux server after deploying FocusDeck
#
# Usage: sudo bash configure-update-system.sh
##############################################################################

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}╔════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║     FocusDeck Update System Configuration Script          ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   echo -e "${RED}This script must be run as root (use sudo)${NC}"
   exit 1
fi

# Configuration
FOCUSDECK_USER="focusdeck"
DEFAULT_REPO_PATH="/home/${FOCUSDECK_USER}/FocusDeck"
LOG_DIR="/var/log/focusdeck"

echo -e "${YELLOW}Step 1: Configure Repository Location${NC}"
echo -e "Default repository path: ${DEFAULT_REPO_PATH}"
read -p "Enter custom path (or press Enter for default): " CUSTOM_REPO_PATH
REPO_PATH="${CUSTOM_REPO_PATH:-$DEFAULT_REPO_PATH}"

# Check if repository exists
if [ ! -d "$REPO_PATH" ]; then
    echo -e "${YELLOW}Repository not found at: ${REPO_PATH}${NC}"
    read -p "Would you like to clone it now? (y/n): " CLONE_REPO
    
    if [[ "$CLONE_REPO" =~ ^[Yy]$ ]]; then
        echo -e "${GREEN}Cloning repository...${NC}"
        
        # Create parent directory if needed
        mkdir -p "$(dirname "$REPO_PATH")"
        
        # Clone repository
        git clone https://github.com/dertder25t-png/FocusDeck.git "$REPO_PATH"
        
        # Set ownership
        chown -R ${FOCUSDECK_USER}:${FOCUSDECK_USER} "$REPO_PATH"
        
        echo -e "${GREEN}✓ Repository cloned successfully${NC}"
    else
        echo -e "${RED}Please clone the repository manually and run this script again${NC}"
        exit 1
    fi
else
    echo -e "${GREEN}✓ Repository found at: ${REPO_PATH}${NC}"
    
    # Fix permissions
    chown -R ${FOCUSDECK_USER}:${FOCUSDECK_USER} "$REPO_PATH"
fi

echo ""
echo -e "${YELLOW}Step 2: Configure Environment Variable${NC}"
if [ "$REPO_PATH" != "$DEFAULT_REPO_PATH" ]; then
    # Add environment variable to systemd service
    SERVICE_FILE="/etc/systemd/system/focusdeck.service"
    
    if [ -f "$SERVICE_FILE" ]; then
        if grep -q "FOCUSDECK_REPO" "$SERVICE_FILE"; then
            echo -e "${YELLOW}Updating existing FOCUSDECK_REPO variable...${NC}"
            sed -i "s|Environment=\"FOCUSDECK_REPO=.*\"|Environment=\"FOCUSDECK_REPO=$REPO_PATH\"|g" "$SERVICE_FILE"
        else
            echo -e "${YELLOW}Adding FOCUSDECK_REPO variable...${NC}"
            # Add after other Environment lines
            sed -i "/Environment=/a Environment=\"FOCUSDECK_REPO=$REPO_PATH\"" "$SERVICE_FILE"
        fi
        echo -e "${GREEN}✓ Environment variable configured${NC}"
    else
        echo -e "${YELLOW}Warning: Service file not found at $SERVICE_FILE${NC}"
        echo -e "${YELLOW}You may need to manually add: Environment=\"FOCUSDECK_REPO=$REPO_PATH\"${NC}"
    fi
else
    echo -e "${GREEN}✓ Using default path, no environment variable needed${NC}"
fi

echo ""
echo -e "${YELLOW}Step 3: Configure Sudo Permissions${NC}"
SUDOERS_FILE="/etc/sudoers.d/focusdeck"

cat > "$SUDOERS_FILE" << EOF
# FocusDeck update system permissions
${FOCUSDECK_USER} ALL=(ALL) NOPASSWD: /bin/systemctl restart focusdeck
${FOCUSDECK_USER} ALL=(ALL) NOPASSWD: /bin/systemctl status focusdeck
${FOCUSDECK_USER} ALL=(ALL) NOPASSWD: /bin/systemctl is-active focusdeck
${FOCUSDECK_USER} ALL=(ALL) NOPASSWD: /bin/mkdir
${FOCUSDECK_USER} ALL=(ALL) NOPASSWD: /bin/chown
EOF

chmod 0440 "$SUDOERS_FILE"
echo -e "${GREEN}✓ Sudo permissions configured${NC}"

echo ""
echo -e "${YELLOW}Step 4: Create Log Directory${NC}"
mkdir -p "$LOG_DIR"
chown ${FOCUSDECK_USER}:${FOCUSDECK_USER} "$LOG_DIR"
chmod 755 "$LOG_DIR"
echo -e "${GREEN}✓ Log directory created: ${LOG_DIR}${NC}"

echo ""
echo -e "${YELLOW}Step 5: Reload Systemd and Restart Service${NC}"
systemctl daemon-reload
if systemctl is-active --quiet focusdeck; then
    systemctl restart focusdeck
    echo -e "${GREEN}✓ FocusDeck service restarted${NC}"
else
    echo -e "${YELLOW}FocusDeck service is not running. Start it with: systemctl start focusdeck${NC}"
fi

echo ""
echo -e "${GREEN}╔════════════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║              Configuration Complete!                       ║${NC}"
echo -e "${GREEN}╚════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${BLUE}Summary:${NC}"
echo -e "  Repository Path:    ${REPO_PATH}"
echo -e "  Log Directory:      ${LOG_DIR}"
echo -e "  Sudoers File:       ${SUDOERS_FILE}"
echo ""
echo -e "${BLUE}Next Steps:${NC}"
echo -e "  1. Open your FocusDeck web interface"
echo -e "  2. Navigate to Settings → Server Management"
echo -e "  3. Click 'Update Server Now' to test the update system"
echo ""
echo -e "${BLUE}Verify Installation:${NC}"
echo -e "  Check service status:   systemctl status focusdeck"
echo -e "  View logs:              journalctl -u focusdeck -f"
echo -e "  Test sudo:              sudo -l -U ${FOCUSDECK_USER}"
echo ""
echo -e "${GREEN}✓ Update system is ready to use!${NC}"
