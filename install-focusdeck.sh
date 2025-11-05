#!/bin/bash
###############################################################################
# FocusDeck Server - Official Linux Installation Script
# 
# USAGE:
#   curl -sSL https://raw.githubusercontent.com/dertder25t-png/FocusDeck/master/install-focusdeck.sh | sudo bash
#
# SUPPORTED: Ubuntu 20.04+ | Debian 11+
# TIME: 5-10 minutes
###############################################################################

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

# Configuration
DOTNET_VERSION="9.0"
FOCUSDECK_USER="focusdeck"
FOCUSDECK_HOME="/home/${FOCUSDECK_USER}/FocusDeck"
FOCUSDECK_REPO="https://github.com/dertder25t-png/FocusDeck.git"
SERVICE_NAME="focusdeck"
SERVICE_PORT="5000"

# Functions
print_header() {
    echo -e "${CYAN}"
    echo ""
    echo "  FocusDeck Server Installation                             "
    echo "  .NET ${DOTNET_VERSION} | Ubuntu/Debian                          "
    echo ""
    echo -e "${NC}"
}

print_step() {
    echo -e "${BLUE} $1${NC}"
}

print_success() {
    echo -e "${GREEN} $1${NC}"
}

print_error() {
    echo -e "${RED} $1${NC}"
    exit 1
}

# Check permissions
if [ "$EUID" -ne 0 ]; then
    echo -e "${RED} Must run with sudo${NC}"
    exit 1
fi

print_header

# Main steps
print_step "Updating system..."
apt-get update -qq
apt-get upgrade -y -qq > /dev/null 2>&1
print_success "System updated"

print_step "Installing Git..."
if ! command -v git &> /dev/null; then
    apt-get install -y -qq git > /dev/null 2>&1
fi
print_success "Git ready"

print_step "Installing .NET SDK..."
if ! command -v dotnet &> /dev/null; then
    cd /tmp
    wget -q https://dot.net/v1/dotnet-install.sh
    chmod +x dotnet-install.sh
    ./dotnet-install.sh --channel ${DOTNET_VERSION} -InstallDir /usr/local/dotnet 2>&1 | grep -v "Downloading" || true
    rm -f dotnet-install.sh
    ln -sf /usr/local/dotnet/dotnet /usr/bin/dotnet
fi
print_success ".NET $(dotnet --version)"

print_step "Creating system user..."
if ! id "${FOCUSDECK_USER}" &>/dev/null; then
    useradd -m -s /bin/bash "${FOCUSDECK_USER}"
fi
print_success "User ready"

print_step "Cloning repository..."
if [ -d "${FOCUSDECK_HOME}" ]; then
    cd "${FOCUSDECK_HOME}"
    git pull origin master > /dev/null 2>&1 || print_error "Failed to pull repository"
else
    git clone https://github.com/dertder25t-png/FocusDeck.git "${FOCUSDECK_HOME}" || print_error "Failed to clone repository"
    chown -R ${FOCUSDECK_USER}:${FOCUSDECK_USER} "${FOCUSDECK_HOME}"
fi
print_success "Repository ready"

print_step "Creating data directory..."
mkdir -p "${FOCUSDECK_HOME}/data"
chown ${FOCUSDECK_USER}:${FOCUSDECK_USER} "${FOCUSDECK_HOME}/data"
print_success "Data directory ready"

print_step "Applying Linux SDK 9.0 compatibility workaround..."
cd "${FOCUSDECK_HOME}"
# This fixes the GenerateTargetFrameworkMonikerAttribute error on Linux
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
export DOTNET_TRY_OPEN_TELEMETRY=false
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true
rm -rf src/*/bin src/*/obj tests/*/bin tests/*/obj FocusDeck.sln.DotSettings
dotnet clean -c Release 2>/dev/null || true
print_success "Build environment cleaned"

print_step "Restoring dependencies (this may take a moment)..."
cd "${FOCUSDECK_HOME}"
# Restore solution - this creates project.assets.json files in each project
dotnet restore --force || print_error "Failed to restore dependencies"
print_success "Dependencies restored"

print_step "Building server (Release mode - sequential to avoid race conditions)..."
cd "${FOCUSDECK_HOME}"

# Helper function to build a project
build_project() {
    local project=$1
    local name=$2
    echo -n "  Building ${name}..."
    if dotnet build "${project}" -c Release --no-restore -p:GenerateTargetFrameworkMonikerAttribute=false > /tmp/build.log 2>&1; then
        echo " ✓"
    else
        echo " FAILED"
        cat /tmp/build.log
        print_error "${name} build failed"
    fi
}

# Build each project sequentially
build_project "src/FocusDeck.Domain/FocusDeck.Domain.csproj" "Domain"
build_project "src/FocusDeck.SharedKernel/FocusDeck.SharedKernel.csproj" "SharedKernel"
build_project "src/FocusDeck.Shared/FocusDeck.Shared.csproj" "Shared"
build_project "src/FocusDeck.Contracts/FocusDeck.Contracts.csproj" "Contracts"
build_project "src/FocusDeck.Persistence/FocusDeck.Persistence.csproj" "Persistence"
build_project "src/FocusDeck.Server/FocusDeck.Server.csproj" "Server"

