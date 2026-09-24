# Modernization notes: eShopLegacyMVCSolution, .NET Framework 4.7.2 -> .NET 8

Branch: `eval/devin-net8-port` (from `main` @ `c654bd3`).
Scope: `eShopLegacyMVCSolution` only. `eShopLegacyWebFormsSolution` and `eShopLegacyNTier` are untouched.
Machine: Linux, .NET SDK 8.0.425, Docker 29.x. No Windows, MSBuild, .NET Framework reference assemblies or LocalDB.

---

## Phase 1 - port (commit `36ac57c3df7fca7ab4287228de70f52ad89e82c0`)

### What changed

| Legacy | .NET 8 |
|---|---|
| `packages.config` / non-SDK csproj (net472) | SDK-style `Microsoft.NET.Sdk.Web` / `Microsoft.NET.Sdk` projects targeting `net8.0` |
| `Global.asax`, `App_Start/*` (routes, filters, bundles, WebApi config), `Web.config` | `Program.cs` minimal hosting; conventional route `{controller=Catalog}/{action=Index}/{id?}`; attribute-routed API controllers; `appsettings*.json` |
| ASP.NET MVC 5 controllers (`System.Web.Mvc`) | ASP.NET Core MVC controllers (`IActionResult`, `NotFound()`, `BadRequest()`) |
| Web API 2 controllers (`ApiController`, `IHttpActionResult`) | `[ApiController] ControllerBase`, `[Route("api/[controller]")]`, `ActionResult<T>` |
| EF6 `DbContext`, `CatalogDBInitializer` (`CreateDatabaseIfNotExists`), embedded `*.sql` sequences | EF Core 8 `DbContext` (SQL Server provider), `EnsureCreated()` + explicit `CREATE SEQUENCE` for `catalog_hilo`, `catalog_brand_hilo`, `catalog_type_hilo` |
| `CatalogItemHiLoGenerator` (`NEXT VALUE FOR catalog_hilo`, block size 10) | Same class, ported to `Database.SqlQueryRaw<int>` |
| Autofac `ApplicationModule` | `IServiceCollection.AddApplicationModule(...)` extension over `Microsoft.Extensions.DependencyInjection` |
| log4net configured from `log4Net.xml` via `XmlConfigurator` | `Microsoft.Extensions.Logging.Log4Net.AspNetCore` reading the same `log4Net.xml` |
| `HttpContext.Current.Session`, `HttpContext.Current.Request` in views/`Global.asax` | `ISession` (`AddSession` + `AddDistributedMemoryCache`), helpers in `Infrastructure.cs` |
| `@Scripts.Render` / `@Styles.Render` bundles | Static `<link>`/`<script>` tags served from `wwwroot` (`Content/`, `Scripts/` moved there) |
| `Html.Partial` | `await Html.PartialAsync` |
| `Views/Web.config` | `_ViewImports.cshtml` (namespaces + tag helpers) |
| `Application Insights` config | Removed (no Core equivalent was requested) |
| MSTest project net472 + MVC 5 mocks | net8.0; `ViewBag` assertions -> `ViewData`, `UrlHelper`/`HttpContextBase` mocks -> ASP.NET Core `ControllerContext` |

Preserved behaviour verified at runtime (mock mode and SQL Server via Docker):
- `UseMockData` / `UseCustomizationData` toggles from configuration (`appsettings.json` / env vars).
- HiLo id generation (new item after 12 seeded items got id 13; `catalog_hilo` advanced to 11 = one block).
- Zero-based pagination (`Skip(pageSize * pageIndex)`, default page size 10).
- Web API endpoints: `GET /api/brands`, `GET /api/brands/{id}` (404 on miss), `DELETE /api/brands/{id}`, `GET /api/files` (binary), `GET /api/catalog` (`Hello World!`).
- Razor views (`Catalog/Index|Details|Create|Edit|Delete`, `_Layout`) and `items/{id}/pic` image route.
- `/api/files` still emits `BinaryFormatter` payloads: `EnableUnsafeBinaryFormatterSerialization=true` + `SYSLIB0011` suppressed; no serializer swap was needed.

### Dependency decisions

