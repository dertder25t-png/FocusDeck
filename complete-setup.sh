#!/bin/bash

##############################################################################
# FocusDeck Server - Complete Setup Script (Updated Nov 2024)
# 
# Features:
# - .NET 9.0 installation
# - Secure JWT configuration with validation
# - CORS setup
# - Database migrations (SyncVersion support)
# - Cloudflare Tunnel integration (optional)
# - Systemd service with proper permissions
# - Web-based update system configuration
#
# Usage: 
#   sudo bash complete-setup.sh
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
APP_DIR="/opt/focusdeck"
REPO_URL="https://github.com/dertder25t-png/FocusDeck.git"
SERVICE_USER="focusdeck"
SERVICE_FILE="/etc/systemd/system/focusdeck.service"
CLOUDFLARED_INSTALLED=false
CF_METHOD=""
CF_TOKEN=""

# --- Helper Functions ---
fn_prompt_cloudflare() {
    echo ""
    echo -e "${BLUE}╔═══════════════════════════════════════════════════╗${NC}"
    echo -e "${BLUE}║     Cloudflare Tunnel Setup (Recommended)        ║${NC}"
    echo -e "${BLUE}╚═══════════════════════════════════════════════════╝${NC}"
    echo ""
    echo "Installing Cloudflare Tunnel on this server provides:"
    echo "  ✓ Secure HTTPS without port forwarding"
    echo "  ✓ Real client IP addresses (required for auth)"
    echo "  ✓ DDoS protection"
    echo "  ✓ Free bandwidth"
    echo ""
    echo -e "${YELLOW}If you skip this, you'll need to configure a tunnel manually${NC}"
    echo -e "${YELLOW}from another machine pointing to this server's IP.${NC}"
    echo ""
    read -r -p "Install cloudflared on this server? [Y/n]: " response
    case "$response" in
        [nN][oO]|[nN])
            CLOUDFLARED_INSTALLED=false
            fn_print "Skipping cloudflared installation."
            ;;
        *)
            CLOUDFLARED_INSTALLED=true
            # Choose install method
            echo ""
            echo "Select Cloudflare setup method:"
            echo "  1) Paste Cloudflare token (remotely managed tunnel) — recommended"
            echo "  2) Manual login + create tunnel + YAML config"
            echo "  3) Skip Cloudflare setup for now"
            read -r -p "Enter choice [1/2/3]: " cf_choice
            case "$cf_choice" in
                3)
                    CLOUDFLARED_INSTALLED=false
                    fn_warn "Skipping Cloudflare setup. You can configure later."
                    ;;
                2)
                    CF_METHOD="manual"
                    fn_install_cloudflared_repo
                    ;;
                *)
                    CF_METHOD="token"
                    fn_install_cloudflared_repo
                    echo ""
                    echo -e "${CYAN}Paste your Cloudflare token (from the dashboard 'service install' button):${NC}"
                    read -r -p "Token: " CF_TOKEN
                    if [ -z "$CF_TOKEN" ]; then
                        fn_warn "No token entered. Falling back to manual method."
                        CF_METHOD="manual"
                    else
                        fn_install_cloudflared_token "$CF_TOKEN"
                    fi
                    ;;
            esac
            ;;
    esac
}

