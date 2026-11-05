# winforms-parity-v1

A component-specific **desktop comparison fixture**, not a replacement for the MVC customization CSVs or a schema migration. Legacy WCF `PreconfiguredData` and all application code remain unchanged. The legacy database starts from the unmodified EF6 WCF baseline; the modern database starts from PR #226's default EF Core seed (`Catalog:UseMockData=false`, `UseMockData=false`, `Catalog:UseCustomizationData=false`).

## Dataset mapping

| Aspect | Legacy WCF seed | Canonical MVC Setup CSVs | #226 default database seed | winforms-parity-v1 |
|---|---|---|---|---|
| Brands | 5: Azure, .NET, Visual Studio, SQL Server, Other | 7; adds CatalogBrandTestOne/Two; source has whitespace on some names | Same 5 as WCF | Same 5 as WCF |
| Types | 4: Mug, T-Shirt, Sheet, USB Memory Stick | 6; adds CatalogTypeTestOne/Two | Same 4 as WCF | Same 4 as WCF |
| Items | 12, application-assigned IDs 1..12 | 13 data rows; extra `pepito` / `De los Palotes` | 12, IDs 1..12 | WCF's 12 items on both servers |
| First description | .NET Bot Black Hoodie | .NET Bot Black Hoodie, and more | .NET Bot Black Hoodie | .NET Bot Black Hoodie |
| Item 12 name/description | Cup<T> TShirt | Prism White TShirt | Prism White TShirt | Cup<T> TShirt |
| Pictures | WCF mapping below | 1.png..12.png, then 12.png for extra row | 1.png..12.png | WCF mapping below |
| Dated stock | 6 rows | Not represented | CatalogItemsStock empty | Exact WCF 6 rows |
| Discounts | 6 historical 2017 rows | Not represented | DiscountItems empty | Exact WCF 6 rows + row 7, 2026-01-01..2030-12-31, size 0.15 |
| Modern-only item fields | Absent | Available stock varies, second item OnReorder=true | AvailableStock=100; thresholds=0; OnReorder=false | Modern values unchanged; excluded from shared comparison |

Canonical CSV row order is shown for comparison; CSVs do not contain explicit item IDs. Shared numeric prices and brand/type IDs match between the two default server seeds and are not updated by this fixture.

| WCF ID | WCF name and description / parity name and description | WCF / parity picture | MVC CSV / #226 picture |
|---:|---|---|---|
| 1 | .NET Bot Black Hoodie | 2.png | 1.png |
| 2 | .NET Black & White Mug | 11.png | 2.png |
| 3 | Prism White T-Shirt | 7.png | 3.png |
| 4 | .NET Foundation T-shirt | 5.png | 4.png |
| 5 | Roslyn Red Sheet | 9.png | 5.png |
| 6 | .NET Blue Hoodie | 1.png | 6.png |
| 7 | Roslyn Red T-Shirt | 6.png | 7.png |
| 8 | Kudu Purple Hoodie | 3.png | 8.png |
| 9 | Cup<T> White Mug | 12.png | 9.png |
| 10 | .NET Foundation Sheet | 8.png | 10.png |
| 11 | Cup<T> Sheet | 10.png | 11.png |
| 12 | Cup<T> TShirt | 4.png | 12.png |

Legacy stock `(StockId, date, item, quantity)`:

```text
1  2017-09-20  1  100
2  2017-09-21  1  120
3  2017-09-22  1   80
4  2017-09-20  2   45
5  2017-09-25  4   65
6  2017-09-28  5   22
```

Discount sizes preserve the actual WCF binary32-to-binary64 values, **not rounded decimal approximations**:

| ID | Start | End | Stored float(53), round-trip representation |
|---:|---|---|---|
| 1 | 2017-09-18 | 2017-09-21 | 0.30000001192092896 |
| 2 | 2017-09-22 | 2017-09-26 | 0.25 |
| 3 | 2017-09-27 | 2017-09-30 | 0.10000000149011612 |
| 4 | 2017-10-05 | 2017-10-20 | 0.5 |
| 5 | 2017-11-13 | 2017-11-25 | 0.30000001192092896 |
| 6 | 2017-12-20 | 2017-12-25 | 0.25 |
| 7 | 2026-01-01 | 2030-12-31 | 0.15 |

## Apply and reset on the comparison VM

Stop both servers first. Restore `eShopWinForms_Legacy` from your original `eShopWinForms_Legacy_baseline.bak` using your baseline reset script. Then, from this directory:

```powershell
$sql = 'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\180\Tools\Binn\SQLCMD.EXE'
& $sql -S '(localdb)\MSSQLLocalDB' -d eShopWinForms_Legacy -b -l 30 -t 60 -i fixtures\winforms-parity-v1.legacy.sql
& $sql -S '(localdb)\MSSQLLocalDB' -d eShopWinForms_Grpc -b -l 30 -t 60 -i fixtures\winforms-parity-v1.modern.sql
```

Check each `$LASTEXITCODE` before continuing. Scripts are transactional and database-name guarded. Legacy replaces only discount ID 7; modern replaces stock IDs 1..6 and discount IDs 1..7, and updates the twelve items' desktop text/pictures. Explicit discount IDs support either identity or nonidentity keys. Unrelated rows are not deleted: start from the stated baselines for deterministic results. Reapplication is idempotent. Modern-only columns, IDs, prices, brand/type IDs, and all source seed files are untouched.

Parity snapshots must be named `eShopWinForms_Legacy_parity_v1.bak` and `eShopWinForms_Grpc_parity_v1.bak` in your backup directory. The harness runner below replaces the VM-local reset/start scripts with parameterized restore, startup, readiness and cleanup operations.

## Normalized comparison

`normalize-parity.ps1` in the evidence directory produces one complete JSON object per database with brands, types, desktop-visible item fields, dated stock, and discounts. Arrays sort by primary key; object fields have fixed order; prices are invariant four-place decimal strings; dates are `yyyy-MM-dd`; discount Size is an invariant round-trip double **string** to preserve exact stored float values. Encoding is UTF-8 without BOM, compact JSON plus one LF. Migration metadata, identity/sequence state, navigation objects and modern-only fields are intentionally excluded.

Both shared datasets SHA256:

```text
82ED9FFC8320B06D7F7CB5CE7D898B49DE90B1DCF73CB5C04C41D2F5F24E186A
```

This establishes data parity, not schema compatibility or identical SOAP/gRPC serialization. SQL money(19,4) versus decimal(18,2), plural versus singular tables, identity/sequence differences and navigation population still differ. Banner/price rendering in desktop clients requires a separate visual check.

## File SHA256

Canonical sources are byte hashes of `eShopLegacyMVCSolution/src/eShopLegacyMVC/Setup/` files (including original whitespace and line endings), not hashes of normalized CSV values.

| File | SHA256 |
|---|---|
| CatalogBrands.csv | 5902017931EB5ADA935BC42A08189D547DC27A889F2BA7C1ACCADAA1506BB76E |
| CatalogItems.csv | 630D09D9F9956640CC4A0E04EF8D7BE75E8119405C74598AAF2B5EB62415BDD2 |
| CatalogTypes.csv | E6BE222C6C20DFAE2AB66399AB58B62033B6E5173872C02D9DDDE57E57FFBF95 |
| fixtures/winforms-parity-v1.legacy.sql | 532763BAB6AB98C9450C6F0B59F9A0FA59A989D8E27B2B3B5BCEB132A12D215D |
| fixtures/winforms-parity-v1.modern.sql | 79FEF6CE8A87D6A166B7DDE2609E969BABB5477EDF3969BA6F0829577591C5AA |

Recompute SQL hashes if editing the fixtures or changing their line endings.

## Real-form parity harness

Requires Windows with an interactive desktop, .NET Framework 4.7 targeting pack, Visual Studio MSBuild, .NET 8 SDK, restored legacy NuGet packages, IIS Express, LocalDB, ODBC sqlcmd and grpcurl. Use isolated disposable databases only: the runner restores **both databases WITH REPLACE** before each client's run and in final cleanup. Backups must already contain this fixture; this script does not create or silently reseed them. The copied WCF site's EntityModel connection string must match `-SqlInstance` and `-LegacyDatabase` (checked before stopping anything). Supply the #226 server DLL and its configuration working directory; no server is built by the runner.

Build from the repository root, supplying your MSBuild executable path:

```powershell
& $msbuild eShopModernizedWinFormsSolution/parity/Harness.Legacy/Harness.Legacy.csproj /p:Configuration=Release /m /v:minimal
dotnet build eShopModernizedWinFormsSolution/parity/Harness.Modern/Harness.Modern.csproj -c Release
```

The net47 executable references the original legacy project and is deliberately **not** in the modern solution. The .NET 8 executable is in that solution. Both compile the same `ParityHarness.Scenarios.cs`; conditional code is limited to client construction, runtime initialization and the legacy working-directory/image setup. Neither uses fake controllers, views or services. Legacy image assets are copied to the output evidence directory's `Assets/Images/Catalog`, with a matching nested working directory for its unchanged relative-path loader. No legacy source/configuration files are edited.

Run from a PowerShell process, supplying absolute local paths in these variables:

```powershell
& ./eShopModernizedWinFormsSolution/parity/run-parity.ps1 `
  -WcfSite $wcfSite -GrpcDll $grpcDll -GrpcWorkingDirectory $grpcWorkingDirectory `
  -BackupDirectory $backupDirectory -OutputDirectory $evidenceDirectory `
  -SqlCmd $sqlcmd -IisExpress $iisExpress -Dotnet $dotnet -Grpcurl $grpcurl `
  -SqlInstance '(localdb)\MSSQLLocalDB' `
  -LegacyDatabase eShopWinForms_Legacy -ModernDatabase eShopWinForms_Grpc `
  -LegacyPort 62314 -ModernPort 5200
```

Optional `-LegacyHarness`, `-ModernHarness`, `-LegacyImages` override repository-relative defaults. Database backup names are `<database>_parity_v1.bak`. The runner stops only IIS Express/catalog processes matching the supplied site/DLL, understands IIS's HTTP.sys listener, and rejects unrelated port owners. It starts gRPC with both mock flags false, the supplied SQL connection and cleartext HTTP/2 address. SOAP GetCatalogBrands and gRPC reflection readiness checks are bounded. Harness subprocesses have a configurable timeout (`-HarnessTimeoutSeconds`, default 180); server startup has a 90-second deadline and sqlcmd uses login/query timeouts.

For the sibling WCF-worker service (`eShopModernizedWcfSolution`, PR #241), supply its DLL/working directory and `-GrpcConnectionStringKey ConnectionStrings__CatalogDb`. The default key remains `ConnectionStrings__Catalog` for PR #226. Unset any inherited `ConnectionString` before running: both legacy and sibling configuration give that variable precedence. A production WCF-created baseline can be restored into separate legacy and sibling databases because the sibling maps the WCF table shapes; verify this on the target SQL Server rather than applying the #226 fixture's singular-table SQL to the sibling. The sibling retains max+1 item IDs, so the round-one #226 HiLo-ID difference does not apply to it. Null-string wire differences and the legacy detached-delete defect remain separate parity limitations.

Sequence: stop/reset/start -> legacy scenarios -> stop/start **without restoring** -> legacy persisted checks -> stop/reset/start -> modern scenarios -> stop/start **without restoring** -> modern persisted checks -> compare -> finally stop/reset/start. The final state is clean parity-v1 databases with both services running, including after scenario failures. Infrastructure/cleanup failures return 1 rather than claiming a clean state.

### Scenarios and evidence

Each real `CatalogView` is shown off-screen on STA. Combobox selections fire filter events, list selection/calendar changes and `PerformClick` invoke stock search, and actual shipment controls invoke the write handler. A background Win32 observer inspects only the harness UI thread's `#32770` dialogs, records Static text and clicks the OK button; it does not replace or bypass MessageBox.Show. Expected FormatException/NullReferenceException are recorded; other failures remain failures and subsequent scenarios still run.

The shared scenarios cover initial load; brand2/type2/brand5+type1/empty-brand3/empty-type4; four historical stock queries; shipment 3@2030-01-15=50 then 75 and 1@2017-09-20=101, searching after each; empty/non-numeric quantity and no selected list item; client Find(1), Find(999), Create, Find(created), Update and Remove using the fetched real item; and the three specified discount dates. `--verify-persisted` starts a fresh client/form and checks both written stocks and absence of the CRUD item after a service restart.

`legacy.json`, `modern.json` and their `-persisted` counterparts contain all observations, exceptions (type **and full message**) and captured dialog texts. Their `.sha256` sidecars hash the exact UTF-8/no-BOM, compact JSON plus LF. Object keys sort ordinally; catalog grid rows sort by numeric ID; UI list ordering is retained. Dates normalize to `yyyy-MM-dd`; client prices use four-place invariant decimal strings; discount doubles use round-trip strings. UI price **text**, image presence/dimensions, banner, filter items/selections, shipment lists/control reset state and stock result rows are captured unchanged. Client item projections cover desktop-visible scalar fields, not transport navigation graphs or modern-only stock fields. Generated IDs are recorded unchanged, not remapped to force equality.

`comparison.csv` and `comparison-persisted.csv` include every scenario's complete legacy/modern result and match flag. `differences.json` and `differences-persisted.json` enumerate differing JSON paths/values. Server generations and subprocess stdout/stderr are retained separately. Exit codes: **0** identical and all assertions pass; **2** differing observations with successful scenarios; **1** infrastructure failure or failed scenario/persistence assertion. Differences are never filtered away, including .NET runtime exception wording and server-generated ID differences. Evidence belongs outside the repository and must not be committed.
