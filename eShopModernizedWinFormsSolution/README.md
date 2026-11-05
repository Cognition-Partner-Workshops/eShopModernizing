# Modern WinForms desktop client

.NET 8 Windows port of the legacy NTier WinForms application. The local gRPC contract is pinned to PR #226 commit af59408cd8dc268d0931988f2cbc86ab66afff45; only its C# namespace changes. No product server or legacy project reference is required.

Build/test this solution with the .NET 8 SDK. NuGet.config uses nuget.org explicitly. Run src/eShopWinForms/bin/Release/net8.0-windows/eShopWinForms.exe. CatalogService:Address in appsettings.json defaults to http://localhost:5200; CatalogService__Address overrides it.

Designer, resources, images, control names and synchronous controller/view flow are copied from the legacy app. Image loading is based on AppContext.BaseDirectory rather than the working directory.

Every gRPC call has a fresh deadline using `CatalogService:CallTimeout`, default `00:01:00` (the legacy WCF one-minute SendTimeout). Set a positive TimeSpan in appsettings.json or override it with `CatalogService__CallTimeout`, for example `00:00:10`. DeadlineExceeded propagates as an RpcException; only NotFound is translated to null.

Intentional legacy defects retained: the banner says "sale endson"; nonnumeric shipment quantity throws from int.Parse; stock search without a selected list item throws NullReferenceException. Empty shipment fields remain a no-op, prices use the legacy double calculation with explicit two-place midpoint-away display to retain .NET Framework formatting, and shipment controls retain their original reset behavior. No validation or UX redesign is included.

Tests use controller fakes and a test-only in-process loopback gRPC server generated from the original #226 contract. Descriptor tests compare every message field and service method against that independently generated reference. STA view tests cover legacy price formatting. See parity/README.md for database fixtures and the parameterized real-form side-by-side parity harness. No CI workflow is included.