print_success "All projects built successfully"

print_step "Publishing server..."
cd "${FOCUSDECK_HOME}"
echo -n "  Publishing..."
if dotnet publish src/FocusDeck.Server/FocusDeck.Server.csproj -c Release -o "${FOCUSDECK_HOME}/publish" --no-build -p:GenerateTargetFrameworkMonikerAttribute=false > /tmp/publish.log 2>&1; then
    echo " ✓"
else
    echo " FAILED"
    cat /tmp/publish.log
    print_error "Publish failed"
fi
chown -R ${FOCUSDECK_USER}:${FOCUSDECK_USER} "${FOCUSDECK_HOME}/publish"
print_success "Build and publish complete"

print_step "Applying Linux SDK 9.0 compatibility workaround..."
cd "${FOCUSDECK_HOME}"
# This fixes the GenerateTargetFrameworkMonikerAttribute error on Linux
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
rm -rf src/*/bin src/*/obj tests/*/bin tests/*/obj .nuget/NuGet/cache
dotnet clean -c Release 2>/dev/null || true
print_success "Build environment cleaned"

print_step "Restoring dependencies (with workaround flags)..."
cd "${FOCUSDECK_HOME}"
export DOTNET_TRY_OPEN_TELEMETRY=false
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true
dotnet restore --force --disable-parallel --no-cache 2>&1 | grep -E "(error|warning:|Restored)" || true
if [ ${PIPESTATUS[0]} -ne 0 ]; then
    print_error "Dependency restore failed"
fi
print_success "Dependencies restored"

print_step "Building server (Release mode)..."
cd "${FOCUSDECK_HOME}"
# Sequential build to avoid race conditions
dotnet build src/FocusDeck.Domain/FocusDeck.Domain.csproj -c Release --no-restore --no-dependencies -p:GenerateTargetFrameworkMonikerAttribute=false || print_error "Domain build failed"
dotnet build src/FocusDeck.SharedKernel/FocusDeck.SharedKernel.csproj -c Release --no-restore --no-dependencies -p:GenerateTargetFrameworkMonikerAttribute=false || print_error "SharedKernel build failed"
dotnet build src/FocusDeck.Shared/FocusDeck.Shared.csproj -c Release --no-restore --no-dependencies -p:GenerateTargetFrameworkMonikerAttribute=false || print_error "Shared build failed"
dotnet build src/FocusDeck.Contracts/FocusDeck.Contracts.csproj -c Release --no-restore --no-dependencies -p:GenerateTargetFrameworkMonikerAttribute=false || print_error "Contracts build failed"
dotnet build src/FocusDeck.Persistence/FocusDeck.Persistence.csproj -c Release --no-restore -p:GenerateTargetFrameworkMonikerAttribute=false || print_error "Persistence build failed"
dotnet build src/FocusDeck.Server/FocusDeck.Server.csproj -c Release --no-restore -p:GenerateTargetFrameworkMonikerAttribute=false || print_error "Server build failed"
print_success "Build completed successfully"

print_step "Publishing server..."
cd "${FOCUSDECK_HOME}"
dotnet publish src/FocusDeck.Server/FocusDeck.Server.csproj -c Release -o "${FOCUSDECK_HOME}/publish" --no-build -p:GenerateTargetFrameworkMonikerAttribute=false || print_error "Publish failed"
chown -R ${FOCUSDECK_USER}:${FOCUSDECK_USER} "${FOCUSDECK_HOME}/publish"
print_success "Build and publish complete"

print_step "Configuring systemd service..."
cat > /etc/systemd/system/${SERVICE_NAME}.service << EOF
[Unit]
Description=FocusDeck Server
After=network.target

[Service]
Type=simple
User=${FOCUSDECK_USER}
WorkingDirectory=${FOCUSDECK_HOME}
Environment="ASPNETCORE_ENVIRONMENT=Production"
Environment="ASPNETCORE_URLS=http://0.0.0.0:${SERVICE_PORT}"
ExecStart=/usr/bin/dotnet ${FOCUSDECK_HOME}/publish/FocusDeck.Server.dll
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable ${SERVICE_NAME}
systemctl start ${SERVICE_NAME} || print_error "Failed to start service"
print_success "Service configured and started"

echo -e ""
echo -e "${GREEN}${NC}"
echo -e "${GREEN}  Installation Complete!                                   ${NC}"
echo -e "${GREEN}${NC}"
echo -e ""
echo -e "${CYAN}Quick Commands:${NC}"
echo -e "  Status:  sudo systemctl status focusdeck"
echo -e "  Logs:    sudo journalctl -u focusdeck -f"
echo -e "  Restart: sudo systemctl restart focusdeck"
echo -e ""
echo -e "${CYAN}Access:${NC}"
echo -e "  Local: http://localhost:5000"
echo -e "  Check: http://localhost:5000/v1/system/health"
echo -e ""