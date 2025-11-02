#!/bin/bash
##############################################################################
# FocusDeck Update System Configuration Script
# Run on a Linux host after deploying FocusDeck.
#
# Usage: sudo bash configure-update-system.sh
##############################################################################

set -euo pipefail

# ANSI colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

info()  { echo -e "${BLUE}$1${NC}"; }
warn()  { echo -e "${YELLOW}$1${NC}"; }
error() { echo -e "${RED}$1${NC}"; }
ok()    { echo -e "${GREEN}$1${NC}"; }

if [[ $EUID -ne 0 ]]; then
    error "This script must be run as root (use sudo)."
    exit 1
fi

FOCUSDECK_USER="focusdeck"
DEFAULT_REPO_PATH="/home/${FOCUSDECK_USER}/FocusDeck"
LOG_DIR="/var/log/focusdeck"
SERVICE_FILE="/etc/systemd/system/focusdeck.service"
SUDOERS_FILE="/etc/sudoers.d/focusdeck"

info "=== FocusDeck Update System Configuration ==="

# Step 1: Repository location
warn "Step 1: Repository location"
echo "Default repository path: ${DEFAULT_REPO_PATH}"
read -r -p "Enter custom path (or press Enter for default): " CUSTOM_REPO_PATH
REPO_PATH="${CUSTOM_REPO_PATH:-$DEFAULT_REPO_PATH}"

if [[ ! -d "$REPO_PATH" ]]; then
    warn "Repository not found at: ${REPO_PATH}"
    read -r -p "Clone repository now? (y/n): " CLONE_REPO
    if [[ "$CLONE_REPO" =~ ^[Yy]$ ]]; then
        ok "Cloning FocusDeck..."
        mkdir -p "$(dirname "$REPO_PATH")"
        git clone https://github.com/dertder25t-png/FocusDeck.git "$REPO_PATH"
        chown -R "${FOCUSDECK_USER}:${FOCUSDECK_USER}" "$REPO_PATH"
        ok "Repository cloned to ${REPO_PATH}"
    else
        error "Clone the repository manually and re-run this script."
        exit 1
    fi
else
    ok "Repository detected at ${REPO_PATH}"
    chown -R "${FOCUSDECK_USER}:${FOCUSDECK_USER}" "$REPO_PATH"
fi

# Step 2: Environment variable
warn "Step 2: Configure FOCUSDECK_REPO"
if [[ "$REPO_PATH" != "$DEFAULT_REPO_PATH" ]]; then
    if [[ -f "$SERVICE_FILE" ]]; then
        if grep -q "FOCUSDECK_REPO" "$SERVICE_FILE"; then
            sed -i "s|Environment=\"FOCUSDECK_REPO=.*\"|Environment=\"FOCUSDECK_REPO=$REPO_PATH\"|g" "$SERVICE_FILE"
        else
            sed -i "/Environment=/a Environment=\"FOCUSDECK_REPO=$REPO_PATH\"" "$SERVICE_FILE"
        fi
        ok "Updated ${SERVICE_FILE} with FOCUSDECK_REPO."
    else
        warn "Service file not found at ${SERVICE_FILE}. Add Environment=\"FOCUSDECK_REPO=$REPO_PATH\" manually."
    fi
else
    ok "Using default repository path; no environment override required."
fi

# Step 3: Sudo permissions
warn "Step 3: Configure sudo permissions"
cat > "$SUDOERS_FILE" << EOF
# FocusDeck update system permissions
${FOCUSDECK_USER} ALL=(ALL) NOPASSWD: /bin/systemctl restart focusdeck
${FOCUSDECK_USER} ALL=(ALL) NOPASSWD: /bin/systemctl status focusdeck
${FOCUSDECK_USER} ALL=(ALL) NOPASSWD: /bin/systemctl is-active focusdeck
${FOCUSDECK_USER} ALL=(ALL) NOPASSWD: /bin/mkdir
${FOCUSDECK_USER} ALL=(ALL) NOPASSWD: /bin/chown
EOF
chmod 0440 "$SUDOERS_FILE"
ok "Wrote sudo rules to ${SUDOERS_FILE}"

# Step 4: Log directory
warn "Step 4: Ensure update log directory exists"
mkdir -p "$LOG_DIR"
chown "${FOCUSDECK_USER}:${FOCUSDECK_USER}" "$LOG_DIR"
chmod 755 "$LOG_DIR"
ok "Log directory ready at ${LOG_DIR}"

# Step 5: Reload systemd
warn "Step 5: Reload systemd and restart service"
systemctl daemon-reload
if systemctl is-active --quiet focusdeck; then
    systemctl restart focusdeck
    ok "Restarted focusdeck service."
else
    warn "FocusDeck service is not running. Start it with: systemctl start focusdeck"
fi

info ""
info "Summary"
info "--------"
echo "Repository Path : ${REPO_PATH}"
echo "Log Directory   : ${LOG_DIR}"
echo "Sudoers File    : ${SUDOERS_FILE}"

info ""
info "Next Steps"
info "----------"
echo "1. Open the FocusDeck web interface."
echo "2. Navigate to Settings -> Server Management."
echo "3. Click 'Check for Updates' and 'Update Server' to verify the workflow."

ok ""
ok "FocusDeck update system is ready."
