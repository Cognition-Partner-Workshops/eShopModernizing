#!/usr/bin/env bash
# Parity gate (NET-73): replays every golden output recorded in the Confluence page
# ".NET Behavioral Baseline" (space NET, page 58687519, section 6) against the modernized stack and
# prints a per-endpoint verdict. Exits non-zero when any check fails.
#
#   scripts/parity-gate.sh                        # builds and starts the mock-data compose stack
#   scripts/parity-gate.sh --no-compose           # runs against an already-running stack
#   scripts/parity-gate.sh --report modernization/parity-report.md
#
# Environment: WEB_PORT (8080), API_PORT (8081), GRPC_PORT (8082), WEB_BASE_URL, API_BASE_URL,
# GRPC_ADDRESS, KEEP_UP=1 to leave a self-started stack running.
#
# The gate mutates catalog state (the baseline CRUD round-trip deletes item 1), so it needs a fresh
# stack: with --no-compose, restart the hosts before re-running.
set -uo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

use_compose=1
report_path=""
while [ $# -gt 0 ]; do
  case "$1" in
    --no-compose) use_compose=0 ;;
    --report) report_path="$2"; shift ;;
    -h|--help) sed -n '2,14p' "$0"; exit 0 ;;
    *) echo "unknown argument: $1" >&2; exit 2 ;;
  esac
  shift
done

web_port="${WEB_PORT:-8080}"
api_port="${API_PORT:-8081}"
grpc_port="${GRPC_PORT:-8082}"
WEB="${WEB_BASE_URL:-http://localhost:${web_port}}"
API="${API_BASE_URL:-http://localhost:${api_port}}"
GRPC="${GRPC_ADDRESS:-localhost:${grpc_port}}"

compose=(docker compose -f docker-compose.yml -f docker-compose.mock.yml)
services=(api grpc web)

cleanup() {
  if [ "$use_compose" = "1" ] && [ "${KEEP_UP:-0}" != "1" ]; then
    "${compose[@]}" down --remove-orphans >/dev/null 2>&1 || true
  fi
}
trap cleanup EXIT

if [ "$use_compose" = "1" ]; then
  echo "==> starting the mock-data stack (${services[*]})"
  MSSQL_SA_PASSWORD="${MSSQL_SA_PASSWORD:-unused-in-mock-mode}" \
    "${compose[@]}" up -d --build "${services[@]}" >/dev/null
  deadline=$((SECONDS + 180))
  while :; do
    unhealthy=0
    for service in "${services[@]}"; do
      container="$(MSSQL_SA_PASSWORD=unused-in-mock-mode "${compose[@]}" ps -q "$service")"
      [ -n "$container" ] || unhealthy=1
      [ -n "$container" ] && [ "$(docker inspect -f '{{.State.Health.Status}}' "$container")" = "healthy" ] || unhealthy=1
    done
    [ "$unhealthy" -eq 0 ] && break
    [ "$SECONDS" -lt "$deadline" ] && sleep 3 || { echo "services did not become healthy" >&2; exit 1; }
  done
fi

if ! command -v grpcurl >/dev/null 2>&1; then
  echo "grpcurl is required for the gRPC section (https://github.com/fullstorydev/grpcurl)" >&2
  exit 1
fi

pass=0; fail=0; accepted=0
rows=()          # markdown rows: surface|request|baseline|modernized|verdict
section=""

record() { # surface request baseline observed verdict
  rows+=("$1|$2|$3|$4|$5")
}

check() { # request baseline-expectation observed-value expected-value [verdict-kind]
  local request="$1" baseline="$2" observed="$3" expected="$4" kind="${5:-Yes}"
  if [ "$observed" = "$expected" ]; then
    if [ "$kind" = "Yes" ]; then pass=$((pass + 1)); else accepted=$((accepted + 1)); fi
    printf '  [%-12s] %-52s %s\n' "$( [ "$kind" = Yes ] && echo PASS || echo ACCEPTED )" "$request" "$observed"
    record "$section" "$request" "$baseline" "$observed" "$( [ "$kind" = Yes ] && echo Yes || echo "Intentional change" )"
  else
    fail=$((fail + 1))
    printf '  [%-12s] %-52s got %s, want %s\n' FAIL "$request" "$observed" "$expected"
    record "$section" "$request" "$baseline" "$observed (expected $expected)" "**No**"
  fi
}

status() { curl -s -o /tmp/parity-body -w '%{http_code}' "$@"; }
status_ctype() { curl -s -o /tmp/parity-body -w '%{http_code} %{content_type}' "$@" | sed 's/;.*//'; }
body() { curl -s "$@"; }

