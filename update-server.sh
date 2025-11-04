#!/bin/bash

##############################################################################
# FocusDeck Server - Quick Update Script
# 
# Updates an existing FocusDeck installation to the latest version
# Preserves your configuration and database
#
# Usage: 
#   sudo bash update-server.sh
##############################################################################

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
APP_DIR="/opt/focusdeck"
SERVICE_USER="focusdeck"

# --- Helper Functions ---
fn_print() {
    echo -e "${GREEN}[FocusDeck]${NC} $1"
}

fn_warn() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

fn_error() {
    echo -e "${RED}[ERROR]${NC} $1"
    exit 1
}

# --- Checks ---
fn_check_root() {
    if [ "$EUID" -ne 0 ]; then
        fn_error "This script must be run as root. Please use 'sudo'."
    fi
}

fn_check_installation() {
    if [ ! -d "$APP_DIR" ]; then
        fn_error "FocusDeck not found at $APP_DIR. Please run complete-setup.sh first."
    fi
    
    if ! systemctl list-unit-files | grep -q "focusdeck.service"; then
        fn_error "FocusDeck service not found. Please run complete-setup.sh first."
    fi
}

# --- Update Process ---
fn_backup_config() {
    fn_print "Backing up configuration..."
    
    if [ -f "$APP_DIR/publish/appsettings.json" ]; then
        cp "$APP_DIR/publish/appsettings.json" "/tmp/focusdeck-config-backup-$(date +%s).json"
        fn_print "✓ Config backed up to /tmp/"
    fi
    
    if [ -f "$APP_DIR/publish/focusdeck.db" ]; then
        cp "$APP_DIR/publish/focusdeck.db" "/tmp/focusdeck-db-backup-$(date +%s).db"
        fn_print "✓ Database backed up to /tmp/"
    fi
}

fn_stop_service() {
    fn_print "Stopping service..."
    systemctl stop focusdeck.service
    fn_print "✓ Service stopped"
}

fn_update_code() {
    fn_print "Pulling latest code from GitHub..."
    
    cd "$APP_DIR"
    sudo -u $SERVICE_USER git fetch --all
    sudo -u $SERVICE_USER git reset --hard origin/master
    sudo -u $SERVICE_USER git pull origin master
    
    fn_print "✓ Code updated"
}

fn_rebuild() {
    fn_print "Rebuilding server..."
    
    cd "$APP_DIR/src/FocusDeck.Server"
    
    # Save current config
    CONFIG_BACKUP=""
    if [ -f "$APP_DIR/publish/appsettings.json" ]; then
        CONFIG_BACKUP=$(cat "$APP_DIR/publish/appsettings.json")
    fi
    
    # Build
    sudo -u $SERVICE_USER dotnet build -c Release -v quiet
    if [ $? -ne 0 ]; then
        fn_error "Build failed. Service not restarted."
    fi
    
    # Publish
    sudo -u $SERVICE_USER dotnet publish -c Release -o "$APP_DIR/publish" -v quiet
    if [ $? -ne 0 ]; then
        fn_error "Publish failed. Service not restarted."
    fi
    
    # Restore config if it was overwritten
    if [ -n "$CONFIG_BACKUP" ]; then
        echo "$CONFIG_BACKUP" > "$APP_DIR/publish/appsettings.json"
        chown $SERVICE_USER:$SERVICE_USER "$APP_DIR/publish/appsettings.json"
    fi
    
    fn_print "✓ Server rebuilt"
}

fn_run_migrations() {
    fn_print "Running database migrations..."
    
    # The server will auto-apply migrations on startup with EnsureCreated
    # For the SyncVersion table, it's handled by the bootstrap code
    
    fn_print "✓ Migrations will apply on startup"
}

fn_start_service() {
    fn_print "Starting service..."
    
    systemctl start focusdeck.service
    sleep 3
    
    if systemctl is-active --quiet focusdeck.service; then
        fn_print "✓ Service started successfully"
    else
        fn_error "Service failed to start. Check logs: journalctl -u focusdeck -n 50"
    fi
}

fn_verify() {
    fn_print "Verifying update..."
    
    sleep 2
    
    if systemctl is-active --quiet focusdeck.service; then
        echo ""
        echo -e "${GREEN}╔═══════════════════════════════════════╗${NC}"
        echo -e "${GREEN}║  ✅ Update completed successfully!   ║${NC}"
        echo -e "${GREEN}╚═══════════════════════════════════════╝${NC}"
        echo ""
        
        local_ip=$(hostname -I | awk '{print $1}')
        echo "Server is running at:"
        echo "  http://localhost:5000"
        echo "  http://${local_ip}:5000"
        echo ""
        echo "Check logs: sudo journalctl -u focusdeck -f"
    else
        fn_error "Service is not running. Check logs: journalctl -u focusdeck -n 50"
    fi
}

# --- Main ---
main() {
    echo -e "${BLUE}"
    echo "╔═══════════════════════════════════════╗"
    echo "║   FocusDeck Server - Quick Update    ║"
    echo "╚═══════════════════════════════════════╝"
    echo -e "${NC}"
    echo ""
    
    fn_check_root
    fn_check_installation
    
    fn_print "Starting update process..."
    echo ""
    
    fn_backup_config
    fn_stop_service
    fn_update_code
    fn_rebuild
    fn_run_migrations
    fn_start_service
    fn_verify
}

main "$@"