# Install via Cloudflare's apt repository (recommended by Cloudflare)
fn_install_cloudflared_repo() {
    fn_print "Installing cloudflared (apt repo)..."
    mkdir -p /usr/share/keyrings
    curl -fsSL https://pkg.cloudflare.com/cloudflare-public-v2.gpg | tee /usr/share/keyrings/cloudflare-public-v2.gpg >/dev/null
    echo 'deb [signed-by=/usr/share/keyrings/cloudflare-public-v2.gpg] https://pkg.cloudflare.com/cloudflared any main' | tee /etc/apt/sources.list.d/cloudflared.list >/dev/null
    apt-get update -qq
    apt-get install -y cloudflared > /dev/null 2>&1 || fn_error "Failed to install cloudflared"

    # Verify version meets remotely managed tunnels requirement (>= 2022.03.04)
    if command -v cloudflared &> /dev/null; then
        ver=$(cloudflared --version 2>/dev/null | awk '{print $3}')
        year=$(echo "$ver" | cut -d. -f1)
        mon=$(echo "$ver" | cut -d. -f2)
        day=$(echo "$ver" | cut -d. -f3)
        pass=false
        if [[ -n "$year" && -n "$mon" ]]; then
            if (( year > 2022 )); then pass=true; fi
            if (( year == 2022 )) && (( mon > 3 )); then pass=true; fi
            if (( year == 2022 )) && (( mon == 3 )) && [[ -n "$day" ]] && (( day >= 4 )); then pass=true; fi
        fi
        if [[ "$pass" == true ]]; then
            fn_print "✓ cloudflared version $ver detected (OK)"
        else
            fn_warn "cloudflared version $ver detected. Remotely managed tunnels require >= 2022.03.04. Consider updating."
        fi
    fi
}

# Configure cloudflared using a token (remotely managed tunnel)
fn_install_cloudflared_token() {
    local token="$1"
    fn_print "Installing cloudflared service with token..."
    cloudflared service install "$token" || fn_error "cloudflared service install failed"
    systemctl enable cloudflared || true
    systemctl restart cloudflared || true
    sleep 2
    if systemctl is-active --quiet cloudflared; then
        fn_print "✓ cloudflared service is running"
    else
        fn_warn "cloudflared service not active. You can run it manually with: cloudflared tunnel run --token <token>"
    fi
}
    echo ""
    echo "Installing Cloudflare Tunnel on this server provides:"
    echo "  ✓ Secure HTTPS without port forwarding"
    echo "  ✓ Real client IP addresses (required for auth)"
    echo "  ✓ DDoS protection"
    echo "  ✓ Free bandwidth"
    echo ""
    echo -e "${YELLOW}If you skip this, you'll need to configure a tunnel manually${NC}"
    echo -e "${YELLOW}from another machine pointing to this server's IP.${NC}"
    echo ""
    read -r -p "Install cloudflared on this server? [Y/n]: " response
    case "$response" in
        [nN][oO]|[nN])
            CLOUDFLARED_INSTALLED=false
            fn_print "Skipping cloudflared installation."
            ;;
        *)
            CLOUDFLARED_INSTALLED=true
            fn_install_cloudflared
            ;;
    esac
}

fn_install_cloudflared() {
    fn_print "Installing cloudflared..."
    
    # Install wget if needed
    if ! command -v wget &> /dev/null; then
        apt-get install -y wget > /dev/null 2>&1
    fi
    
    # Download and install
    wget -q https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64.deb -O /tmp/cloudflared.deb
    dpkg -i /tmp/cloudflared.deb || apt-get install -f -y
    rm /tmp/cloudflared.deb
    
    # Verify version meets remotely managed tunnels requirement (>= 2022.03.04)
    if command -v cloudflared &> /dev/null; then
        ver=$(cloudflared --version 2>/dev/null | awk '{print $3}')
        year=$(echo "$ver" | cut -d. -f1)
        mon=$(echo "$ver" | cut -d. -f2)
        day=$(echo "$ver" | cut -d. -f3)
        pass=false
        if [[ -n "$year" && -n "$mon" ]]; then
            if (( year > 2022 )); then pass=true; fi
            if (( year == 2022 )) && (( mon > 3 )); then pass=true; fi
            if (( year == 2022 )) && (( mon == 3 )) && [[ -n "$day" ]] && (( day >= 4 )); then pass=true; fi
        fi
        if [[ "$pass" == true ]]; then
            fn_print "✓ cloudflared version $ver detected (OK)"
        else
            fn_warn "cloudflared version $ver detected. Remotely managed tunnels require >= 2022.03.04. Consider updating."
        fi
    fi
    
    fn_print "✓ cloudflared installed successfully"
}