echo
echo "=== 1. MVC UI (baseline section 6.1, eShopLegacyMVC CatalogController) — ${WEB}"
section="MVC UI"
check "GET /" "200 text/html, 10 rows" "$(status_ctype "$WEB/")" "200 text/html"
check "GET /Catalog" "200, identical to /" "$(status_ctype "$WEB/Catalog")" "200 text/html"
check "GET /Catalog/Index" "200, byte-identical to /Catalog" \
  "$([ "$(body "$WEB/Catalog" | md5sum)" = "$(body "$WEB/Catalog/Index" | md5sum)" ] && echo identical || echo different)" "identical"
check "GET /Catalog (row count)" "10 catalog rows" "$(body "$WEB/Catalog" | grep -c 'items/[0-9]*/pic')" "10"
check "GET /Catalog (title)" "<title>Index - Catalog manager (MVC)</title>" \
  "$(body "$WEB/Catalog" | grep -o '<title>[^<]*</title>')" "<title>Index - Catalog manager (MVC)</title>"
check "GET /Catalog/Index?pageSize=2&pageIndex=1" "200, 2 rows (second page)" \
  "$(status "$WEB/Catalog/Index?pageSize=2&pageIndex=1") $(grep -c 'items/[0-9]*/pic' /tmp/parity-body)" "200 2"
check "GET /Catalog/Details/1" "200 detail view" "$(status "$WEB/Catalog/Details/1")" "200"
check "GET /Catalog/Details/999" "404 unknown id" "$(status "$WEB/Catalog/Details/999")" "404"
check "GET /Catalog/Details (no id)" "400 BadRequest" "$(status "$WEB/Catalog/Details")" "400"
check "GET /Catalog/Create" "200 form with __RequestVerificationToken" \
  "$(status "$WEB/Catalog/Create") $(grep -c '__RequestVerificationToken' /tmp/parity-body)" "200 1"
check "GET /Catalog/Edit/1" "200 prefilled form" "$(status "$WEB/Catalog/Edit/1")" "200"
check "GET /Catalog/Edit (no id)" "400 BadRequest" "$(status "$WEB/Catalog/Edit")" "400"
check "GET /Catalog/Delete/1" "200 confirmation view" "$(status "$WEB/Catalog/Delete/1")" "200"
check "GET /Catalog/Delete (no id)" "400 BadRequest" "$(status "$WEB/Catalog/Delete")" "400"
check "GET /nope" "404 not found" "$(status "$WEB/nope")" "404"
check "GET /Content/site.css" "404 (IIS served static files)" \
  "$(status "$WEB/Content/site.css") -> $(status "$WEB/css/site.css")" "404 -> 200" "accepted"
check "GET /Default (retired Web Forms route)" "200 Default.aspx (Web Forms)" \
  "$(status "$WEB/Default")" "301" "accepted"
check "GET /Default/index/1/size/2 (retired)" "200 paginated Default.aspx" \
  "$(status "$WEB/Default/index/1/size/2")" "301" "accepted"

echo
echo "=== 2. MVC write path (baseline section 6.2 CRUD round-trip)"
section="MVC write path"
jar="$(mktemp)"
token() { body -c "$jar" "$1" | grep -o 'name="__RequestVerificationToken"[^>]*value="[^"]*"' | sed 's/.*value="//;s/"//'; }
post_status() { curl -s -b "$jar" -o /dev/null -w '%{http_code} %{redirect_url}' -X POST "$@"; }

: >"$jar"; tok="$(token "$WEB/Catalog/Create")"
check "POST /Catalog/Create (with token)" "302 Location: /" \
  "$(post_status "$WEB/Catalog/Create" --data-urlencode "__RequestVerificationToken=$tok" \
      -d 'Name=Smoke Test Item&Description=Smoke&Price=9.99&PictureFileName=1.png&CatalogTypeId=1&CatalogBrandId=1&AvailableStock=1&RestockThreshold=1&MaxStockThreshold=2')" \
  "302 ${WEB}/"
check "  → created item visible in the index" "new item listed" \
  "$(body "$WEB/Catalog/Index?pageSize=50" | grep -c 'Smoke Test Item')" "1"
check "POST /Catalog/Create (no anti-forgery cookie)" "500 HttpAntiForgeryException" \
  "$(curl -s -o /dev/null -w '%{http_code}' -X POST "$WEB/Catalog/Create" -d 'Name=x&Description=y&Price=1&CatalogTypeId=1&CatalogBrandId=1')" \
  "400" "accepted"

: >"$jar"; tok="$(token "$WEB/Catalog/Edit/1")"
check "POST /Catalog/Edit (id=1, Renamed Hoodie)" "302 Location: /" \
  "$(post_status "$WEB/Catalog/Edit" --data-urlencode "__RequestVerificationToken=$tok" \
      -d 'Id=1&Name=Renamed Hoodie&Description=d&Price=19.5&PictureFileName=1.png&CatalogTypeId=2&CatalogBrandId=2&AvailableStock=100&RestockThreshold=10&MaxStockThreshold=200')" \
  "302 ${WEB}/"
