#!/bin/bash

# FocusDeck Server Easy-Setup Script
# Installs .NET 9, Git, and builds/configures FocusDeck
# Now includes optional Cloudflare Tunnel setup

# --- Colors ---
BLUE="\033[1;34m"
GREEN="\033[1;32m"
YELLOW="\033[0;33m"
RED="\033[0;31m"
NC="\033[0m"

# --- Globals ---
APP_DIR="/opt/focusdeck"
REPO_URL="https://github.com/dertder25t-png/FocusDeck.git"
SERVICE_USER="focusdeck"
SERVICE_FILE="/etc/systemd/system/focusdeck.service"
CLOUDFLARED_INSTALLED=false

# --- Helper Functions ---
fn_print() {
    echo -e "${GREEN}FocusDeck Setup:${NC} $1"
}

fn_warn() {
    echo -e "${YELLOW}Warning:${NC} $1"
}

fn_error() {
    echo -e "${RED}Error:${NC} $1"
    exit 1
}

# --- Prerequisite Checks ---
fn_check_root() {
    if [ "$EUID" -ne 0 ]; then
        fn_error "This script must be run as root. Please use 'sudo'."
    fi
}

fn_check_distro() {
    if ! command -v apt-get &> /dev/null; then
        fn_error "This script is designed for Debian-based systems (like Ubuntu) that use 'apt-get'."
    fi
}

# --- New Cloudflare Functions ---
fn_prompt_cloudflare() {
    echo -e "${BLUE}--- Cloudflare Tunnel Setup (Recommended) ---${NC}"
    echo "This script can automatically install 'cloudflared' (Cloudflare Tunnel) on this server."
    echo ""
    echo -e "${GREEN}This is the recommended setup.${NC}"
    echo "Running the tunnel on the same server allows for better security by providing the application"
    echo "with the real client IP address, which is used for authentication."
    echo ""
    echo "If you choose [n], you will need to manually point your existing tunnel (e.g., from another VM)"
    echo "to this server's IP at http://<this_server_ip>:5000. Be aware this less-secure method"
    echo "may break client fingerprinting."
    echo ""
    read -r -p "Do you want to install 'cloudflared' on this server? [Y/n]: " response
    case "$response" in
        [nN][oO]|[nN])
            CLOUDFLARED_INSTALLED=false
            fn_print "Skipping 'cloudflared' installation. You must configure your tunnel manually."
            ;;
        *)
            CLOUDFLARED_INSTALLED=true
            fn_install_cloudflared
            ;;
    esac
    echo -e "${BLUE}------------------------------------------------${NC}"
    sleep 2
}

fn_install_cloudflared() {
    fn_print "Installing 'cloudflared'..."
    if ! command -v wget &> /dev/null; then
        apt-get install -y wget
    fi
    
    # Download the .deb package
    wget https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64.deb -O /tmp/cloudflared.deb
    if [ $? -ne 0 ]; then
        fn_error "Failed to download 'cloudflared' package."
    fi

    # Install the package
    dpkg -i /tmp/cloudflared.deb
    if [ $? -ne 0 ]; then
        fn_warn "dpkg install failed. Attempting to fix dependencies..."
        apt-get install -f -y # Fix broken dependencies if any
        dpkg -i /tmp/cloudflared.deb # Try again
        if [ $? -ne 0 ]; then
            fn_error "Failed to install 'cloudflared' even after fixing dependencies."
        fi
    fi
    
    # Clean up
    rm /tmp/cloudflared.deb
    fn_print "'cloudflared' installed successfully."
}

# --- Core Installation Functions ---
fn_install_dotnet() {
    fn_print "Installing .NET 9 SDK..."
    # Add Microsoft package repository
    if [ ! -f /etc/apt/sources.list.d/microsoft-prod.list ]; then
        wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
        dpkg -i /tmp/packages-microsoft-prod.deb
        rm /tmp/packages-microsoft-prod.deb
    fi
    
    apt-get update
    apt-get install -y apt-transport-https
    apt-get install -y dotnet-sdk-9.0
    if [ $? -ne 0 ]; then
        fn_error "Failed to install .NET 9 SDK."
    fi
}

fn_install_git() {
    fn_print "Installing Git..."
    if ! command -v git &> /dev/null; then
        apt-get install -y git
        if [ $? -ne 0 ]; then
            fn_error "Failed to install Git."
        fi
    fi
}

fn_create_user() {
    fn_print "Creating service user '$SERVICE_USER'..."
    if ! id "$SERVICE_USER" &>/dev/null; then
        useradd --system --shell /bin/false --home $APP_DIR $SERVICE_USER
    else
        fn_print "User '$SERVICE_USER' already exists."
    fi
}

fn_clone_repo() {
    fn_print "Cloning repository to $APP_DIR..."
    if [ -d "$APP_DIR" ]; then
        fn_print "Directory $APP_DIR exists, pulling latest changes..."
        cd "$APP_DIR" || fn_error "Failed to change to directory $APP_DIR"
        git reset --hard HEAD
        git pull origin master
    else
        git clone $REPO_URL $APP_DIR
    fi
    
    if [ $? -ne 0 ]; then
        fn_error "Failed to clone or pull the repository."
    fi
    
    chown -R $SERVICE_USER:$SERVICE_USER $APP_DIR
}

