#!/bin/bash
###############################################################################
# FocusDeck Server - Unified Startup Script
# This is the ONLY startup script you need!
# Usage: ./start-focusdeck.sh [COMMAND]
# Commands: start, stop, restart, status, build, update, logs
###############################################################################

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
FOCUSDECK_HOME="${HOME}/FocusDeck"
SERVICE_NAME="focusdeck"
PORT="5239"
SYSTEMD_SERVICE="/etc/systemd/system/${SERVICE_NAME}.service"

# Helper functions
print_header() {
    echo -e "${BLUE}╔════════════════════════════════════════╗${NC}"
    echo -e "${BLUE}║ $1${NC}"
    echo -e "${BLUE}╚════════════════════════════════════════╝${NC}"
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_info() {
    echo -e "${YELLOW}ℹ $1${NC}"
}

# Function: Install dependencies
install_dependencies() {
    print_header "Installing Dependencies"
    
    if [ "$EUID" -ne 0 ]; then 
        SUDO="sudo"
    else 
        SUDO=""
    fi
    
    print_info "Updating system packages..."
    $SUDO apt-get update -qq
    $SUDO apt-get upgrade -y -qq
    
    # Install Git
    if ! command -v git &> /dev/null; then
        print_info "Installing Git..."
        $SUDO apt-get install -y git > /dev/null 2>&1
        print_success "Git installed"
    else
        print_success "Git already installed"
    fi
    
    # Install .NET SDK
    if ! command -v dotnet &> /dev/null; then
        print_info "Installing .NET 8 SDK..."
        if [ ! -f "dotnet-install.sh" ]; then
            wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh 2>/dev/null
            chmod +x dotnet-install.sh
        fi
        ./dotnet-install.sh --channel 8.0 > /dev/null 2>&1
        rm dotnet-install.sh
        
        export PATH=$PATH:$HOME/.dotnet
        echo 'export PATH=$PATH:$HOME/.dotnet' >> ~/.bashrc
        source ~/.bashrc
        print_success ".NET SDK installed"
    else
        print_success ".NET SDK already installed ($(dotnet --version))"
    fi
}

# Function: Clone or update repository
setup_repository() {
    print_header "Setting Up Repository"
    
    if [ ! -d "$FOCUSDECK_HOME" ]; then
        print_info "Cloning FocusDeck repository..."
        cd ~ || exit
        git clone https://github.com/dertder25t-png/FocusDeck.git 2>&1 | grep -E "(Cloning|done)"
        print_success "Repository cloned"
    else
        print_success "Repository already exists"
        print_info "Pulling latest changes..."
        cd "$FOCUSDECK_HOME"
        git pull origin master 2>&1 | grep -E "(Already up to date|Fast-forward|CONFLICT)" || true
    fi
}

# Function: Build the application
build_application() {
    print_header "Building FocusDeck Server"
    
    cd "$FOCUSDECK_HOME" || exit
    
    print_info "Restoring dependencies..."
    dotnet restore > /dev/null 2>&1
    
    print_info "Building application..."
    dotnet build ./src/FocusDeck.Server/FocusDeck.Server.csproj -c Release -q
    
    print_success "Build completed successfully"
}

# Function: Setup systemd service
setup_service() {
    print_header "Setting Up Systemd Service"
    
    if [ "$EUID" -ne 0 ]; then 
        SUDO="sudo"
    else 
        SUDO=""
    fi
    
    print_info "Creating systemd service file..."
    
    $SUDO tee "$SYSTEMD_SERVICE" > /dev/null <<EOF
[Unit]
Description=FocusDeck Server - Study & Focus Application
After=network.target
StartLimitInterval=200
StartLimitBurst=5

[Service]
Type=simple
User=$USER
WorkingDirectory=$FOCUSDECK_HOME/src/FocusDeck.Server
ExecStart=$HOME/.dotnet/dotnet run --configuration Release --urls "http://0.0.0.0:$PORT" --no-build
Restart=always
RestartSec=10
StandardOutput=journal
StandardError=journal

# Security
PrivateTmp=true
NoNewPrivileges=true

[Install]
WantedBy=multi-user.target
EOF

    $SUDO systemctl daemon-reload
    $SUDO systemctl enable "$SERVICE_NAME"
    
    print_success "Systemd service configured"
}

# Function: Start the server
start_server() {
    print_header "Starting FocusDeck Server"
    
    if [ "$EUID" -ne 0 ]; then 
        SUDO="sudo"
    else 
        SUDO=""
    fi
    
    if $SUDO systemctl is-active --quiet "$SERVICE_NAME"; then
        print_info "Server is already running"
        return 0
    fi
    
    $SUDO systemctl start "$SERVICE_NAME"
    
    # Wait a moment and check status
    sleep 2
    if $SUDO systemctl is-active --quiet "$SERVICE_NAME"; then
        print_success "Server started successfully"
        print_info "Web UI: http://localhost:$PORT"
        return 0
    else
        print_error "Failed to start server"
        $SUDO systemctl status "$SERVICE_NAME"
        return 1
    fi
}

# Function: Stop the server
stop_server() {
    print_header "Stopping FocusDeck Server"
    
    if [ "$EUID" -ne 0 ]; then 
        SUDO="sudo"
    else 
        SUDO=""
    fi
    
    if ! $SUDO systemctl is-active --quiet "$SERVICE_NAME"; then
        print_info "Server is not running"
        return 0
    fi
    
    $SUDO systemctl stop "$SERVICE_NAME"
    sleep 1
    print_success "Server stopped"
}

# Function: Restart the server
restart_server() {
    print_header "Restarting FocusDeck Server"
    
    if [ "$EUID" -ne 0 ]; then 
        SUDO="sudo"
    else 
        SUDO=""
    fi
    
    $SUDO systemctl restart "$SERVICE_NAME"
    sleep 2
    
    if $SUDO systemctl is-active --quiet "$SERVICE_NAME"; then
        print_success "Server restarted successfully"
        print_info "Web UI: http://localhost:$PORT"
    else
        print_error "Failed to restart server"
        return 1
    fi
}

# Function: Show server status
show_status() {
    print_header "FocusDeck Server Status"
    
    if [ "$EUID" -ne 0 ]; then 
        SUDO="sudo"
    else 
        SUDO=""
    fi
    
    $SUDO systemctl status "$SERVICE_NAME" || true
    
    echo ""
    print_info "Checking connectivity on port $PORT..."
    if netstat -tlnp 2>/dev/null | grep -q ":$PORT"; then
        print_success "Server is listening on port $PORT"
    else
        print_error "Server is not listening on port $PORT"
    fi
}

# Function: Show server logs
show_logs() {
    print_header "FocusDeck Server Logs (Last 50 lines)"
    
    if [ "$EUID" -ne 0 ]; then 
        SUDO="sudo"
    else 
        SUDO=""
    fi
    
    $SUDO journalctl -u "$SERVICE_NAME" -n 50 --no-pager -o short-iso
}

# Function: Update server
update_server() {
    print_header "Updating FocusDeck Server"
    
    print_info "Pulling latest code..."
    cd "$FOCUSDECK_HOME" || exit
    git pull origin master
    
    print_info "Building new version..."
    build_application
    
    print_info "Restarting service..."
    if [ "$EUID" -ne 0 ]; then 
        SUDO="sudo"
    else 
        SUDO=""
    fi
    
    $SUDO systemctl restart "$SERVICE_NAME"
    sleep 2
    
    print_success "Server updated and restarted"
}

# Function: Complete setup (fresh install)
complete_setup() {
    print_header "FocusDeck - Complete Fresh Setup"
    
    install_dependencies
    setup_repository
    build_application
    setup_service
    start_server
    
    echo ""
    print_header "Setup Complete! ✓"
    echo ""
    echo -e "${GREEN}Your FocusDeck server is ready!${NC}"
    echo ""
    echo "Quick commands:"
    echo "  Start:   $0 start"
    echo "  Stop:    $0 stop"
    echo "  Restart: $0 restart"
    echo "  Status:  $0 status"
    echo "  Logs:    $0 logs"
    echo "  Update:  $0 update"
    echo ""
    echo -e "${YELLOW}Access the web UI at: http://localhost:$PORT${NC}"
    echo ""
}

# Function: Print usage
print_usage() {
    cat << EOF
${BLUE}FocusDeck Server Control Script${NC}

Usage: $0 [COMMAND]

Commands:
  setup       Complete fresh installation and setup
  start       Start the FocusDeck server
  stop        Stop the FocusDeck server
  restart     Restart the FocusDeck server
  status      Show server status
  logs        Show server logs (last 50 lines)
  build       Rebuild the application
  update      Update and restart the server
  help        Show this help message

Examples:
  $0 setup              # First time: Install everything and start
  $0 start              # Start the server
  $0 restart            # Restart the server
  $0 logs               # View logs
  $0 update             # Pull latest code and restart

${YELLOW}For the first time, run: $0 setup${NC}
EOF
}

# Main script logic
COMMAND="${1:-help}"

case "$COMMAND" in
    setup)
        complete_setup
        ;;
    start)
        start_server
        ;;
    stop)
        stop_server
        ;;
    restart)
        restart_server
        ;;
    status)
        show_status
        ;;
    logs)
        show_logs
        ;;
    build)
        build_application
        ;;
    update)
        update_server
        ;;
    help|--help|-h)
        print_usage
        ;;
    *)
        print_error "Unknown command: $COMMAND"
        echo ""
        print_usage
        exit 1
        ;;
esac
