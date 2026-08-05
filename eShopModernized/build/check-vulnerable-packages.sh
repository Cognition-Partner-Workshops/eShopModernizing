#!/usr/bin/env bash
#
# NET-72 (D-08): fail the build when any package in the modernized solution — direct or
# transitive — is covered by a NuGet security advisory.
#
# `dotnet list package --vulnerable` exits 0 even when it finds advisories, so the output has to be
# inspected. Known blind spot (C-12): the command reports nothing for packages.config projects, so
# the legacy Web Forms / WCF / WinForms projects are not covered by this gate at all.
#
# Usage: check-vulnerable-packages.sh [solution-or-project]   (default: eShopModernized/eShop.sln)
set -uo pipefail

TARGET="${1:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)/eShop.sln}"

output=$(dotnet list "$TARGET" package --vulnerable --include-transitive 2>&1)
status=$?

echo "$output"

if [[ $status -ne 0 ]]; then
  echo "check-vulnerable-packages: FAILED — 'dotnet list package' exited with $status." >&2
  exit "$status"
fi

if grep -q "has the following vulnerable packages" <<<"$output"; then
  echo "check-vulnerable-packages: FAILED — vulnerable packages found in '$TARGET'." >&2
  echo "Bump the offending package, or pin the transitive version in Directory.Packages.props." >&2
  exit 1
fi

echo "check-vulnerable-packages: OK — no vulnerable packages in '$TARGET'."
exit 0
