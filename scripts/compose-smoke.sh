#!/usr/bin/env bash
# Smoke test for the containerized stack in mock-data mode (no database): brings the three hosts
# up, waits for their health checks and asserts the HTTP and gRPC surfaces answer.
#
#   scripts/compose-smoke.sh
#
# Set KEEP_UP=1 to leave the stack running afterwards. Set COMPOSE_BUILD=0 to run pre-built images
# instead of rebuilding them (CI loads the images produced by the docker-build job). grpcurl is
# optional: the gRPC assertions are skipped (not failed) when it is not installed.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

compose=(docker compose -f docker-compose.yml -f docker-compose.mock.yml)
services=(api grpc web)

# Unused in mock mode, but docker-compose.yml still interpolates it on every invocation.
export MSSQL_SA_PASSWORD="${MSSQL_SA_PASSWORD:-unused-in-mock-mode}"

web_port="${WEB_PORT:-8080}"
api_port="${API_PORT:-8081}"
grpc_port="${GRPC_PORT:-8082}"

cleanup() {
  if [ "${KEEP_UP:-0}" != "1" ]; then
    "${compose[@]}" down --remove-orphans >/dev/null 2>&1 || true
  fi
}
trap cleanup EXIT

fail() {
  echo "FAIL: $*" >&2
  "${compose[@]}" ps || true
  "${compose[@]}" logs --tail 50 || true
  exit 1
}

up_args=(-d)
if [ "${COMPOSE_BUILD:-1}" = "1" ]; then
  up_args+=(--build)
  echo "==> building and starting ${services[*]} in mock-data mode"
else
  echo "==> starting ${services[*]} in mock-data mode from the existing images"
fi

"${compose[@]}" up "${up_args[@]}" "${services[@]}"

echo "==> waiting for health checks"
deadline=$((SECONDS + 180))
while :; do
  unhealthy=0
  for service in "${services[@]}"; do
    container="$("${compose[@]}" ps -q "$service")"
    [ -n "$container" ] || fail "service $service has no container"
    status="$(docker inspect -f '{{.State.Health.Status}}' "$container")"
    [ "$status" = "healthy" ] || unhealthy=1
  done
  [ "$unhealthy" -eq 0 ] && break
  [ "$SECONDS" -lt "$deadline" ] || fail "services did not become healthy within 180s"
  sleep 3
done
"${compose[@]}" ps

echo "==> health endpoints"
curl --fail --silent "http://localhost:${web_port}/health" | grep -q '"status":"Healthy"' || fail "web /health"
curl --fail --silent "http://localhost:${api_port}/health" | grep -q '"status":"Healthy"' || fail "api /health"
curl --fail --silent --http2-prior-knowledge "http://localhost:${grpc_port}/health" | grep -q '"status":"Healthy"' || fail "grpc /health"

echo "==> catalog API"
curl --fail --silent "http://localhost:${api_port}/api/brands" | grep -q '"Brand":".NET"' || fail "GET /api/brands"
curl --fail --silent "http://localhost:${api_port}/api/brands/1" | grep -q '"Id":1' || fail "GET /api/brands/1"
curl --fail --silent "http://localhost:${api_port}/api/files" | grep -q '"Brand":"Azure"' || fail "GET /api/files"
[ "$(curl --silent --output /dev/null --write-out '%{http_code}' "http://localhost:${api_port}/api/brands/999")" = "404" ] \
  || fail "GET /api/brands/999 should be 404"
[ "$(curl --silent --output /dev/null --write-out '%{content_type}' "http://localhost:${api_port}/items/1/pic")" = "image/png" ] \
  || fail "GET /items/1/pic should be image/png"

echo "==> catalog UI"
[ "$(curl --silent --output /dev/null --write-out '%{http_code}' "http://localhost:${web_port}/")" = "200" ] || fail "GET /"

if command -v grpcurl >/dev/null 2>&1; then
  echo "==> catalog gRPC"
  grpcurl -plaintext "localhost:${grpc_port}" list | grep -q 'eshop.catalog.v1.Catalog' || fail "grpc reflection"
  grpcurl -plaintext -d '{"id":1}' "localhost:${grpc_port}" eshop.catalog.v1.Catalog/FindCatalogItem \
    | grep -q '.NET Bot Black Hoodie' || fail "grpc FindCatalogItem"
  grpcurl -plaintext "localhost:${grpc_port}" eshop.catalog.v1.Catalog/GetCatalogBrands \
    | grep -q '"brand": "Azure"' || fail "grpc GetCatalogBrands"
else
  echo "==> grpcurl not installed; skipping the gRPC assertions"
fi

echo "PASS: containerized stack answered on all three surfaces"
