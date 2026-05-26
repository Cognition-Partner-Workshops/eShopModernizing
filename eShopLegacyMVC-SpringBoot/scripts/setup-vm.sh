#!/bin/bash
# setup-vm.sh — Idempotent script to prepare a fresh Azure VM for deployments.
# Usage: sudo ./setup-vm.sh [staging|production]
#   Defaults to 'production' if no argument is provided.
#
# This script:
#   1. Installs Docker (if not already installed)
#   2. Creates application directories
#   3. Generates an environment file template
#
# Tested on Ubuntu 22.04 LTS.

set -euo pipefail

ENV_NAME="${1:-production}"
APP_DIR="/opt/app"
ENV_FILE="$APP_DIR/${ENV_NAME}.env"

echo "=== eShop Catalog VM Setup ==="

# ── Install Docker ──────────────────────────────────────────────
if command -v docker &>/dev/null; then
    echo "[OK] Docker is already installed: $(docker --version)"
else
    echo "[*] Installing Docker..."
    apt-get update -qq
    apt-get install -y -qq ca-certificates curl gnupg

    install -m 0755 -d /etc/apt/keyrings
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg \
        | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
    chmod a+r /etc/apt/keyrings/docker.gpg

    echo \
        "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
        https://download.docker.com/linux/ubuntu \
        $(. /etc/os-release && echo "$VERSION_CODENAME") stable" \
        | tee /etc/apt/sources.list.d/docker.list > /dev/null

    apt-get update -qq
    apt-get install -y -qq docker-ce docker-ce-cli containerd.io docker-compose-plugin

    systemctl enable docker
    systemctl start docker
    echo "[OK] Docker installed: $(docker --version)"
fi

# Add current sudo user to docker group (if invoked via sudo)
if [ -n "${SUDO_USER:-}" ]; then
    if ! groups "$SUDO_USER" | grep -q docker; then
        usermod -aG docker "$SUDO_USER"
        echo "[OK] Added $SUDO_USER to docker group (re-login required)"
    fi
fi

# ── Create application directories ─────────────────────────────
mkdir -p "$APP_DIR"
chmod 755 "$APP_DIR"
echo "[OK] Application directory: $APP_DIR"

# ── Generate environment file template ──────────────────────────
if [ ! -f "$ENV_FILE" ]; then
    cat > "$ENV_FILE" <<'EOF'
# eShop Catalog — Environment Configuration
# Fill in actual values before the first deployment.

SPRING_PROFILES_ACTIVE=prod

# Database connection
DB_URL=jdbc:sqlserver://${DB_HOST}:1433;databaseName=${DB_NAME};encrypt=true;trustServerCertificate=true
DB_USERNAME=${DB_USER}
DB_PASSWORD=${DB_PASSWORD}
DB_DRIVER=com.microsoft.sqlserver.jdbc.SQLServerDriver

# Application settings
USE_MOCK_DATA=false
EOF
    chmod 600 "$ENV_FILE"
    echo "[OK] Environment template created: $ENV_FILE"
    echo "     Edit this file with actual database credentials before deploying."
else
    echo "[OK] Environment file already exists: $ENV_FILE (not overwritten)"
fi

echo ""
echo "=== Setup Complete ==="
echo "Next steps:"
echo "  1. Edit $ENV_FILE with actual database credentials"
echo "  2. Log out and back in for docker group membership to take effect"
echo "  3. Test with: docker run --rm hello-world"
