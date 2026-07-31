#!/bin/bash
set -euo pipefail

###############################################################################
# setup-vm.sh — Provision a fresh Ubuntu VM for running the eShop Catalog
#               Spring Boot application in Docker.
#
# Usage:
#   chmod +x setup-vm.sh
#   sudo ./setup-vm.sh
#
# What it does:
#   1. Installs Docker from the official repository
#   2. Enables and starts the Docker service
#   3. Adds the current (or sudo-invoking) user to the docker group
#   4. Creates the application directory at /opt/app
#   5. Generates an environment file template at /opt/app/production.env
#
# After running this script, log out and back in so the docker group
# membership takes effect.
###############################################################################

# Determine the non-root user (handles both direct and sudo invocation)
TARGET_USER="${SUDO_USER:-$USER}"

echo "==> Updating package index..."
apt-get update -y

echo "==> Installing prerequisites..."
apt-get install -y \
  ca-certificates \
  curl \
  gnupg \
  lsb-release

echo "==> Adding Docker GPG key..."
install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg \
  | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
chmod a+r /etc/apt/keyrings/docker.gpg

echo "==> Adding Docker repository..."
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
  https://download.docker.com/linux/ubuntu \
  $(lsb_release -cs) stable" \
  | tee /etc/apt/sources.list.d/docker.list > /dev/null

echo "==> Installing Docker..."
apt-get update -y
apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

echo "==> Enabling and starting Docker service..."
systemctl enable docker
systemctl start docker

echo "==> Adding user '${TARGET_USER}' to docker group..."
usermod -aG docker "${TARGET_USER}"

echo "==> Creating application directory /opt/app..."
mkdir -p /opt/app
chown "${TARGET_USER}:${TARGET_USER}" /opt/app

echo "==> Creating environment file template at /opt/app/production.env..."
cat > /opt/app/production.env <<'EOF'
# eShop Catalog — Production Environment Variables
# Fill in the values below before deploying.

SPRING_PROFILES_ACTIVE=prod

# Database connection
DB_URL=jdbc:sqlserver://${DB_HOST}:1433;databaseName=${DB_NAME};encrypt=true;trustServerCertificate=true
DB_USERNAME=${DB_USER}
DB_PASSWORD=${DB_PASSWORD}

# Server
SERVER_PORT=8080
EOF

chown "${TARGET_USER}:${TARGET_USER}" /opt/app/production.env
chmod 600 /opt/app/production.env

echo ""
echo "==> VM setup complete."
echo "    - Log out and back in for docker group membership to take effect."
echo "    - Edit /opt/app/production.env with your actual database credentials."
echo "    - Then run deploy.sh to start the application."
