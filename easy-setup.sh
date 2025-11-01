#!/bin/bash
##############################################################################
# FocusDeck Easy Setup Script
# One-command setup for Cloudflare deployment
#
# Usage: sudo bash easy-setup.sh
##############################################################################

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

clear
echo -e "${CYAN}"
cat << "EOF"
╔═══════════════════════════════════════════════════════════════╗
║                                                               ║
║              ░█▀▀░█▀█░█▀▀░█░█░█▀▀░█▀▄░█▀▀░█▀▀░█░█              ║
║              ░█▀▀░█░█░█░░░█░█░▀▀█░█░█░█▀▀░█░░░█▀▄              ║
║              ░▀░░░▀▀▀░▀▀▀░▀▀▀░▀▀▀░▀▀░░▀▀▀░▀▀▀░▀░▀              ║
║                                                               ║
║                    Easy Setup Script                          ║
║                                                               ║
╚═══════════════════════════════════════════════════════════════╝
EOF
echo -e "${NC}"
echo ""

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   echo -e "${RED}❌ This script must be run as root (use sudo)${NC}"
   exit 1
fi

echo -e "${BLUE}📋 This script will:${NC}"
echo "   1. Install dependencies (.NET 9.0, Git)"
echo "   2. Create FocusDeck user and clone repository"
echo "   3. Generate secure JWT key"
echo "   4. Configure systemd service"
echo "   5. Set up sudo permissions"
echo "   6. Start FocusDeck server"
echo ""
echo -e "${YELLOW}⚠️  You'll need your Cloudflare domain name${NC}"
echo ""
echo -n "Press Enter to continue or Ctrl+C to cancel..."
read -r </dev/tty
echo ""

# ============================================================================
# STEP 1: Get user input
# ============================================================================

echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}📝 Configuration${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

# Get Cloudflare domain with retry logic
CF_DOMAIN=""
while [[ -z "$CF_DOMAIN" ]]; do
    echo -n "Enter your Cloudflare domain (e.g., focusdeck.909436.xyz): "
    read -r CF_DOMAIN </dev/tty
    
    # Trim whitespace
    CF_DOMAIN=$(echo "$CF_DOMAIN" | xargs)
    
    if [[ -z "$CF_DOMAIN" ]]; then
        echo -e "${RED}❌ Domain cannot be empty!${NC}"
        echo ""
    else
        # Remove protocol if user included it
        CF_DOMAIN=$(echo "$CF_DOMAIN" | sed 's|https\?://||' | sed 's|/$||')
        echo -e "${GREEN}✓ Domain set to: ${CF_DOMAIN}${NC}"
    fi
done

echo ""
echo -n "Enter FocusDeck username [default: focusdeck]: "
read -r FOCUSDECK_USER </dev/tty
FOCUSDECK_USER="${FOCUSDECK_USER:-focusdeck}"
# Trim whitespace
FOCUSDECK_USER=$(echo "$FOCUSDECK_USER" | xargs)

INSTALL_DIR="/home/${FOCUSDECK_USER}/FocusDeck"
echo ""
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}📋 Configuration Summary${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${GREEN}✓ Domain: https://${CF_DOMAIN}${NC}"
echo -e "${GREEN}✓ User: ${FOCUSDECK_USER}${NC}"
echo -e "${GREEN}✓ Install directory: ${INSTALL_DIR}${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""
echo -n "Does this look correct? [Y/n]: "
read -r CONFIRM </dev/tty
CONFIRM="${CONFIRM:-Y}"
if [[ ! "$CONFIRM" =~ ^[Yy] ]]; then
    echo -e "${YELLOW}Setup cancelled. Run the script again to start over.${NC}"
    exit 0
fi
echo ""

# ============================================================================
# STEP 2: Install dependencies
# ============================================================================

echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}📦 Installing dependencies...${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

# Update package list
echo -e "${YELLOW}➜${NC} Updating package list..."
apt-get update -qq

# Install Git if not present
if ! command -v git &> /dev/null; then
    echo -e "${YELLOW}➜${NC} Installing Git..."
    apt-get install -y git
    echo -e "${GREEN}✓ Git installed${NC}"
else
    echo -e "${GREEN}✓ Git already installed${NC}"
fi

# Install .NET 9.0 if not present
if ! command -v dotnet &> /dev/null || ! dotnet --list-sdks 2>/dev/null | grep -q "9.0"; then
    echo -e "${YELLOW}➜${NC} Installing .NET 9.0 SDK..."
    
    # Install prerequisites
    apt-get install -y wget apt-transport-https
    
    # Detect Ubuntu/Debian version
    if [ -f /etc/os-release ]; then
        . /etc/os-release
        OS_VERSION=$VERSION_ID
        OS_NAME=$ID
        
        echo -e "${YELLOW}   Detected: $OS_NAME $OS_VERSION${NC}"
        
        # Add Microsoft package repository based on OS
        if [ "$OS_NAME" = "ubuntu" ]; then
            wget -q https://packages.microsoft.com/config/ubuntu/$OS_VERSION/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
        elif [ "$OS_NAME" = "debian" ]; then
            wget -q https://packages.microsoft.com/config/debian/$OS_VERSION/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
        else
            echo -e "${RED}   Unsupported OS: $OS_NAME${NC}"
            echo -e "${YELLOW}   Trying generic installation...${NC}"
            wget -q https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
        fi
        
        if [ -f packages-microsoft-prod.deb ]; then
            dpkg -i packages-microsoft-prod.deb 2>/dev/null
            rm -f packages-microsoft-prod.deb
            
            # Update and install
            apt-get update -qq
            apt-get install -y dotnet-sdk-9.0
            
            # Verify installation
            if dotnet --version &>/dev/null; then
                echo -e "${GREEN}✓ .NET 9.0 SDK installed successfully${NC}"
                echo -e "${GREEN}   Version: $(dotnet --version)${NC}"
            else
                echo -e "${RED}✗ .NET SDK installation failed${NC}"
                echo -e "${YELLOW}   Please install .NET 9.0 manually: https://dotnet.microsoft.com/download${NC}"
                exit 1
            fi
        else
            echo -e "${RED}✗ Failed to download .NET installer${NC}"
            exit 1
        fi
    else
        echo -e "${RED}✗ Cannot detect OS version${NC}"
        exit 1
    fi
else
    echo -e "${GREEN}✓ .NET 9.0 SDK already installed${NC}"
    echo -e "${GREEN}   Version: $(dotnet --version)${NC}"
fi

# Install OpenSSL for key generation
if ! command -v openssl &> /dev/null; then
    echo -e "${YELLOW}➜${NC} Installing OpenSSL..."
    apt-get install -y openssl
    echo -e "${GREEN}✓ OpenSSL installed${NC}"
else
    echo -e "${GREEN}✓ OpenSSL already installed${NC}"
fi

echo ""

# ============================================================================
# STEP 3: Create user and clone repository
# ============================================================================

echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}👤 Setting up user and repository...${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

# Create user if doesn't exist
if ! id "$FOCUSDECK_USER" &>/dev/null; then
    echo -e "${YELLOW}➜${NC} Creating user: ${FOCUSDECK_USER}..."
    useradd -m -s /bin/bash "$FOCUSDECK_USER"
    echo -e "${GREEN}✓ User created${NC}"
else
    echo -e "${GREEN}✓ User already exists${NC}"
fi

# Clone or update repository
if [ -d "$INSTALL_DIR" ]; then
    echo -e "${YELLOW}➜${NC} Repository exists, pulling latest changes..."
    cd "$INSTALL_DIR"
    sudo -u "$FOCUSDECK_USER" git pull origin master
    echo -e "${GREEN}✓ Repository updated${NC}"
else
    echo -e "${YELLOW}➜${NC} Cloning repository..."
    sudo -u "$FOCUSDECK_USER" git clone https://github.com/dertder25t-png/FocusDeck.git "$INSTALL_DIR"
    echo -e "${GREEN}✓ Repository cloned${NC}"
fi

chown -R "$FOCUSDECK_USER:$FOCUSDECK_USER" "$INSTALL_DIR"

echo ""

# ============================================================================
# STEP 4: Generate secure JWT key
# ============================================================================

echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}🔐 Generating secure JWT key...${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

JWT_KEY=$(openssl rand -base64 32)
echo -e "${GREEN}✓ JWT key generated${NC}"
echo ""

# ============================================================================
# STEP 5: Build the application
# ============================================================================

echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}🔨 Building FocusDeck...${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

cd "$INSTALL_DIR"
echo -e "${YELLOW}➜${NC} Building in Release mode..."
sudo -u "$FOCUSDECK_USER" dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj -c Release -v quiet

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Build successful${NC}"
else
    echo -e "${RED}❌ Build failed!${NC}"
    exit 1
fi

echo ""

# ============================================================================
# STEP 6: Configure systemd service
# ============================================================================

echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}⚙️  Configuring systemd service...${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

cat > /etc/systemd/system/focusdeck.service << EOF
[Unit]
Description=FocusDeck Server
After=network.target

[Service]
Type=notify
User=${FOCUSDECK_USER}
Group=${FOCUSDECK_USER}
WorkingDirectory=${INSTALL_DIR}/src/FocusDeck.Server

# Bind to all interfaces for Cloudflare Tunnel
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000

# Enable forwarded headers for Cloudflare proxy
Environment=ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

# Use Production environment
Environment=ASPNETCORE_ENVIRONMENT=Production

# JWT Configuration
Environment=Jwt__Issuer=https://${CF_DOMAIN}
Environment=Jwt__Audience=focusdeck-clients
Environment=Jwt__Key=${JWT_KEY}

# Repository path for update system
Environment=FOCUSDECK_REPO=${INSTALL_DIR}

# Start the application
ExecStart=/usr/bin/dotnet ${INSTALL_DIR}/src/FocusDeck.Server/bin/Release/net9.0/FocusDeck.Server.dll

Restart=always
RestartSec=10
KillSignal=SIGINT

[Install]
WantedBy=multi-user.target
EOF

echo -e "${GREEN}✓ Systemd service configured${NC}"
echo ""

# ============================================================================
# STEP 7: Set up sudo permissions
# ============================================================================

echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}🔒 Configuring sudo permissions...${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

cat > /etc/sudoers.d/focusdeck << EOF
# FocusDeck update system permissions
${FOCUSDECK_USER} ALL=(root) NOPASSWD: /usr/bin/systemctl restart focusdeck
${FOCUSDECK_USER} ALL=(root) NOPASSWD: /usr/bin/systemctl status focusdeck
${FOCUSDECK_USER} ALL=(root) NOPASSWD: /usr/bin/systemctl is-active focusdeck
${FOCUSDECK_USER} ALL=(root) NOPASSWD: ${INSTALL_DIR}/configure-update-system.sh
${FOCUSDECK_USER} ALL=(root) NOPASSWD: /usr/bin/git
${FOCUSDECK_USER} ALL=(root) NOPASSWD: /usr/bin/mkdir
${FOCUSDECK_USER} ALL=(root) NOPASSWD: /usr/bin/chown
EOF

chmod 0440 /etc/sudoers.d/focusdeck
echo -e "${GREEN}✓ Sudo permissions configured${NC}"
echo ""

# ============================================================================
# STEP 8: Create log directory
# ============================================================================

echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}📁 Setting up log directory...${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

mkdir -p /var/log/focusdeck
chown "$FOCUSDECK_USER:$FOCUSDECK_USER" /var/log/focusdeck
chmod 755 /var/log/focusdeck
echo -e "${GREEN}✓ Log directory created${NC}"
echo ""

# ============================================================================
# STEP 9: Enable time synchronization
# ============================================================================

echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}🕐 Enabling time synchronization...${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

timedatectl set-ntp true
echo -e "${GREEN}✓ NTP enabled${NC}"
echo ""

# ============================================================================
# STEP 10: Start the service
# ============================================================================

echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}🚀 Starting FocusDeck...${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

systemctl daemon-reload
systemctl enable focusdeck
systemctl restart focusdeck

sleep 2

if systemctl is-active --quiet focusdeck; then
    echo -e "${GREEN}✓ FocusDeck is running!${NC}"
else
    echo -e "${RED}❌ FocusDeck failed to start${NC}"
    echo -e "${YELLOW}Check logs: journalctl -u focusdeck -n 50${NC}"
    exit 1
fi

echo ""

# ============================================================================
# STEP 11: Test the service
# ============================================================================

echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}🧪 Testing service...${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

echo -e "${YELLOW}➜${NC} Testing local health check..."
sleep 3
if curl -s http://localhost:5000/healthz | grep -q "ok"; then
    echo -e "${GREEN}✓ Local health check passed${NC}"
else
    echo -e "${YELLOW}⚠ Local health check failed (service may still be starting)${NC}"
fi

echo ""

# ============================================================================
# SUCCESS!
# ============================================================================

clear
echo -e "${GREEN}"
cat << "EOF"
╔═══════════════════════════════════════════════════════════════╗
║                                                               ║
║                  ✓ SETUP COMPLETE! ✓                         ║
║                                                               ║
╚═══════════════════════════════════════════════════════════════╝
EOF
echo -e "${NC}"
echo ""

echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${GREEN}✓ FocusDeck is now running!${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""
echo -e "${CYAN}📍 Access your server:${NC}"
echo -e "   🌐 Web UI:     ${GREEN}https://${CF_DOMAIN}${NC}"
echo -e "   ❤️  Health:     ${GREEN}https://${CF_DOMAIN}/healthz${NC}"
echo ""
echo -e "${CYAN}🔐 Important Security Info:${NC}"
echo -e "   Your JWT key has been securely generated and stored"
echo -e "   It's in: ${YELLOW}/etc/systemd/system/focusdeck.service${NC}"
echo ""
echo -e "${CYAN}📝 Next Steps:${NC}"
echo -e "   1. Open ${GREEN}https://${CF_DOMAIN}${NC} in your browser"
echo -e "   2. Go to Settings → Authentication Token"
echo -e "   3. Generate a token for your desktop app"
echo -e "   4. Copy the token to your desktop app (Settings → Sync)"
echo ""
echo -e "${CYAN}📊 Useful Commands:${NC}"
echo -e "   View logs:     ${YELLOW}journalctl -u focusdeck -f${NC}"
echo -e "   Check status:  ${YELLOW}systemctl status focusdeck${NC}"
echo -e "   Restart:       ${YELLOW}sudo systemctl restart focusdeck${NC}"
echo -e "   Stop:          ${YELLOW}sudo systemctl stop focusdeck${NC}"
echo ""
echo -e "${CYAN}🔧 Configuration:${NC}"
echo -e "   Service file:  ${YELLOW}/etc/systemd/system/focusdeck.service${NC}"
echo -e "   Install dir:   ${YELLOW}${INSTALL_DIR}${NC}"
echo -e "   Database:      ${YELLOW}${INSTALL_DIR}/src/FocusDeck.Server/focusdeck.db${NC}"
echo -e "   Logs:          ${YELLOW}/var/log/focusdeck/${NC}"
echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${GREEN}Need help? Check the documentation at:${NC}"
echo -e "${CYAN}https://github.com/dertder25t-png/FocusDeck${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""
