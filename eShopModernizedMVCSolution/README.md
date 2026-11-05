# ASP.NET Core MVC catalog (.NET 8)

The MVC 5 application remains unchanged in `eShopLegacyMVCSolution` for before/after comparison.

```powershell
dotnet build eShopModernizedMVCSolution/eShopModernizedMVC.sln -c Release
dotnet test eShopModernizedMVCSolution/eShopModernizedMVC.sln -c Release --no-build
$env:UseMockData = 'true'
dotnet run --project eShopModernizedMVCSolution/src/eShopModernizedMVC -c Release --no-build --urls http://localhost:5100
```

`UseMockData` defaults to true. Set it with an environment variable or an `appsettings.Development.json` override. Mock state is singleton/in-memory; restart the app to reset the original twelve products. The SQL Server connection string retains the legacy value and can be overridden with `ConnectionStrings__CatalogDBContext`.

## Reset

From the repository root on Windows (PowerShell 5 or later):

```powershell
powershell -ExecutionPolicy Bypass -File eShopModernizedMVCSolution\scripts\reset.ps1 -Start
```

Stops existing demo processes and listeners on 5100/5101, rebuilds both sites, and recreates `%USERPROFILE%\scratch\legacy-site` with mock data enabled without changing the legacy source configuration. Requires SDK 8.0.427, Visual Studio 2022 Build Tools and IIS Express. NuGet is downloaded to scratch if absent. `-Start` launches both apps and waits for HTTP 200 at http://localhost:5100/ (port) and http://localhost:5101/ (legacy). Logs are in `%USERPROFILE%\scratch`; omit `-Start` to rebuild/reset without launching. Restarting resets the in-memory catalog.

## Parity and deliberate platform differences

- MVC routes, bound property lists, antiforgery protection, validation attributes, image MIME mapping, views, assets, mock data and pagination are retained. The 48 legacy test methods and assertions are ported, with only ASP.NET Core plumbing/result-type substitutions.
- `GET /api/files` now returns UTF-8 JSON (`application/json`, PascalCase `Id`/`Brand`) instead of a BinaryFormatter stream. Consumers must deserialize JSON; BinaryFormatter payloads are not supported in .NET 8.
- API responses use System.Text.Json rather than Web API's content negotiation (XML responses are not offered). JSON property casing remains PascalCase.
- EF Core SQL Server replaces EF6. First-run `EnsureCreated` creates a fresh schema with the same tables and three HiLo sequences (increment 10); EF Core allocates IDs. CSV validation/fallback, preconfigured seeding and optional picture extraction are retained. Existing databases are not migrated automatically. SQL Server runtime parity is not verified by the mock-mode checks.
- Built-in scoped DI and ILogger replace Autofac/log4net. Application Insights/log4net-specific telemetry enrichment is not carried over; controller and request log messages remain.
- ASP.NET Core in-memory session replaces System.Web session for the machine/time footer. Cookies, antiforgery tokens and error middleware are framework-native, not wire-compatible with MVC 5. Session footer content is HTML-encoded rather than rendered as raw HTML.
- Bundles are replaced by direct references to copied CSS/JavaScript, retaining jQuery unobtrusive validation. Razor/HTML helpers may differ in generated IDs, attribute ordering and validation/antiforgery markup; the page structure, styling and data remain the same.

No database migration or production deployment is performed by this solution.