- `Microsoft.EntityFrameworkCore.SqlServer` / `.Relational` **8.0.11** (LTS line matching net8.0).
- `Microsoft.Extensions.Logging.Log4Net.AspNetCore` **8.0.0** to keep log4net and the existing `log4Net.xml` rather than rewriting logging.
- `MSTest.TestFramework/TestAdapter` **3.2.2**, `Microsoft.NET.Test.Sdk` **17.9.0**, `Moq` kept.
- Autofac dropped in favour of built-in DI (single module, no advanced features used).
- Application Insights, `Microsoft.CodeDom.Providers.DotNetCompilerPlatform`, `Antlr`, `WebGrease`, `Microsoft.AspNet.Web.Optimization` dropped (no runtime role on Core).

### Behaviour not preserved

- Bundling/minification (`BundleConfig`) is gone; the individual CSS/JS files are referenced directly.
- Application Insights telemetry is removed.
- `Global.asax` request logging of `Request.UserHostAddress`/`UserAgent` is replaced by ASP.NET Core request logging (`WebRequestInfo` helper kept for the layout).
- EF6 `CreateDatabaseIfNotExists` initializer is replaced by `EnsureCreated()`; there are no EF Core migrations in Phase 1 (added for the extracted service in Phase 2).
- Legacy `Web.config` transforms (`Web.Debug/Release.config`) have no equivalent; environment-specific config is in `appsettings.Development.json`.

### Test totals

```
Passed!  - Failed:     0, Passed:    48, Skipped:     0, Total:    48, Duration: 143 ms - eShopLegacyMVC.Tests.dll (net8.0)
```

### Commands run

```bash
dotnet --version                      # 8.0.425
git checkout -b eval/devin-net8-port main
dotnet build eShopLegacyMVCSolution/eShopLegacyMVC.sln
dotnet test eShopLegacyMVCSolution/tests/eShopLegacyMVC.Tests/eShopLegacyMVC.Tests.csproj
# runtime checks (mock mode)
dotnet run --project eShopLegacyMVCSolution/src/eShopLegacyMVC          # ASPNETCORE_ENVIRONMENT=Development -> UseMockData=true
curl http://localhost:5000/api/catalog/items ; curl http://localhost:5000/api/brands ; curl http://localhost:5000/api/files
# runtime checks (SQL Server mode)
docker run -d --name sqltest -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='Pass@word1' -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
ASPNETCORE_ENVIRONMENT=Production dotnet run --project eShopLegacyMVCSolution/src/eShopLegacyMVC
git commit -m "phase1: port eShopLegacyMVC solution from .NET Framework 4.7.2 to ASP.NET Core / EF Core on net8.0"
```

---

## Phase 2 - beyond the port (commit `phase2:`; SHA recorded in the final report)

### What changed

1. **Containerization**
   - `eShopLegacyMVCSolution/src/eShopLegacyMVC/Dockerfile`: multi-stage (`sdk:8.0` -> `aspnet:8.0`), restore/publish layer split, runs as the non-root `app` user, `UseMockData=true` and `ASPNETCORE_HTTP_PORTS=8080` defaults, writable `/app/logFiles` for log4net.
   - `eShopLegacyMVCSolution/src/eShop.Catalog.Api/Dockerfile`: same shape for the extracted API.
   - `eShopLegacyMVCSolution/docker-compose.yml`: `sqlserver` (SQL Server 2022, healthcheck via `sqlcmd`, named volume) + `catalog-api` (non-mock, applies EF Core migrations on start) + `webmvc` (non-mock, `CatalogApi__BaseUrl=http://catalog-api:8080`). Ports 5000 (MVC), 5100 (API), 1433.
   - `eShopLegacyMVCSolution/.dockerignore`.
   - Docker **was available**: image built and smoke-tested (see commands). `GET /api/catalog/items` inside the mock-mode container returned 200 with 12 items, running as `app`.

