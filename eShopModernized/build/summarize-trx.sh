#!/usr/bin/env bash
#
# NET-72: render the VSTest .trx files produced by `dotnet test --logger trx` as a markdown table,
# so the run summary carries per-project test counts instead of only the raw artifact.
#
# Usage: summarize-trx.sh [results-directory]   (default: artifacts/test-results)
set -uo pipefail

RESULTS_DIR="${1:-artifacts/test-results}"

if [[ ! -d "$RESULTS_DIR" ]]; then
  echo "No test results found under '$RESULTS_DIR'."
  exit 0
fi

python3 - "$RESULTS_DIR" <<'PY'
import os
import sys
import xml.etree.ElementTree as ET

results_dir = sys.argv[1]
namespace = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
rows = []
totals = {"total": 0, "passed": 0, "failed": 0, "skipped": 0}

for name in sorted(os.listdir(results_dir)):
    if not name.endswith(".trx"):
        continue
    root = ET.parse(os.path.join(results_dir, name)).getroot()
    counters = root.find("t:ResultSummary/t:Counters", namespace)
    if counters is None:
        continue

    # The assembly under test is the storage path of any unit test entry in the run.
    storage = root.find("t:TestDefinitions/t:UnitTest", namespace)
    project = os.path.basename(storage.get("storage", name)) if storage is not None else name

    total = int(counters.get("total", 0))
    passed = int(counters.get("passed", 0))
    failed = int(counters.get("failed", 0))
    skipped = total - passed - failed
    rows.append((project, total, passed, failed, skipped))
    totals["total"] += total
    totals["passed"] += passed
    totals["failed"] += failed
    totals["skipped"] += skipped

if not rows:
    print(f"No .trx files found under `{results_dir}`.")
    sys.exit(0)

print("### Test results\n")
print("| Test project | Total | Passed | Failed | Skipped |")
print("| --- | ---: | ---: | ---: | ---: |")
for project, total, passed, failed, skipped in rows:
    print(f"| {project} | {total} | {passed} | {failed} | {skipped} |")
print(
    f"| **All** | **{totals['total']}** | **{totals['passed']}** "
    f"| **{totals['failed']}** | **{totals['skipped']}** |"
)
PY
