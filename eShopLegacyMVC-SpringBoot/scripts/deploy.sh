#!/bin/bash
set -euo pipefail

###############################################################################
# deploy.sh — Pull and run the eShop Catalog container from GHCR.
#
# Usage:
#   ./deploy.sh [IMAGE_TAG]
#
# Arguments:
#   IMAGE_TAG   Docker image tag to deploy (default: latest)
#
# Environment (optional overrides):
#   GHCR_IMAGE  Full image path (default: ghcr.io/cognition-partner-workshops/eshopmodernizing)
#   ENV_FILE    Path to environment file (default: /opt/app/production.env)
#   APP_PORT    Host port to bind (default: 8080)
#
# Examples:
#   ./deploy.sh                 # Deploy latest
#   ./deploy.sh v1.2.3          # Deploy specific tag
#   GHCR_IMAGE=ghcr.io/myorg/myapp ./deploy.sh main  # Custom image
###############################################################################

IMAGE_TAG="${1:-latest}"
GHCR_IMAGE="${GHCR_IMAGE:-ghcr.io/cognition-partner-workshops/eshopmodernizing}"
ENV_FILE="${ENV_FILE:-/opt/app/production.env}"
APP_PORT="${APP_PORT:-8080}"
CONTAINER_NAME="eshop-catalog"
HEALTH_URL="http://localhost:${APP_PORT}/actuator/health"
HEALTH_RETRIES=30
HEALTH_INTERVAL=5

FULL_IMAGE="${GHCR_IMAGE}:${IMAGE_TAG}"

usage() {
  echo "Usage: $0 [IMAGE_TAG]"
  echo ""
  echo "Deploy the eShop Catalog container from GHCR."
  echo ""
  echo "Arguments:"
  echo "  IMAGE_TAG   Docker image tag to deploy (default: latest)"
  echo ""
  echo "Environment variables:"
  echo "  GHCR_IMAGE  Full image path (default: ${GHCR_IMAGE})"
  echo "  ENV_FILE    Path to env file (default: ${ENV_FILE})"
  echo "  APP_PORT    Host port (default: ${APP_PORT})"
  echo ""
  echo "Examples:"
  echo "  $0              # Deploy latest"
  echo "  $0 v1.2.3       # Deploy a specific version"
}

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
  usage
  exit 0
fi

# Validate environment file exists
if [[ ! -f "${ENV_FILE}" ]]; then
  echo "ERROR: Environment file not found: ${ENV_FILE}"
  echo "Run setup-vm.sh first, then edit /opt/app/production.env with your settings."
  exit 1
fi

echo "==> Pulling image: ${FULL_IMAGE}"
docker pull "${FULL_IMAGE}"

echo "==> Stopping existing container (if running)..."
if docker ps -aq -f name="^${CONTAINER_NAME}$" | grep -q .; then
  docker stop "${CONTAINER_NAME}" 2>/dev/null || true
  docker rm "${CONTAINER_NAME}" 2>/dev/null || true
  echo "    Removed existing container."
fi

echo "==> Starting container: ${CONTAINER_NAME}"
docker run -d \
  --name "${CONTAINER_NAME}" \
  --env-file "${ENV_FILE}" \
  -p "${APP_PORT}:8080" \
  --restart unless-stopped \
  --log-opt max-size=10m \
  --log-opt max-file=3 \
  "${FULL_IMAGE}"

echo "==> Waiting for health check at ${HEALTH_URL}..."
for i in $(seq 1 ${HEALTH_RETRIES}); do
  if curl -sf "${HEALTH_URL}" > /dev/null 2>&1; then
    echo "    Health check passed (attempt ${i}/${HEALTH_RETRIES})."
    echo ""
    echo "==> Deployment successful!"
    echo "    Image:     ${FULL_IMAGE}"
    echo "    Container: ${CONTAINER_NAME}"
    echo "    URL:       http://localhost:${APP_PORT}"
    echo "    Health:    ${HEALTH_URL}"
    exit 0
  fi
  echo "    Waiting... (${i}/${HEALTH_RETRIES})"
  sleep "${HEALTH_INTERVAL}"
done

echo ""
echo "ERROR: Health check did not pass after $((HEALTH_RETRIES * HEALTH_INTERVAL)) seconds."
echo "Check container logs: docker logs ${CONTAINER_NAME}"
exit 1
