#!/usr/bin/env bash
#
# NET-73 parity gate — replays every row of the ".NET Behavioral Baseline" section 6 golden
# outputs against the containerized modernized stack and prints a per-row verdict.
#
#   cd eShopModernized
#   docker compose down -v && docker compose up -d --wait
#   ./tests/parity/replay-golden-baseline.sh
#
# Requires curl and grpcurl. The script mutates the catalog database (section 6.2 is a CRUD
# round-trip), so run it against a freshly seeded stack and drop the volume afterwards.
#
# Exit code 0 when every check matches, 1 otherwise.

set -u

WEB=${WEB:-http://localhost:8080}
API=${API:-http://localhost:8081}
GRPC=${GRPC:-localhost:8082}
SVC=eshop.catalog.v1.CatalogService

pass=0
fail=0
tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT

# check <verdict-label> <expected> <actual> <description>
check() {
  if [ "$2" = "$3" ]; then
    printf 'PASS  %-22s %-58s %s\n' "$1" "$4" "$3"
    pass=$((pass + 1))
  else
    printf 'FAIL  %-22s %-58s expected %s, got %s\n' "$1" "$4" "$2" "$3"
    fail=$((fail + 1))
  fi
}

status() { curl -s -o "$tmp/body" -w '%{http_code}' "$@"; }
ctype() { curl -s -o /dev/null -w '%{content_type}' "$@"; }
body() { curl -s "$@"; }
location() { curl -s -o /dev/null -D - "$1" | grep -i '^location:' | tr -d '\r' | sed 's/^[Ll]ocation: //'; }

# yes/no rather than an occurrence count: a product name shows up in several cells and in the
# thumbnail alt attribute, so counting lines is not a stable assertion.
contains() { if body "$2" | grep -qF "$1"; then echo yes; else echo no; fi; }

section() { printf '\n== %s\n' "$1"; }

# --------------------------------------------------------------------------------------------
section 'Preflight — health and seeding'

check Yes 200 "$(status "$WEB/health")" 'GET /health (web)'
check Yes 200 "$(status "$API/health")" 'GET /health (api)'
check Yes 200 "$(status "$API/ready")" 'GET /ready (api, checks the database)'
check Yes Healthy "$(curl -s --http2-prior-knowledge "$GRPC/health" 2>/dev/null || \
  curl -s --http2-prior-knowledge "http://$GRPC/health")" 'GET /health (grpc, h2c)'
check Yes 12 "$(body "$WEB/Catalog/Index?pageSize=50" | grep -c 'esh-thumbnail')" 'catalog seeded with 12 items'
check Yes yes "$(contains 'Prism White TShirt' "$WEB/Catalog/Index?pageSize=50")" 'C-04 seed set: item 12 is "Prism White TShirt"'
check Yes 200 "$(status "$WEB/items/1/pic")" 'C-04 seed set: item 1 has a picture (1.png)'

# --------------------------------------------------------------------------------------------
section 'Baseline 6.1 — MVC UI (eShop.Web)'

for url in /Catalog /Catalog/Index; do
  check Yes 200 "$(status "$WEB$url")" "GET $url -> 200"
  check Yes 10 "$(grep -c 'esh-thumbnail' "$tmp/body")" "GET $url renders 10 rows"
  check Yes yes "$(grep -qF '<title>Index - Catalog manager (MVC)</title>' "$tmp/body" && echo yes || echo no)" "GET $url title"
done
check Yes "$(body "$WEB/Catalog" | md5sum)" "$(body "$WEB/Catalog/Index" | md5sum)" '/Catalog and /Catalog/Index are identical'
check Yes "$(body "$WEB/Catalog" | md5sum)" "$(body "$WEB/" | md5sum)" '/ serves the catalog list too'

check Yes 200 "$(status "$WEB/Catalog/Index?pageSize=2&pageIndex=1")" 'GET /Catalog/Index?pageSize=2&pageIndex=1 -> 200'
check Yes 2 "$(grep -c 'esh-thumbnail' "$tmp/body")" 'pagination honoured (2 rows, second page)'
check Yes yes "$(grep -qF 'Page 2 - 6' "$tmp/body" && echo yes || echo no)" 'pagination reports page 2 of 6'

check Yes 200 "$(status "$WEB/Catalog/Details/1")" 'GET /Catalog/Details/1 -> 200'
check Yes 404 "$(status "$WEB/Catalog/Details/999")" 'GET /Catalog/Details/999 -> 404'
check Yes 400 "$(status "$WEB/Catalog/Details")" 'GET /Catalog/Details (no id) -> 400'
check Yes 200 "$(status "$WEB/Catalog/Create")" 'GET /Catalog/Create -> 200'
check Yes yes "$(grep -qF '__RequestVerificationToken' "$tmp/body" && echo yes || echo no)" '/Catalog/Create carries the anti-forgery field'
check Yes 200 "$(status "$WEB/Catalog/Edit/1")" 'GET /Catalog/Edit/1 -> 200'
check Yes yes "$(grep -qF 'value=".NET Bot Black Hoodie"' "$tmp/body" && echo yes || echo no)" '/Catalog/Edit/1 is prefilled'
check Yes 200 "$(status "$WEB/Catalog/Delete/1")" 'GET /Catalog/Delete/1 -> 200'

check Yes 200 "$(status "$WEB/items/1/pic")" 'GET /items/1/pic -> 200'
check Yes image/png "$(ctype "$WEB/items/1/pic")" 'GET /items/1/pic content type'
check Yes 151640 "$(curl -s -o /dev/null -w '%{size_download}' "$WEB/items/1/pic")" 'GET /items/1/pic byte count'
check Yes 400 "$(status "$WEB/items/0/pic")" 'GET /items/0/pic -> 400'
check Yes 404 "$(status "$WEB/nope")" 'GET /nope -> 404'
check 'Intentional change' 200 "$(status "$WEB/css/site.css")" 'GET /css/site.css -> 200 (golden 404 was a harness artefact)'

# --------------------------------------------------------------------------------------------
section 'Baseline 6.1 — HTTP API (eShop.Catalog.Api)'

check Yes 200 "$(status "$API/api/brands")" 'GET /api/brands -> 200'
check Yes '[{"Id":1,"Brand":"Azure"},{"Id":2,"Brand":".NET"},{"Id":3,"Brand":"Visual Studio"},{"Id":4,"Brand":"SQL Server"},{"Id":5,"Brand":"Other"}]' \
  "$(body "$API/api/brands")" 'GET /api/brands body'
check Yes '{"Id":1,"Brand":"Azure"}' "$(body "$API/api/brands/1")" 'GET /api/brands/1 body'
check Yes 404 "$(status "$API/api/brands/99")" 'GET /api/brands/99 -> 404'
check Yes 0 "$(curl -s -o /dev/null -w '%{size_download}' "$API/api/brands/99")" 'GET /api/brands/99 has an empty body'
check Yes 404 "$(status "$API/api")" 'GET /api -> 404 (dead route deleted)'
check 'Intentional change' 200 "$(status "$API/api/files")" 'GET /api/files -> 200'
check 'Intentional change' 'application/json; charset=utf-8' "$(ctype "$API/api/files")" 'GET /api/files is JSON, not a BinaryFormatter stream'
check 'Intentional change' "$(body "$API/api/brands")" "$(body "$API/api/files")" 'GET /api/files returns the BrandDTO[] payload'
check 'Intentional change' 405 "$(status -X DELETE "$API/api/brands/1")" 'DELETE /api/brands/1 -> 405 (legacy no-op removed)'
check Yes 200 "$(status "$API/items/1/pic")" 'GET /items/1/pic -> 200 (API copy of the route)'
check Yes 400 "$(status "$API/items/0/pic")" 'GET /items/0/pic -> 400 (API copy of the route)'

# --------------------------------------------------------------------------------------------
section 'NET-70 §4.3 — retired Web Forms pager URLs'

check Yes 301 "$(status "$WEB/Default/index/1/size/2")" 'GET /Default/index/1/size/2 -> 301'
check Yes '/Catalog/Index?pageIndex=1&pageSize=2' "$(location "$WEB/Default/index/1/size/2")" 'pager redirect target'
check Yes 301 "$(status "$WEB/Default")" 'GET /Default -> 301'
check Yes '/' "$(location "$WEB/Default")" '/Default redirect target'
check Yes 404 "$(status "$WEB/Default/index/abc/size/xyz")" 'non-numeric pager segments fall through to 404'

# --------------------------------------------------------------------------------------------
section 'Baseline 6.3 — gRPC contract (no golden output; contract-verified)'

grpcurl -plaintext "$GRPC" list "$SVC" > "$tmp/rpcs" 2>&1
for rpc in FindCatalogItem GetCatalogBrands GetCatalogItems GetCatalogTypes GetAvailableStock \
           CreateAvailableStock CreateCatalogItem UpdateCatalogItem RemoveCatalogItem GetDiscount; do
  check 'Contract-verified' 1 "$(grep -c "$SVC.$rpc\$" "$tmp/rpcs")" "reflection exposes $rpc"
done

check 'Contract-verified' '.NET Bot Black Hoodie' \
  "$(grpcurl -plaintext -d '{"id":1}' "$GRPC" "$SVC/FindCatalogItem" | grep -o '"name": "[^"]*"' | sed 's/.*: "//; s/"//')" \
  'FindCatalogItem(1) returns the item'
check 'Contract-verified' 'NotFound' \
  "$(grpcurl -plaintext -d '{"id":999}' "$GRPC" "$SVC/FindCatalogItem" 2>&1 | grep -o 'NotFound')" \
  'FindCatalogItem(999) maps the legacy null to NOT_FOUND'
check 'Contract-verified' 5 \
  "$(grpcurl -plaintext -d '{}' "$GRPC" "$SVC/GetCatalogBrands" | grep -c '"brand":')" 'GetCatalogBrands returns 5 brands'
check 'Contract-verified' 4 \
  "$(grpcurl -plaintext -d '{}' "$GRPC" "$SVC/GetCatalogTypes" | grep -c '"type":')" 'GetCatalogTypes returns 4 types'
check 'Contract-verified' 12 \
  "$(grpcurl -plaintext -d '{"brandIdFilter":0,"typeIdFilter":0}' "$GRPC" "$SVC/GetCatalogItems" | grep -c '"picture_file_name"')" \
  'GetCatalogItems(0,0) returns all 12 items (0 = no filter)'
check 'Contract-verified' 0 \
  "$(grpcurl -plaintext -d '{"brandIdFilter":2,"typeIdFilter":0}' "$GRPC" "$SVC/GetCatalogItems" \
     | grep -c '"catalog_brand_id": [^2]')" \
  'GetCatalogItems(brand=2) returns only brand 2 items'
check 'Contract-verified' '{}' \
  "$(grpcurl -plaintext -d '{"date":"2026-08-05T00:00:00Z","catalogItemId":2}' "$GRPC" "$SVC/GetAvailableStock" | tr -d ' \n')" \
  'GetAvailableStock with no row answers 0 (legacy semantics)'
check 'Contract-verified' '{}' \
  "$(grpcurl -plaintext -d '{"date":"2026-08-05T00:00:00Z","catalogItemId":2,"availableStock":42}' "$GRPC" "$SVC/CreateAvailableStock" | tr -d ' \n')" \
  'CreateAvailableStock returns Empty'
check 'Contract-verified' 42 \
  "$(grpcurl -plaintext -d '{"date":"2026-08-05T00:00:00Z","catalogItemId":2}' "$GRPC" "$SVC/GetAvailableStock" | grep -o '[0-9]*$')" \
  'GetAvailableStock reads the shipment back'
check 'Contract-verified' 'NotFound' \
  "$(grpcurl -plaintext -d '{"day":"2026-08-05T00:00:00Z"}' "$GRPC" "$SVC/GetDiscount" 2>&1 | grep -o 'NotFound')" \
  'GetDiscount maps the legacy null to NOT_FOUND'

# The four RPCs C-08 records as never having had a consumer: exercised against the contract only.
check 'Contract-verified' '{}' \
  "$(grpcurl -plaintext -d '{"name":"gRPC Contract Item","description":"contract check","price":{"value":"3.25"},"pictureFileName":"1.png","catalogBrandId":1,"catalogTypeId":1}' "$GRPC" "$SVC/CreateCatalogItem" | tr -d ' \n')" \
  'C-08 CreateCatalogItem returns Empty'
check 'Contract-verified' '{}' \
  "$(grpcurl -plaintext -d '{"id":2,"name":"Mug (updated)","description":".NET Black & White Mug","price":{"value":"8.50"},"pictureFileName":"2.png","catalogBrandId":2,"catalogTypeId":1}' "$GRPC" "$SVC/UpdateCatalogItem" | tr -d ' \n')" \
  'C-08 UpdateCatalogItem returns Empty'
check 'Contract-verified' 'Mug (updated)' \
  "$(grpcurl -plaintext -d '{"id":2}' "$GRPC" "$SVC/FindCatalogItem" | grep -o '"name": "[^"]*"' | sed 's/.*: "//; s/"//')" \
  'C-08 UpdateCatalogItem is visible afterwards'
check 'Contract-verified' '{}' \
  "$(grpcurl -plaintext -d '{"id":11,"name":"Cup<T> Sheet","description":"Cup<T> Sheet","price":{"value":"8.50"},"pictureFileName":"11.png","catalogBrandId":5,"catalogTypeId":3}' "$GRPC" "$SVC/RemoveCatalogItem" | tr -d ' \n')" \
  'C-08 RemoveCatalogItem returns Empty'
check 'Contract-verified' 'NotFound' \
  "$(grpcurl -plaintext -d '{"id":11}' "$GRPC" "$SVC/FindCatalogItem" 2>&1 | grep -o 'NotFound')" \
  'C-08 RemoveCatalogItem is visible afterwards'

# --------------------------------------------------------------------------------------------
section 'Baseline 6.2 — write path (mutates the catalog)'

jar=$tmp/cookies
antiforgery() {
  curl -s -c "$jar" -b "$jar" "$1" \
    | grep -o 'name="__RequestVerificationToken"[^>]*value="[^"]*"' \
    | sed 's/.*value="//; s/"//'
}

t=$(antiforgery "$WEB/Catalog/Create")
code=$(curl -s -o /dev/null -D "$tmp/h" -w '%{http_code}' -b "$jar" -c "$jar" -X POST "$WEB/Catalog/Create" \
  --data-urlencode "__RequestVerificationToken=$t" --data-urlencode 'Name=Smoke Test Item' \
  --data-urlencode 'Description=Parity smoke test' --data-urlencode 'Price=9.99' \
  --data-urlencode 'PictureFileName=1.png' --data-urlencode 'CatalogBrandId=1' \
  --data-urlencode 'CatalogTypeId=1' --data-urlencode 'AvailableStock=10' \
  --data-urlencode 'RestockThreshold=1' --data-urlencode 'MaxStockThreshold=100')
check Yes 302 "$code" 'POST /Catalog/Create with a token -> 302'
check Yes '/' "$(grep -i '^location:' "$tmp/h" | tr -d '\r' | sed 's/^[Ll]ocation: //')" 'POST /Catalog/Create redirects to /'
check Yes yes "$(contains 'Smoke Test Item' "$WEB/Catalog/Index?pageSize=50")" 'the created item is visible afterwards'

code=$(curl -s -o /dev/null -w '%{http_code}' -X POST "$WEB/Catalog/Create" \
  --data-urlencode 'Name=Should Not Exist' --data-urlencode 'Price=1.00' \
  --data-urlencode 'CatalogBrandId=1' --data-urlencode 'CatalogTypeId=1')
check 'Intentional change' 400 "$code" 'POST without an anti-forgery token -> 400 (legacy 500)'
check Yes no "$(contains 'Should Not Exist' "$WEB/Catalog/Index?pageSize=50")" 'the untokenized write is refused'

t=$(antiforgery "$WEB/Catalog/Edit/1")
code=$(curl -s -o /dev/null -D "$tmp/h" -w '%{http_code}' -b "$jar" -c "$jar" -X POST "$WEB/Catalog/Edit" \
  --data-urlencode "__RequestVerificationToken=$t" --data-urlencode 'Id=1' \
  --data-urlencode 'Name=Renamed Hoodie' --data-urlencode 'Description=.NET Bot Black Hoodie' \
  --data-urlencode 'Price=19.50' --data-urlencode 'PictureFileName=1.png' \
  --data-urlencode 'CatalogBrandId=2' --data-urlencode 'CatalogTypeId=2' \
  --data-urlencode 'AvailableStock=100' --data-urlencode 'RestockThreshold=0' \
  --data-urlencode 'MaxStockThreshold=0')
check Yes 302 "$code" 'POST /Catalog/Edit -> 302'
check Yes '/' "$(grep -i '^location:' "$tmp/h" | tr -d '\r' | sed 's/^[Ll]ocation: //')" 'POST /Catalog/Edit redirects to /'
check Yes yes "$(contains 'Renamed Hoodie' "$WEB/Catalog/Details/1")" 'Details/1 renders the new name'

t=$(antiforgery "$WEB/Catalog/Delete/1")
code=$(curl -s -o /dev/null -D "$tmp/h" -w '%{http_code}' -b "$jar" -c "$jar" -X POST "$WEB/Catalog/Delete" \
  --data-urlencode "__RequestVerificationToken=$t" --data-urlencode 'id=1')
check Yes 302 "$code" 'POST /Catalog/Delete -> 302'
check Yes '/' "$(grep -i '^location:' "$tmp/h" | tr -d '\r' | sed 's/^[Ll]ocation: //')" 'POST /Catalog/Delete redirects to /'
check Yes 404 "$(status "$WEB/Catalog/Details/1")" 'Details/1 subsequently returns 404'

# --------------------------------------------------------------------------------------------
printf '\n%s\n' '-----------------------------------------------------------'
printf 'parity checks: %d passed, %d failed\n' "$pass" "$fail"
[ "$fail" -eq 0 ]