check "  → GET /Catalog/Details/1 renders the new name" "Renamed Hoodie" \
  "$(body "$WEB/Catalog/Details/1" | grep -c 'Renamed Hoodie')" "1"

: >"$jar"; tok="$(token "$WEB/Catalog/Delete/1")"
check "POST /Catalog/Delete (id=1)" "302 Location: /" \
  "$(post_status "$WEB/Catalog/Delete" --data-urlencode "__RequestVerificationToken=$tok" -d 'id=1')" "302 ${WEB}/"
check "  → GET /Catalog/Details/1 afterwards" "404" "$(status "$WEB/Catalog/Details/1")" "404"
rm -f "$jar"

echo
echo "=== 3. HTTP API (baseline section 6.1 Web API 2 surface) — ${API}"
section="HTTP API"
brands_json='[{"Id":1,"Brand":"Azure"},{"Id":2,"Brand":".NET"},{"Id":3,"Brand":"Visual Studio"},{"Id":4,"Brand":"SQL Server"},{"Id":5,"Brand":"Other"}]'
check "GET /api/brands" "200 application/json, 5 brands" "$(status_ctype "$API/api/brands")" "200 application/json"
check "  → body" "$brands_json" "$(body "$API/api/brands")" "$brands_json"
check "GET /api/brands/1" '200 {"Id":1,"Brand":"Azure"}' "$(status "$API/api/brands/1") $(body "$API/api/brands/1")" \
  '200 {"Id":1,"Brand":"Azure"}'
check "GET /api/brands/99" "404 empty body" "$(status "$API/api/brands/99") [$(cat /tmp/parity-body)]" "404 []"
check "DELETE /api/brands/1" "200 (no-op demo action)" "$(status -X DELETE "$API/api/brands/1")" "405" "accepted"
check "GET /api/files" "200 BinaryFormatter payload (719 bytes, text/html)" \
  "$(status_ctype "$API/api/files") $(body "$API/api/files")" "200 application/json $brands_json" "accepted"
check "GET /items/1/pic" "200 image/png, 151640 bytes" \
  "$(status_ctype "$API/items/1/pic") $(stat -c %s /tmp/parity-body)" "200 image/png 151640"
check "GET /items/0/pic" "400 non-positive id" "$(status "$API/items/0/pic")" "400"
check "GET /items/999/pic" "404 absent picture" "$(status "$API/items/999/pic")" "404"
check "GET /api" "404 (unreachable dead route)" "$(status "$API/api")" "404"
check "GET /swagger/index.html" "n/a (no OpenAPI in the legacy app)" "$(status "$API/swagger/index.html")" "200" "accepted"

echo
echo "=== 4. gRPC — WCF ICatalogService equivalents (baseline section 2.1, static-only) — ${GRPC}"
section="gRPC (WCF equivalent)"
g() { grpcurl -plaintext -d "$2" "$GRPC" "eshop.catalog.v1.Catalog/$1" 2>&1; }
gcode() { g "$1" "$2" | sed -n 's/^ *Code: //p'; }

check "reflection: eshop.catalog.v1.Catalog operations" "10 SOAP [OperationContract] members" \
  "$(grpcurl -plaintext "$GRPC" list eshop.catalog.v1.Catalog | wc -l)" "10"
check "GetCatalogBrands" "5 brands, Azure/.NET/Visual Studio/SQL Server/Other" \
  "$(g GetCatalogBrands '{}' | grep -c '"brand"')" "5"
check "GetCatalogTypes" "4 types (Mug, T-Shirt, Sheet, USB Memory Stick)" \
  "$(g GetCatalogTypes '{}' | grep -c '"type":')" "4"
