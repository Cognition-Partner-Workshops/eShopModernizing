# Windows LocalDB parity harness

Run from `eShopModernizedMVCSolution` in Windows PowerShell as the same Windows user that owns LocalDB. Build the untouched legacy solution with Framework MSBuild first. Prerequisites: SDK 8.0.427, IIS Express, LocalDB 2022, Framework 4.7.2 targeting pack, VS web build tools.

```powershell
dotnet --version
dotnet build eShopModernizedMVC.sln -c Debug
.\tests\Invoke-Parity.ps1 -Action Prepare
.\tests\Invoke-Parity.ps1 -Action Run -Run run1
.\tests\Invoke-Parity.ps1 -Action Run -Run run2
.\tests\Export-ParityEvidence.ps1
```

Do not run concurrently. Ports: legacy IIS Express 5101, existing-schema modern Kestrel 5102, fresh-schema Kestrel 5103. Each run restores two separate databases from a single immutable backup, exercises writes, restarts both servers and checks persistence. Servers stop at the end. `-Action Stop` also terminates recorded process trees. Evidence defaults to `C:\Users\Administrator\evidence\parity` and can be overridden.

Prepare refuses to overwrite a baseline backup or an existing default legacy database. The untouched legacy SQL seed scripts hard-code `USE [Microsoft.eShopOnContainers.Services.CatalogDb]`, so Prepare seeds that **previously absent** database, backs it up, then restores `eShopParity_Baseline`. It never modifies legacy scripts/source. Only the copied deployment Web.config is overridden. Reset only drops/restores the explicitly named `eShopParity_*` databases; the source seed database is preserved. Do not use these names for other work.

The canonical CSVs and picture ZIP must match the legacy setup inputs byte-for-byte. The fresh modern database is seeded separately and its schema/data are dumped without merging away differences. Dumps include columns/types/precision/nullability/identity, foreign keys/delete behavior and sequence definitions/current state. Normalized application data exclude EF migration metadata and are ordered by ID. Cross-app SQL assertions join brand/type names and omit generated IDs; generated IDs are separately recorded.

The HTTP tests parse HTML with AngleSharp, never compare raw markup, keep separate cookies, extract real antiforgery tokens, assert validation prevents writes, and record known transport differences. BinaryFormatter payloads are **never deserialized**. Files responses record only status, content type and length. Non-200 error/redirect body differences are recorded but status codes are asserted. API/XML/binary-format differences are explicit observations, not parity assertions.

The project is inconclusive unless both `LEGACY_BASE_URL` and `MODERN_BASE_URL` are set. The orchestration also sets `PARITY_EVIDENCE` and `PARITY_PHASE`. Its SQL checks deliberately target only `eShopParity_Legacy` and `eShopParity_Modern` on the current user's LocalDB. It is not a production test runner.