2. **Catalog bounded context -> `src/eShop.Catalog.Api`**
   - Own entities (`Model/CatalogItem|CatalogBrand|CatalogType`), own `CatalogContext` in SQL schema **`catalog`** (tables `catalog.Catalog`, `catalog.CatalogBrand`, `catalog.CatalogType`, sequences `catalog.catalog_hilo|catalog_brand_hilo|catalog_type_hilo` used via EF Core `UseHiLo`, block size 10).
   - EF Core migration `InitialCatalogSchema` (`Infrastructure/Migrations`), applied with `Database.Migrate()` at startup in relational mode; InMemory provider when `UseMockData=true`.
   - Seeder (`CatalogContextSeed`, `PreconfiguredData`, `CsvSetupReader`) reproduces the 4 types / 5 brands / 12 items and the optional CSV customization (`Setup/*.csv` moved from the MVC project).
   - Endpoints: `GET /api/catalog/items?pageSize&pageIndex&brandId&typeId` (zero-based, 400 on invalid paging), `GET/PUT/DELETE /api/catalog/items/{id}` (404 on miss), `POST /api/catalog/items` (201 + Location), `GET /api/catalog/brands`, `GET /api/catalog/types`, `/health`, Swagger.
   - MVC side: EF Core, `CatalogDBContext`, `CatalogService`, HiLo generator and `*.sql` files **removed** from `eShopLegacyMVC`. New `Services/Catalog/CatalogApiDtos.cs` (MVC-owned DTOs, no shared types), `CatalogApiClient` (typed `HttpClient`, `System.Net.Http.Json`), `CatalogHttpService : ICatalogService` (maps DTOs <-> MVC view models). `ApplicationModule` registers `CatalogServiceMock` (in-process) when `UseMockData=true`, otherwise the typed client with `CatalogApi:BaseUrl`.
   - `Controllers/WebApi/CatalogApiController.cs` keeps `/api/catalog/items|items/{id}|brands|types` answering on the MVC host (backed by `ICatalogService`) so existing clients and the container smoke test keep working in both modes.

3. **Integration tests -> `tests/eShop.Catalog.Api.IntegrationTests`**
   - MSTest + `Microsoft.AspNetCore.Mvc.Testing` `WebApplicationFactory<Program>` with `UseMockData=true` (EF Core InMemory, seeded).
   - 17 tests: default page is zero-based (ids 1-10 of 12), page 1 returns ids 11-12, `pageSize=5&pageIndex=1` returns 6-10, page past end returns empty data + total count, invalid paging -> 400, brand/type names included, get-by-id, 404 on miss, brand filter (6 `.NET` items), type filter (3 `Sheet` items), combined filter, unknown brand -> empty, brands (5) and types (4), create/update/delete round-trip, validation 400, `/health`.
   - Added to `eShopLegacyMVC.sln`.

### Dependency decisions

- `Microsoft.EntityFrameworkCore.{SqlServer,InMemory,Design}` **8.0.11** in the API only; the MVC project now has **no EF dependency** (only `Microsoft.Extensions.Http` 8.0.1 for `AddHttpClient<T>`).
- InMemory provider (not SQLite) for mock mode and integration tests: the API already needed a provider that supports HiLo-free key generation for mock mode, and InMemory keeps the tests dependency-free. HiLo is configured only when `Database.IsRelational()`.
- `Swashbuckle.AspNetCore` 6.9.0 for API discovery.
- `Microsoft.AspNetCore.Mvc.Testing` 8.0.11.
- `ICatalogService` stayed synchronous to avoid touching every MVC controller/test; `CatalogHttpService` blocks on the async client (`GetAwaiter().GetResult()`). Acceptable for this app; an async interface is the obvious follow-up.
- No `HEALTHCHECK` instruction in the Dockerfiles: `aspnet:8.0` ships without `curl`/`wget`; `/health` endpoints exist for orchestrators to probe instead.

### Behaviour not preserved

- In non-mock mode the MVC app no longer talks to SQL Server directly; catalog data lives in the `eShop.Catalog` database, schema `catalog`, owned by the API. The Phase 1 `Microsoft.eShopOnContainers.Services.CatalogDb` (dbo schema) is not migrated automatically.
- `DELETE /api/brands/{id}` in the MVC app is still the legacy demo stub (no persistence) - unchanged from the original.
- HiLo now uses EF Core's built-in `UseHiLo` (sequence increment 10) instead of the hand-written `CatalogItemHiLoGenerator`; ids are still allocated in blocks of 10 from a SQL sequence, but the exact values after a restart can differ from EF6 (EF Core reserves a new block per context instance; the legacy generator reserved per process).
- `POST /api/catalog/items` from the API returns 201 with the created item; the legacy MVC `Create` action still redirects to `Index` as before.
- `PUT /api/catalog/items/{id}` / `DELETE` return 204 (REST convention) - the legacy app had no such API.

### Defects found and fixed (one-line repros)