# --- System Dependencies ---
fn_install_dependencies() {
    fn_print "Installing system dependencies..."
    
    apt-get update -qq
    apt-get install -y curl wget git unzip apt-transport-https > /dev/null 2>&1
    
    fn_print "✓ System dependencies installed"
}

fn_install_dotnet() {
    fn_print "Installing .NET 9.0 SDK..."
    
    # Check if already installed
    if command -v dotnet &> /dev/null; then
        DOTNET_VERSION=$(dotnet --version 2>/dev/null | cut -d'.' -f1)
        if [ "$DOTNET_VERSION" = "9" ]; then
            fn_print "✓ .NET 9.0 already installed"
            return
        fi
    fi
    
    # Add Microsoft repository
    wget -q https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
    dpkg -i /tmp/packages-microsoft-prod.deb
    rm /tmp/packages-microsoft-prod.deb
    
    apt-get update -qq
    apt-get install -y dotnet-sdk-9.0 > /dev/null 2>&1
    
    fn_print "✓ .NET 9.0 SDK installed"
}

# --- User and Directory Setup ---
fn_create_user() {
    fn_print "Creating service user '$SERVICE_USER'..."
    
    if ! id "$SERVICE_USER" &>/dev/null; then
        useradd --system --shell /bin/false --home "$APP_DIR" --create-home "$SERVICE_USER"
        fn_print "✓ User '$SERVICE_USER' created"
    else
        fn_print "✓ User '$SERVICE_USER' already exists"
    fi
}

# --- Repository and Build ---
fn_clone_repo() {
    fn_print "Cloning/updating repository..."
    
    if [ -d "$APP_DIR/.git" ]; then
        cd "$APP_DIR"
        sudo -u $SERVICE_USER git fetch --all
        sudo -u $SERVICE_USER git reset --hard origin/master
        sudo -u $SERVICE_USER git pull origin master
        fn_print "✓ Repository updated"
    else
        rm -rf "$APP_DIR"
        git clone "$REPO_URL" "$APP_DIR"
        chown -R $SERVICE_USER:$SERVICE_USER "$APP_DIR"
        fn_print "✓ Repository cloned"
    fi
}

fn_build_server() {
    fn_print "Building FocusDeck Server..."
    
    cd "$APP_DIR/src/FocusDeck.Server"
    
    # Build
    sudo -u $SERVICE_USER dotnet build -c Release -v quiet
    if [ $? -ne 0 ]; then
        fn_error "Build failed. Check logs above."
    fi
    
    # Publish
    sudo -u $SERVICE_USER dotnet publish -c Release -o "$APP_DIR/publish" -v quiet
    if [ $? -ne 0 ]; then
        fn_error "Publish failed. Check logs above."
    fi
    
    fn_print "✓ Server built and published"
}