fn_build_server() {
    fn_print "Building FocusDeck.Server..."
    local project_path="$APP_DIR/src/FocusDeck.Server"
    
    # Run build as the service user for permissions safety
    sudo -u $SERVICE_USER dotnet build "$project_path" -c Release
    if [ $? -ne 0 ]; then
        fn_error "Failed to build the server. Check build logs."
    fi

    fn_print "Publishing FocusDeck.Server..."
    sudo -u $SERVICE_USER dotnet publish "$project_path" -c Release -o "$APP_DIR/publish"
    if [ $? -ne 0 ]; then
        fn_error "Failed to publish the server."
    fi
}

fn_create_config() {
    fn_print "Creating default appsettings.json..."
    local config_path="$APP_DIR/publish/appsettings.json"
    
    if [ -f "$config_path" ]; then
        fn_warn "appsettings.json already exists. Skipping creation."
        return
    fi

    # Generate a secure 32-character+ key
    JWT_SECRET=$(head -c 32 /dev/urandom | base64)

    cat > $config_path << EOL
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=focusdeck.db"
  },
  "Jwt": {
    "Secret": "${JWT_SECRET}",
    "Issuer": "FocusDeck.Server",
    "Audience": "FocusDeck.Client",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
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
EOL

    chown $SERVICE_USER:$SERVICE_USER $config_path
    chmod 600 $config_path
    fn_print "Default appsettings.json created with a new JWT Secret."
}

fn_create_service() {
    fn_print "Creating systemd service..."
    
    cat > $SERVICE_FILE << EOL
[Unit]
Description=FocusDeck Server
After=network.target

[Service]
ExecStart=/usr/bin/dotnet $APP_DIR/publish/FocusDeck.Server.dll
WorkingDirectory=$APP_DIR/publish
User=$SERVICE_USER
Group=$SERVICE_USER
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
EOL

    systemctl daemon-reload
    systemctl enable focusdeck.service
    systemctl start focusdeck.service
    
    fn_print "Systemd service 'focusdeck' created and started."
}

# --- Final Instructions ---
fn_print_instructions() {
    local_ip=$(hostname -I | awk '{print $1}')

    echo -e "${GREEN}--- ✅ FocusDeck Server Installation Complete! ---${NC}"
    echo "The server is now running at: ${BLUE}http://localhost:5000${NC} (or ${BLUE}http://${local_ip}:5000${NC} from your network)"
    echo ""
    echo "Your configuration file is at: ${YELLOW}$APP_DIR/publish/appsettings.json${NC}"
    echo "The service is managed by: ${YELLOW}sudo systemctl (status|start|stop|restart) focusdeck${NC}"
    echo ""

    if [ "$CLOUDFLARED_INSTALLED" = true ]; then
        echo -e "${BLUE}--- Next Steps: Configure Cloudflare Tunnel ---${NC}"
        echo "You chose the recommended setup. 'cloudflared' is installed."
        echo "You must now authorize it and create the tunnel:"
        echo ""
        echo "1. ${YELLOW}Authorize 'cloudflared'${NC} (this will open a browser login):"
        echo "   sudo cloudflared tunnel login"
        echo ""
        echo "2. ${YELLOW}Create a tunnel${NC} (e.g., 'focusdeck'):"
        echo "   sudo cloudflared tunnel create focusdeck"
        echo "   (This will give you a Tunnel ID and a credentials file path)"
        echo ""
        echo "3. ${YELLOW}Create a config file${NC} in ${YELLOW}/etc/cloudflared/config.yml${NC}"
        echo "   (Replace with your Tunnel ID and credentials path)"
        echo ""
        echo "   tunnel: YOUR-TUNNEL-ID-HERE"
        echo "   credentials-file: /root/.cloudflared/YOUR-CREDENTIALS-FILE.json"
        echo "   ingress:"
        echo "     - hostname: your-domain.com"
        echo "       service: http://localhost:5000"
        echo "     - service: http_status:404"
        echo ""
        echo "4. ${YELLOW}Route traffic${NC} to your tunnel in the Cloudflare Dashboard or via CLI:"
        echo "   sudo cloudflared tunnel route dns focusdeck your-domain.com"
        echo ""
        echo "5. ${YELLOW}Run the tunnel${NC} (as a service):"
        echo "   sudo cloudflared service install"
        echo "   sudo systemctl start cloudflared"
        echo ""
    else
        echo -e "${YELLOW}--- Next Steps: Manual Tunnel Configuration ---${NC}"
        echo "You skipped the local 'cloudflared' install."
        echo "You must now point your existing tunnel (on your other VM) to this server."
        echo ""
        echo "1. On your tunnel VM, find your ${YELLOW}config.yml${NC}"
        echo "2. Add/update the ingress rule to point to this server's IP:"
        echo ""
        echo "   ingress:"
        echo "     - hostname: your-domain.com"
        echo "       service: http://${local_ip}:5000"
        echo "     - service: http_status:404"
        echo ""
        echo -e "${RED}Security Warning:${NC} This setup will break client IP detection and weaken"
        echo "authentication security. This is not the recommended configuration."
    fi
}

# --- Main Execution ---
main() {
    fn_check_root
    fn_check_distro
    
    # NEW STEP
    fn_prompt_cloudflare
    
    fn_install_dotnet
    fn_install_git
    fn_create_user
    fn_clone_repo
    fn_build_server
    fn_create_config
    fn_create_service
    
    fn_print_instructions
}

main "$@"