check "FindCatalogItem(1)" "item 1 = .NET Bot Black Hoodie, 19.50" \
  "$(g FindCatalogItem '{"id":1}' | tr -d ' \n' | grep -o '"name":"[^"]*"'),$(g FindCatalogItem '{"id":1}' | tr -d ' \n' | grep -o '"units":"19","nanos":500000000')" \
  '"name":".NETBotBlackHoodie","units":"19","nanos":500000000'
check "FindCatalogItem(999)" "null" "$(gcode FindCatalogItem '{"id":999}')" "NotFound" "accepted"
check "GetCatalogItems(0,0)" "0 = no filter → all 12 seeded items" "$(g GetCatalogItems '{}' | grep -c '"picture_filename"')" "12"
check "GetCatalogItems(2,0)" "brand filter → 6 .NET items" \
  "$(g GetCatalogItems '{"brand_id_filter":2}' | grep -c '"picture_filename"')" "6"
check "GetCatalogItems(2,2)" "brand+type filter → 3 items" \
  "$(g GetCatalogItems '{"brand_id_filter":2,"type_id_filter":2}' | grep -c '"picture_filename"')" "3"
check "GetAvailableStock(2017-09-20, 1)" "100 (seeded stock row)" \
  "$(g GetAvailableStock '{"date":"2017-09-20T00:00:00Z","catalog_item_id":1}' | tr -d ' \n')" \
  '{"available_stock":100}'
check "GetAvailableStock(no stock row)" "0" \
  "$(g GetAvailableStock '{"date":"2001-01-01T00:00:00Z","catalog_item_id":1}' | tr -d ' \n')" '{}'
check "GetDiscount(2017-09-20)" "discount 1, size 0.3f" \
  "$(g GetDiscount '{"day":"2017-09-20T00:00:00Z"}' | tr -d ' \n' | grep -o '"size":0.30000001192092896.*"id":1')" \
  '"size":0.30000001192092896,"start":"2017-09-18T00:00:00Z","end":"2017-09-21T00:00:00Z","id":1'
check "GetDiscount(no discount that day)" "null" "$(gcode GetDiscount '{"day":"2001-01-01T00:00:00Z"}')" "NotFound" "accepted"
check "CreateAvailableStock then GetAvailableStock" "stock row upserted for the date" \
  "$(g CreateAvailableStock '{"catalog_items_stock":{"catalog_item_id":3,"available_stock":42,"date":"2017-10-01T00:00:00Z"}}' | tr -d ' \n')$(g GetAvailableStock '{"date":"2017-10-01T00:00:00Z","catalog_item_id":3}' | tr -d ' \n')" \
  '{}{"available_stock":42}'
check "CreateCatalogItem" "item added (void)" \
  "$(g CreateCatalogItem '{"catalog_item":{"name":"Parity Item","description":"parity","price":{"units":"5","nanos":0},"picture_filename":"1.png","catalog_type_id":1,"catalog_brand_id":1}}' | tr -d ' \n')" '{}'
new_id="$(g GetCatalogItems '{}' | grep -B2 '"name": "Parity Item"' | sed -n 's/.*"id": \([0-9]*\).*/\1/p' | head -1)"
check "UpdateCatalogItem" "item updated (void)" \
  "$(g UpdateCatalogItem "{\"catalog_item\":{\"id\":$new_id,\"name\":\"Parity Item Renamed\",\"description\":\"parity\",\"price\":{\"units\":\"6\",\"nanos\":0},\"picture_filename\":\"1.png\",\"catalog_type_id\":1,\"catalog_brand_id\":1}}" | tr -d ' \n')$(g FindCatalogItem "{\"id\":$new_id}" | grep -c 'Parity Item Renamed')" '{}1'
check "RemoveCatalogItem" "item removed (void)" \
  "$(g RemoveCatalogItem "{\"catalog_item\":{\"id\":$new_id}}" | tr -d ' \n')$(gcode FindCatalogItem "{\"id\":$new_id}")" '{}NotFound'
check "RemoveCatalogItem(unknown id)" "opaque FaultException" "$(gcode RemoveCatalogItem '{"catalog_item":{"id":987654}}')" "NotFound" "accepted"
check "FindCatalogItem(-1)" "undefined (empty or fault)" "$(gcode FindCatalogItem '{"id":-1}')" "InvalidArgument" "accepted"

echo
echo "=== 5. Health / observability (added in NET-62, no legacy counterpart)"
section="Operations"
check "GET /health (web, api)" "n/a" "$(status "$WEB/health") $(status "$API/health")" "200 200" "accepted"
check "GET /health (grpc, h2c)" "n/a" \
  "$(curl -s --http2-prior-knowledge -o /dev/null -w '%{http_code}' "http://${GRPC}/health")" "200" "accepted"
check "GET /ready (web, api)" "n/a" "$(status "$WEB/ready") $(status "$API/ready")" "200 200" "accepted"

echo
echo "======================================================================"
printf 'parity gate: %d matched, %d intentional/accepted differences, %d mismatches\n' "$pass" "$accepted" "$fail"

if [ -n "$report_path" ]; then
  {
    echo "| Surface | Endpoint / operation | Legacy (baseline) | Modernized | Match? |"
    echo "| --- | --- | --- | --- | --- |"
    for row in "${rows[@]}"; do
      IFS='|' read -r s r b o v <<<"$row"
      printf '| %s | `%s` | %s | %s | %s |\n' "$s" "$r" "$b" "$o" "$v"
    done
  } >"$report_path"
  echo "table written to $report_path"
fi

[ "$fail" -eq 0 ] || exit 1
