#!/usr/bin/env bash
# NET-63 (risk R3): fail if the modernized solution reintroduces BinaryFormatter or another
# legacy runtime formatter. Complements the BannedApiAnalyzers build gate (RS0030) by also
# catching references in non-compiled files (props, targets, config, docs snippets).
#
# Usage: scripts/check-no-binaryformatter.sh [path ...]   (defaults to src/ and tests/)
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

paths=("$@")
if [ ${#paths[@]} -eq 0 ]; then
  paths=(src tests)
fi

pattern='BinaryFormatter|SoapFormatter|NetDataContractSerializer'

matcher() {
  if command -v rg >/dev/null 2>&1; then
    rg --no-heading --line-number --color never "$pattern" "${paths[@]}"
  else
    grep -rEn "$pattern" "${paths[@]}"
  fi
}

matches="$(matcher || true)"

if [ -n "$matches" ]; then
  echo "ERROR: banned serializer usage found in the modernized solution:" >&2
  echo "$matches" >&2
  echo >&2
  echo "Use eShop.Shared.Serialization.JsonSerialization (System.Text.Json) instead." >&2
  exit 1
fi

echo "OK: no BinaryFormatter/SoapFormatter/NetDataContractSerializer usage under ${paths[*]}."
