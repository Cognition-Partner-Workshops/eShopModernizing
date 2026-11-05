# Modern WinForms desktop client

.NET 8 Windows port of the legacy NTier WinForms application. The local gRPC contract is pinned to PR #226 commit af59408cd8dc268d0931988f2cbc86ab66afff45; only its C# namespace changes. No product server or legacy project reference is required.

Build/test this solution with the .NET 8 SDK. NuGet.config uses nuget.org explicitly. Run src/eShopWinForms/bin/Release/net8.0-windows/eShopWinForms.exe. CatalogService:Address in appsettings.json defaults to http://localhost:5200; CatalogService__Address overrides it.

Designer, resources, images, control names and synchronous controller/view flow are copied from the legacy app. Image loading is based on AppContext.BaseDirectory rather than the working directory.

Every gRPC call has a fresh deadline using `CatalogService:CallTimeout`, default `00:01:00` (the legacy WCF one-minute SendTimeout). Set a positive TimeSpan in appsettings.json or override it with `CatalogService__CallTimeout`, for example `00:00:10`. DeadlineExceeded propagates as an RpcException; only NotFound is translated to null.

Intentional legacy defects retained: the banner says "sale endson"; nonnumeric shipment quantity throws from int.Parse; stock search without a selected list item throws NullReferenceException. Empty shipment fields remain a no-op, prices use the legacy double calculation with explicit two-place midpoint-away display to retain .NET Framework formatting, and shipment controls retain their original reset behavior. No validation or UX redesign is included.

Tests use controller fakes and a test-only in-process loopback gRPC server generated from the original #226 contract. Descriptor tests compare every message field and service method against that independently generated reference. STA view tests cover legacy price formatting. See parity/README.md for database fixtures and the parameterized real-form side-by-side parity harness. The component's Windows CI builds both clients and runs the modern unit tests; it does not claim side-by-side database parity.

## Independent service deployment

The client includes no catalog server and has no project reference to an unmerged server PR. Deploy a server implementing the pinned `eshop.catalog.v1.Catalog` contract separately. The sibling `eShopModernizedWcfSolution` service in PR #241 uses that contract without changing RPC or field numbers. Its SQL configuration key is `ConnectionStrings__CatalogDb` (or `ConnectionString`), unlike PR #226's `ConnectionStrings__Catalog`. Use separate disposable database copies for comparison; do not point PR #226's EF Core migrations at an EF6 WCF database.

The `parity/PeerProbe` console executable exercises all ten RPCs through this desktop client's actual wrapper against a separately running server. It checks signed four-place decimals, nullable-description mapping, missing rows, filters, inclusive discount boundaries and calendar-date stock overwrite behavior. It is an integration probe, not an original Framework test suite or proof of strict SOAP equivalence. It mutates stock and catalog rows and requires explicit `--allow-writes`; run only against a disposable database:

```powershell
dotnet run --project parity/PeerProbe -c Release -- http://localhost:5200 C:\evidence\peer-probe.json --allow-writes
```

The probe expects the historical WCF discount fixture, with discount 1 inclusive on 2017-09-18..21 and discount 2 starting 2017-09-22. It accepts either the 12-row WCF seed or the 13-row canonical CSV seed. Restore the database after the probe; its stock writes are intentionally retained for restart-persistence checks.
