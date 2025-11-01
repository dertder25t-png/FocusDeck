#!/bin/bash
##############################################################################
# FocusDeck Sudo Permissions Setup for Update System
# This script configures the necessary sudo permissions for the FocusDeck
# user to run update and service management commands
##############################################################################

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}╔════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║     FocusDeck Sudo Permissions Setup                      ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   echo -e "${RED}This script must be run as root (use sudo)${NC}"
   exit 1
fi

# Get the FocusDeck user
DEFAULT_USER="focusdeck"
read -p "Enter the FocusDeck service user (default: focusdeck): " FOCUSDECK_USER
FOCUSDECK_USER="${FOCUSDECK_USER:-$DEFAULT_USER}"

# Check if user exists
if ! id "$FOCUSDECK_USER" &>/dev/null; then
    echo -e "${RED}User $FOCUSDECK_USER does not exist!${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Configuring sudo permissions for user: $FOCUSDECK_USER${NC}"
echo ""

# Get repository path
DEFAULT_REPO="/home/${FOCUSDECK_USER}/FocusDeck"
read -p "Enter FocusDeck repository path (default: $DEFAULT_REPO): " REPO_PATH
REPO_PATH="${REPO_PATH:-$DEFAULT_REPO}"

# Create sudoers file
SUDOERS_FILE="/etc/sudoers.d/focusdeck"

echo -e "${YELLOW}Creating sudoers file: $SUDOERS_FILE${NC}"

cat > "$SUDOERS_FILE" << EOF
# FocusDeck update system permissions
# Created: $(date)

# Service management commands
${FOCUSDECK_USER} ALL=(root) NOPASSWD: /usr/bin/systemctl restart focusdeck
${FOCUSDECK_USER} ALL=(root) NOPASSWD: /usr/bin/systemctl status focusdeck
${FOCUSDECK_USER} ALL=(root) NOPASSWD: /usr/bin/systemctl is-active focusdeck
${FOCUSDECK_USER} ALL=(root) NOPASSWD: /usr/bin/systemctl stop focusdeck
${FOCUSDECK_USER} ALL=(root) NOPASSWD: /usr/bin/systemctl start focusdeck

# Update system commands
${FOCUSDECK_USER} ALL=(root) NOPASSWD: ${REPO_PATH}/configure-update-system.sh
${FOCUSDECK_USER} ALL=(root) NOPASSWD: /usr/bin/git

# File management for logs
${FOCUSDECK_USER} ALL=(root) NOPASSWD: /usr/bin/mkdir
${FOCUSDECK_USER} ALL=(root) NOPASSWD: /usr/bin/chown
EOF

# Set proper permissions on sudoers file
chmod 0440 "$SUDOERS_FILE"

echo -e "${GREEN}✓ Sudoers file created and permissions set${NC}"
echo ""

# Validate sudoers syntax
echo -e "${YELLOW}Validating sudoers syntax...${NC}"
if visudo -cf "$SUDOERS_FILE"; then
    echo -e "${GREEN}✓ Sudoers syntax is valid${NC}"
else
    echo -e "${RED}✗ Sudoers syntax error! Removing file...${NC}"
    rm "$SUDOERS_FILE"
    exit 1
fi

echo ""
echo -e "${BLUE}Testing sudo permissions...${NC}"
sudo -l -U "$FOCUSDECK_USER" | grep -E "(systemctl|git|mkdir|chown)" || true

echo ""
echo -e "${GREEN}╔════════════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║              Setup Complete!                               ║${NC}"
echo -e "${GREEN}╚════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${BLUE}Permissions Summary:${NC}"
echo -e "  User:          ${FOCUSDECK_USER}"
echo -e "  Sudoers file:  ${SUDOERS_FILE}"
echo -e "  Repository:    ${REPO_PATH}"
echo ""
echo -e "${BLUE}Allowed Commands:${NC}"
echo -e "  • systemctl (restart/status/start/stop/is-active) focusdeck"
echo -e "  • git (for pulling updates)"
echo -e "  • mkdir/chown (for log directory management)"
echo -e "  • configure-update-system.sh"
echo ""
echo -e "${BLUE}Test the permissions:${NC}"
echo -e "  sudo -l -U ${FOCUSDECK_USER}"
echo ""
echo -e "${GREEN}✓ Update system is ready to use!${NC}"