# --- Configuration ---
fn_create_config() {
    fn_print "Configuring server settings..."
    
    local config_path="$APP_DIR/publish/appsettings.json"
    
    # Backup existing config
    if [ -f "$config_path" ]; then
        cp "$config_path" "${config_path}.backup.$(date +%s)"
        fn_print "Existing config backed up"
    fi
    
    # Generate secure JWT key (64 characters, base64)
    JWT_SECRET=$(head -c 48 /dev/urandom | base64 | tr -d '\n' | head -c 64)
    
    # Get domain or use localhost
    echo ""
    read -r -p "Enter your domain (e.g., focusdeck.example.com) or press Enter for localhost: " DOMAIN
    
    if [ -z "$DOMAIN" ]; then
        DOMAIN="localhost"
        JWT_ISSUER="http://localhost:5000"
        CORS_ORIGINS='      "http://localhost:5173",\n      "http://localhost:5000"'
    else
        JWT_ISSUER="https://${DOMAIN}"
        CORS_ORIGINS='      "https://'"${DOMAIN}"'"'
    fi
    
    # Create configuration file
    cat > "$config_path" << EOF
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=focusdeck.db"
  },
  "Jwt": {
    "Key": "${JWT_SECRET}",
    "Issuer": "${JWT_ISSUER}",
    "Audience": "focusdeck-clients"
  },
  "Cors": {
    "AllowedOrigins": [
${CORS_ORIGINS}
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
    
    chown $SERVICE_USER:$SERVICE_USER "$config_path"
    chmod 600 "$config_path"
    
    fn_print "✓ Configuration created with secure JWT key"
}

fn_setup_database() {
    fn_print "Setting up database..."
    
    # Create database directory and set permissions
    mkdir -p "$APP_DIR/publish"
    touch "$APP_DIR/publish/focusdeck.db" 2>/dev/null || true
    chown -R $SERVICE_USER:$SERVICE_USER "$APP_DIR/publish"
    
    fn_print "✓ Database initialized (will be created on first run)"
}

# --- Systemd Service ---
fn_create_service() {
    fn_print "Creating systemd service..."
    
    cat > "$SERVICE_FILE" << EOF
[Unit]
Description=FocusDeck Server
After=network.target

[Service]
Type=simple
User=$SERVICE_USER
Group=$SERVICE_USER
WorkingDirectory=$APP_DIR/publish
ExecStart=/usr/bin/dotnet $APP_DIR/publish/FocusDeck.Server.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=focusdeck
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000

# Security settings
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=$APP_DIR/publish

[Install]
WantedBy=multi-user.target
EOF
    
    systemctl daemon-reload
    systemctl enable focusdeck.service
    
    fn_print "✓ Systemd service created and enabled"
}

fn_setup_sudo_permissions() {
    fn_print "Configuring sudo permissions for web updates..."
    
    # Allow the service user to restart itself without password
    SUDOERS_FILE="/etc/sudoers.d/focusdeck"
    
    cat > "$SUDOERS_FILE" << EOF
$SERVICE_USER ALL=(ALL) NOPASSWD: /bin/systemctl restart focusdeck
$SERVICE_USER ALL=(ALL) NOPASSWD: /bin/systemctl stop focusdeck
$SERVICE_USER ALL=(ALL) NOPASSWD: /bin/systemctl start focusdeck
$SERVICE_USER ALL=(ALL) NOPASSWD: /bin/systemctl status focusdeck
EOF
    
    chmod 0440 "$SUDOERS_FILE"
    
    fn_print "✓ Sudo permissions configured"
}

# --- Start Service ---
fn_start_service() {
    fn_print "Starting FocusDeck service..."
    
    systemctl restart focusdeck.service
    sleep 3
    
    if systemctl is-active --quiet focusdeck.service; then
        fn_print "✓ Service started successfully"
    else
        fn_error "Service failed to start. Check logs: journalctl -u focusdeck -n 50"
    fi
}

# --- Final Instructions ---
fn_print_instructions() {
    local_ip=$(hostname -I | awk '{print $1}')
    
    echo ""
    echo -e "${GREEN}╔═══════════════════════════════════════════════════════╗${NC}"
    echo -e "${GREEN}║                                                       ║${NC}"
    echo -e "${GREEN}║       ✅ FocusDeck Server Installation Complete!     ║${NC}"
    echo -e "${GREEN}║                                                       ║${NC}"
    echo -e "${GREEN}╚═══════════════════════════════════════════════════════╝${NC}"
    echo ""
    echo -e "${CYAN}Server Access:${NC}"
    echo "  Local:    http://localhost:5000"
    echo "  Network:  http://${local_ip}:5000"
    if [ -n "$DOMAIN" ] && [ "$DOMAIN" != "localhost" ]; then
        echo "  Domain:   https://${DOMAIN} (after tunnel setup)"
    fi
    echo ""
    echo -e "${CYAN}Management Commands:${NC}"
    echo "  Status:   sudo systemctl status focusdeck"
    echo "  Restart:  sudo systemctl restart focusdeck"
    echo "  Logs:     sudo journalctl -u focusdeck -f"
    echo "  Config:   $APP_DIR/publish/appsettings.json"
    echo ""
    
    if [ "$CLOUDFLARED_INSTALLED" = true ] && [ "$CF_METHOD" = "token" ]; then
        echo -e "${YELLOW}╔═══════════════════════════════════════════════════╗${NC}"
        echo -e "${YELLOW}║   Cloudflare Tunnel (Token) Configured            ║${NC}"
        echo -e "${YELLOW}╚═══════════════════════════════════════════════════╝${NC}"
        echo ""
        echo "The cloudflared service was installed using your token."
        echo "If you need to reinstall it later, run:"
        echo -e "  ${CYAN}sudo cloudflared service install <token>${NC}"
        echo "To view logs:"
        echo -e "  ${CYAN}sudo journalctl -u cloudflared -f${NC}"
        echo ""
    elif [ "$CLOUDFLARED_INSTALLED" = true ]; then
        echo -e "${YELLOW}╔═══════════════════════════════════════════════════╗${NC}"
        echo -e "${YELLOW}║   Next: Configure Cloudflare Tunnel               ║${NC}"
        echo -e "${YELLOW}╚═══════════════════════════════════════════════════╝${NC}"
        echo ""
        echo "1. Login to Cloudflare:"
        echo -e "   ${CYAN}sudo cloudflared tunnel login${NC}"
        echo ""
        echo "2. Create a tunnel:"
        echo -e "   ${CYAN}sudo cloudflared tunnel create focusdeck${NC}"
        echo "   (Note the Tunnel ID)"
        echo ""
        echo "3. Create config file at /etc/cloudflared/config.yml:"
        echo -e "${CYAN}"
        cat << 'EOF'
tunnel: YOUR-TUNNEL-ID
credentials-file: /root/.cloudflared/YOUR-CREDENTIALS-FILE.json

ingress:
  - hostname: your-domain.com
    service: http://localhost:5000
    originRequest:
      noTLSVerify: false
  - service: http_status:404
EOF
        echo -e "${NC}"
        echo "4. Route DNS:"
        echo -e "   ${CYAN}sudo cloudflared tunnel route dns focusdeck your-domain.com${NC}"
        echo ""
        echo "5. Install and start the tunnel service:"
        echo -e "   ${CYAN}sudo cloudflared service install${NC}"
        echo -e "   ${CYAN}sudo systemctl start cloudflared${NC}"
        echo -e "   ${CYAN}sudo systemctl enable cloudflared${NC}"
        echo ""
        echo -e "${GREEN}✓ After completing these steps, your server will be accessible at https://your-domain.com${NC}"
    else
        echo -e "${YELLOW}╔═══════════════════════════════════════════════════╗${NC}"
        echo -e "${YELLOW}║   Configure External Tunnel Manually              ║${NC}"
        echo -e "${YELLOW}╚═══════════════════════════════════════════════════╝${NC}"
        echo ""
        echo "Point your existing tunnel to: http://${local_ip}:5000"
        echo ""
        echo -e "${RED}⚠️  Warning: External tunnels may not preserve client IPs,${NC}"
        echo -e "${RED}   which can weaken authentication security.${NC}"
    fi
    
    echo ""
    echo -e "${CYAN}Web UI Update System:${NC}"
    echo "  Access Settings → Server Management in the web UI"
    echo "  to update the server with one click."
    echo ""
}

# --- Main Execution ---
main() {
    fn_banner
    fn_check_root
    fn_check_distro
    
    echo ""
    fn_print "Starting installation..."
    echo ""
    
    fn_prompt_cloudflare
    fn_install_dependencies
    fn_install_dotnet
    fn_create_user
    fn_clone_repo
    fn_build_server
    fn_create_config
    fn_setup_database
    fn_create_service
    fn_setup_sudo_permissions
    fn_start_service
    fn_print_instructions
}

main "$@"
