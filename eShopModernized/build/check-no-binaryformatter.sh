#!/usr/bin/env bash
#
# NET-63 (risk R3): fail if BinaryFormatter / System.Runtime.Serialization.Formatters.Binary
# is reintroduced into the modernized solution.
#
# The primary guard is Microsoft.CodeAnalysis.BannedApiAnalyzers (RS0030, configured through
# BannedSymbols.txt + .editorconfig) which breaks the compile. This script is the grep-based
# fallback CI (NET-72) can call without building, and it also catches usages in project files
# and non-compiled sources.
#
# The match is textual and deliberately strict: any occurrence in a scanned file fails, including
# comments. Refer to the type indirectly in prose (e.g. "the runtime binary formatter").
#
# Usage: check-no-binaryformatter.sh [scan-root]   (default: the eShopModernized tree)
set -uo pipefail

SCAN_ROOT="${1:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)}"

if [[ ! -d "$SCAN_ROOT" ]]; then
  echo "check-no-binaryformatter: scan root '$SCAN_ROOT' is not a directory" >&2
  exit 2
fi

PATTERN='BinaryFormatter|System\.Runtime\.Serialization\.Formatters\.Binary'

matches=$(grep -rInE --binary-files=without-match \
  --include='*.cs' --include='*.cshtml' --include='*.razor' --include='*.vb' \
  --include='*.csproj' --include='*.props' --include='*.targets' --include='*.sln' \
  --exclude-dir=bin --exclude-dir=obj --exclude-dir=.git \
  "$PATTERN" "$SCAN_ROOT")

if [[ -n "$matches" ]]; then
  echo "check-no-binaryformatter: FAILED — BinaryFormatter usage found under '$SCAN_ROOT':" >&2
  echo "$matches" >&2
  echo "Use eShop.Shared.Serialization.JsonSerializing (System.Text.Json) instead — see NET-63." >&2
  exit 1
fi

echo "check-no-binaryformatter: OK — no BinaryFormatter usage under '$SCAN_ROOT'."
exit 0