| # | Defect | Repro | Fix |
|---|---|---|---|
| 1 | `log4Net.xml` was declared with `<Content Update=...>` so it was never copied to the publish output; the container crashed with `FileNotFoundException: /app/log4Net.xml`. | `docker build -f src/eShopLegacyMVC/Dockerfile . && docker run -p 8085:8080 <img>` -> exits immediately. | `<Content Include="log4Net.xml" CopyToOutputDirectory/CopyToPublishDirectory>` in `eShopLegacyMVC.csproj`. |
| 2 | `CatalogServiceMock.FindCatalogItem` returned items without `CatalogBrand`/`CatalogType` navigation objects, so `/Catalog/Details/{id}` and `/api/catalog/items/{id}` in mock mode had null brand/type. | `ASPNETCORE_ENVIRONMENT=Development dotnet run` then `curl localhost:5000/api/catalog/items/1` -> `"CatalogBrand":null`. | `FindCatalogItem` now runs the same brand/type composition as `GetCatalogItemsPaginated`. |
| 3 | `log4Net.xml` used a Windows path separator (`logFiles\myapp.log`), producing a file literally named `logFiles\myapp.log` on Linux. | `dotnet run` on Linux, `ls src/eShopLegacyMVC` -> file `logFiles\myapp.log`. | Path changed to `logFiles/myapp.log` (Phase 1). |
| 4 | `docker-compose` SQL Server healthcheck used `-S localhost`, which resolves to `::1` in the mssql image and times out, so `catalog-api` never started. | `docker compose up -d` -> `sqlserver` stuck in `health: starting`. | Healthcheck uses `-S tcp:127.0.0.1,1433`. |
| 5 | Seed transcription: `Cup<T> Sheet` was given brand `Other` instead of `.NET` in the new API seed (caught by the brand-filter integration test expecting 6 `.NET` items). | `dotnet test tests/eShop.Catalog.Api.IntegrationTests` -> `Items_FilterByBrand` expected 6, actual 5. | Corrected `PreconfiguredData`. |

### Test totals

```
Passed!  - Failed:     0, Passed:    48, Skipped:     0, Total:    48, Duration: 146 ms - eShopLegacyMVC.Tests.dll (net8.0)
Passed!  - Failed:     0, Passed:    17, Skipped:     0, Total:    17, Duration: 882 ms - eShop.Catalog.Api.IntegrationTests.dll (net8.0)
```

### Commands run

```bash
# build & tests
dotnet build eShopLegacyMVCSolution/eShopLegacyMVC.sln
dotnet test eShopLegacyMVCSolution/tests/eShopLegacyMVC.Tests/eShopLegacyMVC.Tests.csproj
dotnet test eShopLegacyMVCSolution/tests/eShop.Catalog.Api.IntegrationTests/eShop.Catalog.Api.IntegrationTests.csproj

# migrations
cd eShopLegacyMVCSolution/src/eShop.Catalog.Api
dotnet tool install --global dotnet-ef --version 8.0.11
dotnet ef migrations add InitialCatalogSchema -o Infrastructure/Migrations

# local two-process run (API mock, MVC calling it over HTTP)
dotnet run --project eShopLegacyMVCSolution/src/eShop.Catalog.Api          # http://localhost:5100
UseMockData=false CatalogApi__BaseUrl=http://localhost:5100 dotnet run --project eShopLegacyMVCSolution/src/eShopLegacyMVC
curl 'http://localhost:5100/api/catalog/items?pageSize=5&pageIndex=1' ; curl -i http://localhost:5100/api/catalog/items/9999

# container smoke test (mock mode, no SQL Server)
cd eShopLegacyMVCSolution
docker build -t eshop-webmvc -f src/eShopLegacyMVC/Dockerfile .
docker run -d --name smoke -p 8085:8080 eshop-webmvc
docker exec smoke whoami                                   # app
curl -w '%{http_code}' http://localhost:8085/api/catalog/items   # 200, Count=12
docker rm -f smoke

# full non-mock stack
docker compose up -d --build
curl http://localhost:5100/api/catalog/items ; curl http://localhost:5000/api/catalog/items ; curl -i http://localhost:5000/api/catalog/items/9999
docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -C -S tcp:127.0.0.1,1433 -U sa -P "$SA_PASSWORD" -d eShop.Catalog \
  -Q "SELECT name, CAST(current_value AS int) FROM sys.sequences; SELECT COUNT(*) FROM catalog.Catalog"   # catalog_hilo=11, 12 rows
docker compose down -v
```

---

## Main obstacles

- No `HEALTHCHECK` possible with the stock `aspnet:8.0` image (no curl); relied on `/health` endpoints instead.
- The `sqlcmd` healthcheck in the mssql image fails on `localhost` (IPv6); needed an explicit IPv4 address.
- MSBuild `Content Update` vs `Include` for non-default content items only surfaces at publish time (found through the container smoke test).
- `System.Web`-specific test mocks (`HttpContextBase`, `UrlHelper`) had to be rewritten for ASP.NET Core while keeping the same assertions.
