#!/bin/bash

# ============================================================================
# Cloudflare Tunnel Setup for FocusDeck
# ============================================================================
# This script sets up Cloudflare Tunnel (cloudflared) to expose your
# FocusDeck server to the internet via your Cloudflare domain.
# ============================================================================

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}🌐 Cloudflare Tunnel Setup for FocusDeck${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""
echo "This script will:"
echo "  1. Install cloudflared"
echo "  2. Authenticate with Cloudflare"
echo "  3. Create a tunnel"
echo "  4. Route your domain to the tunnel"
echo "  5. Configure the tunnel as a systemd service"
echo ""

# Check if running as root
if [ "$EUID" -ne 0 ]; then
    echo -e "${RED}Error: This script must be run as root${NC}"
    echo "Please run: sudo bash $0"
    exit 1
fi

# ============================================================================
# STEP 1: Install cloudflared
# ============================================================================

echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}📦 Installing cloudflared...${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

if command -v cloudflared &> /dev/null; then
    echo -e "${GREEN}✓ cloudflared already installed${NC}"
    cloudflared --version
else
    echo -e "${YELLOW}➜${NC} Installing cloudflared..."
    
    # Add Cloudflare GPG key and repo
    curl -fsSL https://pkg.cloudflare.com/cloudflare-main.gpg | tee /usr/share/keyrings/cloudflare-main.gpg >/dev/null
    echo "deb [signed-by=/usr/share/keyrings/cloudflare-main.gpg] https://pkg.cloudflare.com/cloudflared $(lsb_release -cs) main" | tee /etc/apt/sources.list.d/cloudflared.list
    
    apt-get update -qq
    apt-get install -y cloudflared
    
    echo -e "${GREEN}✓ cloudflared installed${NC}"
    cloudflared --version
fi

echo ""

# ============================================================================
# STEP 2: Get tunnel details from user
# ============================================================================

echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}📝 Configuration${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

# Get domain
DOMAIN=""
while [[ -z "$DOMAIN" ]]; do
    echo -n "Enter your Cloudflare domain (e.g., focusdeck.909436.xyz): "
    read -r DOMAIN </dev/tty
    DOMAIN=$(echo "$DOMAIN" | xargs | sed 's|https\?://||' | sed 's|/$||')
    
    if [[ -z "$DOMAIN" ]]; then
        echo -e "${RED}❌ Domain cannot be empty!${NC}"
        echo ""
    else
        echo -e "${GREEN}✓ Domain set to: ${DOMAIN}${NC}"
    fi
done

# Get tunnel name
echo ""
echo -n "Enter tunnel name [default: focusdeck-tunnel]: "
read -r TUNNEL_NAME </dev/tty
TUNNEL_NAME="${TUNNEL_NAME:-focusdeck-tunnel}"
TUNNEL_NAME=$(echo "$TUNNEL_NAME" | xargs)

echo ""
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}📋 Configuration Summary${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${GREEN}✓ Domain: ${DOMAIN}${NC}"
echo -e "${GREEN}✓ Tunnel name: ${TUNNEL_NAME}${NC}"
echo -e "${GREEN}✓ Local service: http://localhost:5000${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

# ============================================================================
# STEP 3: Authenticate with Cloudflare
# ============================================================================

echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}🔐 Cloudflare Authentication${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

if [ ! -f ~/.cloudflared/cert.pem ]; then
    echo -e "${YELLOW}⚠ You need to authenticate with Cloudflare${NC}"
    echo ""
    echo "A browser window will open. Please:"
    echo "  1. Log in to your Cloudflare account"
    echo "  2. Select the domain: ${DOMAIN}"
    echo "  3. Authorize the tunnel"
    echo ""
    echo -n "Press Enter to open the browser..."
    read -r </dev/tty
    
    cloudflared tunnel login
    
    if [ -f ~/.cloudflared/cert.pem ]; then
        echo -e "${GREEN}✓ Successfully authenticated with Cloudflare${NC}"
    else
        echo -e "${RED}✗ Authentication failed${NC}"
        exit 1
    fi
else
    echo -e "${GREEN}✓ Already authenticated with Cloudflare${NC}"
fi

echo ""

# ============================================================================
# STEP 4: Create tunnel
# ============================================================================

echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}🚇 Creating Cloudflare Tunnel${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

# Check if tunnel already exists
if cloudflared tunnel list | grep -q "$TUNNEL_NAME"; then
    echo -e "${YELLOW}⚠ Tunnel '$TUNNEL_NAME' already exists${NC}"
    TUNNEL_ID=$(cloudflared tunnel list | grep "$TUNNEL_NAME" | awk '{print $1}')
    echo -e "${GREEN}✓ Using existing tunnel ID: ${TUNNEL_ID}${NC}"
else
    echo -e "${YELLOW}➜${NC} Creating tunnel..."
    cloudflared tunnel create "$TUNNEL_NAME"
    TUNNEL_ID=$(cloudflared tunnel list | grep "$TUNNEL_NAME" | awk '{print $1}')
    echo -e "${GREEN}✓ Tunnel created with ID: ${TUNNEL_ID}${NC}"
fi

echo ""

# ============================================================================
# STEP 5: Route domain to tunnel
# ============================================================================

echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}🔗 Routing Domain to Tunnel${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

echo -e "${YELLOW}➜${NC} Creating DNS route..."
cloudflared tunnel route dns "$TUNNEL_ID" "$DOMAIN" || {
    echo -e "${YELLOW}⚠ Route may already exist${NC}"
}
echo -e "${GREEN}✓ Domain routed to tunnel${NC}"

echo ""

# ============================================================================
# STEP 6: Create tunnel configuration
# ============================================================================

echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}⚙️  Creating Tunnel Configuration${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

mkdir -p ~/.cloudflared

cat > ~/.cloudflared/config.yml << EOF
tunnel: $TUNNEL_ID
credentials-file: /root/.cloudflared/$TUNNEL_ID.json

ingress:
  - hostname: $DOMAIN
    service: http://localhost:5000
  - service: http_status:404
EOF

echo -e "${GREEN}✓ Configuration created at ~/.cloudflared/config.yml${NC}"

echo ""

# ============================================================================
# STEP 7: Create systemd service
# ============================================================================

echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}🔧 Configuring systemd service${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

cat > /etc/systemd/system/cloudflared.service << EOF
[Unit]
Description=Cloudflare Tunnel
After=network.target

[Service]
Type=simple
User=root
ExecStart=/usr/bin/cloudflared tunnel run
Restart=on-failure
RestartSec=5s

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable cloudflared.service
systemctl restart cloudflared.service

echo -e "${GREEN}✓ Cloudflare Tunnel service configured and started${NC}"

echo ""

# ============================================================================
# STEP 8: Verify tunnel status
# ============================================================================

echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}✅ Setup Complete!${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

sleep 2

if systemctl is-active --quiet cloudflared.service; then
    echo -e "${GREEN}✓ Cloudflare Tunnel is running${NC}"
else
    echo -e "${RED}✗ Cloudflare Tunnel service failed to start${NC}"
    echo "Check logs with: journalctl -xeu cloudflared.service"
    exit 1
fi

echo ""
echo -e "${GREEN}🎉 Your FocusDeck server should now be accessible at:${NC}"
echo -e "${CYAN}   https://${DOMAIN}${NC}"
echo ""
echo -e "${YELLOW}📝 Useful Commands:${NC}"
echo "  • Check tunnel status: sudo systemctl status cloudflared"
echo "  • View tunnel logs: sudo journalctl -fu cloudflared"
echo "  • Restart tunnel: sudo systemctl restart cloudflared"
echo "  • List all tunnels: cloudflared tunnel list"
echo ""
echo -e "${YELLOW}⚠️  IMPORTANT: Make sure your FocusDeck server is running!${NC}"
echo "  • Check server status: sudo systemctl status focusdeck"
echo "  • View server logs: sudo journalctl -fu focusdeck"
echo ""
