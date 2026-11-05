# Committed SOAP-versus-gRPC parity harness

This console calls the **real, unchanged production WCF service over SOAP 1.1**
and the real .NET 8 server over HTTP/2 gRPC. It does not instantiate either data
service, use a mock, or compare one implementation with itself. The only project
reference is the generated protobuf Contracts client. SOAP uses raw HttpClient
POSTs, quoted SOAPAction, XML escaping, alphabetical DataContract member order,
and the actual WCF operation, model, stock and SOAP namespaces.

## Run on Windows

From the solution directory (SDK 8 selected by `global.json`):

```powershell
dotnet build eShopModernizedWcfSolution.sln -c Release
dotnet format eShopModernizedWcfSolution.sln --verify-no-changes
dotnet test eShopModernizedWcfSolution.sln -c Release --no-build --logger "trx;LogFileName=modern-tests.trx" --results-directory C:\Users\Administrator\evidence\modern-tests
& ./tests/eShopWcf.Parity/run-parity.ps1 -Dataset Legacy
& ./tests/eShopWcf.Parity/run-parity.ps1 -Dataset Csv
```

The script builds the legacy project with **VS 2022 MSBuild**, and the modern
Release solution with .NET 8. `-SkipBuild` reuses already-verified Release binaries.
Parameters include `-LegacyPort` (5100), `-ModernPort` (5200), `-TimeoutSeconds`
(180), and `-ArtifactsRoot` (`C:\Users\Administrator\evidence\parity`). An occupied
port is an error; the script never kills an unrelated listener. It refreshes the
process PATH from the installed Windows environment. No credential is required.

The script makes the Legacy baseline by invoking SOAP GetCatalogBrands against
EF6, or the Csv baseline by starting the modern CSV initializer. Baselines are
`eShopWcfParity_Baseline_Legacy` and `eShopWcfParity_Baseline_Csv`. The first run
backs each up **once**, with COPY_ONLY and CHECKSUM. Subsequent runs verify and
reuse that immutable `.bak`; they **re-restore both disposable copies** with
unique MDF/LDF paths. Only names beginning with the fixed `eShopWcfParity_`
prefix are used. Initial ordered table-dump hashes must match before comparison.

The Legacy run therefore tests modern access to an **EF6-created database**;
the Csv run tests legacy access to an **EF Core-created database**. No migration,
schema edit, disabled EF initializer, fabricated migration history or fallback
database is used to make compatibility pass. Any EF refusal is recorded as the
actual SOAP fault and comparison failure, not silently repaired.

Each server gets its own process-local ConnectionString and database. Processes
are launched directly, tracked by Process objects, stopped individually (never
Kill(entireProcessTree), taskkill or a global LocalDB stop), and restarted between
phases. Startup, RPC/HTTP, child commands, SQL, and shutdown have finite bounds.
Final databases remain available for inspection; reset is re-running the script.

## Comparison and evidence

The full phase executes all requested reads, writes, detached deletion and
validation, then snapshots both sides. It tests **both empty and null names**, as
well as negative money. The restart phase repeats the reads and looks up the
generated IDs independently on each side. Explicit checkpoints require each
side's item rows to equal its own full-phase snapshot and both written stock
values (101 and 7) to persist. Symmetrically lost writes cannot pass merely because
the two servers agree with one another.

Results use the same canonical field names on both sides, sorted list IDs,
invariant decimal `G29` strings (no binary floating-point conversion), `yyyy-MM-dd`
dates, round-trip `R` double strings, and explicit null navigation members.
SOAP null results become JSON null. Every call stores the original SOAP body
and protobuf JSON separately, so raw decimal scale and units/nanos remain visible.
Transport errors, timeouts and gRPC Unavailable/DeadlineExceeded are **never**
mapped as intentional application faults.

Verdicts are MATCH, MAPPED or MISMATCH. Intentional mappings are:

- Legacy null result versus gRPC NOT_FOUND.
- Legacy SOAP fault versus an application-level non-OK gRPC status (actual status
  and original faultstring are recorded).
- **KNOWN_LEGACY_DEFECT** is a MAPPED subcategory: detached EF6 Remove faults
  whereas gRPC deletes successfully. The harness verifies the unchanged legacy
  row and modern NOT_FOUND before accepting this classification. Production WCF
  hides exception details; the independently run committed legacy BEFORE harness
  establishes the InvalidOperationException/ObjectStateManager cause. The SOAP
  harness does not pretend the generic SOAP fault disclosed the exception type.

Deletion has downstream effects: legacy retains the highest-ID row, whereas
modern max-plus-one creation can reuse its deleted ID. The harness records each
side's generated IDs. It permits only the verified retained row and this exact
ID remapping when checking downstream results; all other fields must match.
It does **not** mask unrelated row/value differences. Artifact counts show
ordinary MAPPED and KNOWN_LEGACY_DEFECT separately (disjoint counts).

Nullable scalar strings are **not normalized to empty**. Proto3 nonoptional
strings cannot express null: the null-name probe sends SOAP `i:nil="true"`, but
the generated gRPC field must be empty. If the stored/read names differ, that is
a visible MISMATCH, not an approved mapping. The harness does not change the
frozen protobuf contract or silently convert legacy database nulls to empty.

Files under `<ArtifactsRoot>/<Dataset>/`:

- `results.json`, `report.md`, `normalized-results.json`: full calls, raw and
  normalized output, verdicts/mapping reasons and counts.
- `restart-*`: separate restart-phase results/report/hash input.
- `state.json`: generated IDs and per-side persistence snapshots, required by
  `--phase reads-after-restart`.
- `initial-*.tsv`, `final-<dataset>-legacy.tsv`, `final-<dataset>-modern.tsv`:
  all five catalog tables, ordered by PK. Values use invariant decimal/date/double
  forms, JSON-escaped strings, and `\N` for SQL NULL. No EF migration metadata is
  included. `final-tables.diff` is the actual ordered diff; inequality is not hidden.
- `.bak`, unique MDF/LDF files, separate server/command stdout/stderr logs,
  `summary.json` and `summary.md`: commit, dirty-tree flag, ports, DB names,
  immutable backup SHA256, normalized result SHA256s, table hashes and exit codes.

SHA256 is calculated from the exact UTF-8 `normalized-results.json` bytes, excluding
raw transport payloads, timestamps, durations and process IDs. The console prints
both raw outputs per call and the normalized SHA256. Exit is 1 for any MISMATCH,
2 for an infrastructure/scenario error, otherwise 0. The orchestrator still runs
the restart phase and final table diff after a comparison exits nonzero, and
returns the maximum phase exit code. A raw table diff exit 1 means unequal files,
not a failed diff command.

Direct use:

```powershell
dotnet ./tests/eShopWcf.Parity/bin/Release/net8.0/eShopWcf.Parity.dll --legacy-url http://localhost:5100/CatalogService.svc --modern-url http://localhost:5200 --output C:\Users\Administrator\evidence\parity\Legacy --phase full
# Stop and restart both servers against the same copies before this invocation:
dotnet ./tests/eShopWcf.Parity/bin/Release/net8.0/eShopWcf.Parity.dll --legacy-url http://localhost:5100/CatalogService.svc --modern-url http://localhost:5200 --output C:\Users\Administrator\evidence\parity\Legacy --phase reads-after-restart
```
