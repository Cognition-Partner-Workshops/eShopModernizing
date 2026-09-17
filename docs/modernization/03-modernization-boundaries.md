# Proposed .NET Modernization Boundaries

**Repository:** [Cognition-Partner-Workshops/eShopModernizing](https://github.com/Cognition-Partner-Workshops/eShopModernizing) · **Commit:** `c654bd3` · **Generated:** 2026-07-27 by Devin (.NET Discovery playbook, Phase 3).

Related pages: [.NET Codebase Inventory](./01-codebase-inventory.md) · [.NET Behavioral Baseline](./02-behavioral-baseline.md) · [.NET Modernization Roadmap](./04-modernization-roadmap.md)

## 1. Components and extraction order

Eight components, ordered so that leaves of the dependency graph are extracted first. Target runtime for everything is **.NET 8 (LTS)** on Linux containers, except the desktop client.

| # | Component | Source projects / paths | Legacy patterns | Target stack | Depends on | Effort |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | **C1 — Platform Foundation** | `eShopLegacy.Utilities`; `App_Start/*`, `Global.asax.cs`, `Web.config`, `log4Net.xml` of both web apps | Autofac modules, log4net + `Trace.CorrelationManager`, Web.config appSettings/connection strings, `BinaryFormatter`, binding redirects | .NET 8 class library, `Microsoft.Extensions.{DependencyInjection,Configuration,Logging}`, Serilog + OpenTelemetry, `System.Text.Json`, `appsettings.json` + env vars | — | **M** |
| 2 | **C2 — Catalog Domain & Data** | `eShopLegacyMVC/Models/*`, `eShopLegacyWebForms/Models/*`, `eShopWCFService/{EntityModel,CatalogItem,CatalogBrand,CatalogType,CatalogItemsStock,DiscountItem}.cs`, `Models/Infrastructure/*.sql`, `Setup/*.csv|zip` | Three duplicated EF6 `DbContext`s, `CreateDatabaseIfNotExists` seeding, SQL sequence/HiLo id generation, LocalDB connection strings | Single shared .NET 8 domain + `eShop.Catalog.Data` on EF Core 8 (SQL Server provider), EF Core migrations, HiLo value generator, seed from CSV/ZIP at startup | C1 | **L** |
| 3 | **C3 — Catalog HTTP API** | `eShopLegacyMVC/Controllers/WebApi/{BrandsController,FilesController}.cs`, `Controllers/PicController.cs`, `Controllers/Api/CatalogController.cs` (delete — dead) | Web API 2 `ApiController`, attribute routing, `BinaryFormatter` response, no-op DELETE | ASP.NET Core 8 controllers/minimal APIs, JSON via `System.Text.Json`, static/blob picture endpoint, OpenAPI | C2 | **M** |
| 4 | **C4 — Catalog SOAP Service** | `eShopLegacyNTier/src/eShopWCFService/*` | WCF `[ServiceContract]` with 10 operations, `basicHttpBinding`, mex/WSDL metadata, EF6 `EntityModel` | gRPC service (internal callers) + REST facade (external), or CoreWCF only if SOAP clients cannot be changed | C2 | **L** |
| 5 | **C5 — MVC Web UI** | `eShopLegacyMVC` (Controllers/Views/Global.asax/bundles) | MVC 5 controllers, Razor v3 views, `ViewBag`, InProc session, bundling, `HandleErrorAttribute` | ASP.NET Core 8 MVC, Razor views ported, tag helpers, exception-handler middleware, static assets bundler | C2, C3 | **L** |
| 6 | **C6 — Web Forms UI** | `eShopLegacyWebForms` | ASPX pages + code-behind, ViewState/postbacks, FriendlyUrls, `<%$RouteUrl%>` expression builders, ScriptManager/MsAjax, Autofac.Web property injection | **No in-place path** — rewrite as Razor Pages on .NET 8, or retire in favour of C5 (the two UIs are functionally equivalent) | C2, C3 | **XL** (rewrite) / **S** (retire) |
| 7 | **C7 — WinForms Desktop Client** | `eShopLegacyNTier/src/eShopWinForms/*` | WinForms UI, generated WCF proxy (`eShopServiceReference`), `App.config` endpoint | .NET 8 Windows Desktop (WinForms is supported, Windows-only) with a generated gRPC/REST client; or retire and fold into the web UI | C4 | **M** |
| 8 | **C8 — Containerization & CI/CD** | `.github/workflows/ci.yml`, new `Dockerfile`s, `docker-compose.yml` | `windows-latest` + msbuild/nuget CI, IIS-hosted deployment, no containers | Linux containers (`mcr.microsoft.com/dotnet/aspnet:8.0`), docker-compose with SQL Server, `dotnet build/test/publish` on ubuntu-latest, image scanning | C3, C5 (C4, C6, C7 as they land) | **M** |

**Effort key:** S ≈ 1 dev-week, M ≈ 2–3, L ≈ 4–6, XL ≈ 8–10. Total ≈ **34 dev-weeks** (≈ 26 if the Web Forms UI is retired instead of rewritten).

## 2. Target architecture

**Target state (.NET 8, Linux containers)**

```
                          ┌──────────────────────────────┐
   browser  ─────────────▶│  eShop.Web (ASP.NET Core 8)  │   C5 (+C6 folded in or retired)
                          │  MVC/Razor Pages, DI, Serilog│
                          └──────────────┬───────────────┘
                                         │ HTTP/JSON (in-proc or REST)
                          ┌──────────────▼───────────────┐
   external clients ─────▶│  eShop.Catalog.Api           │   C3
        REST/JSON         │  ASP.NET Core 8 controllers  │
                          │  OpenAPI, /items/{id}/pic    │
                          └──────────────┬───────────────┘
                                         │
   WinForms / internal ──gRPC──▶┌────────▼─────────────────┐
        clients (C7)            │ eShop.Catalog.Grpc       │  C4  (CoreWCF only if SOAP
                                │ proto-first services     │       clients cannot change)
                                └────────┬─────────────────┘
                                         │
                          ┌──────────────▼───────────────┐
                          │ eShop.Catalog.Data (EF Core) │   C2
                          │ + eShop.Catalog.Domain       │
                          └──────────────┬───────────────┘
                                         │ TDS
                                ┌────────▼────────┐
                                │  SQL Server     │  (container in dev,
                                │  (Azure SQL)    │   managed in prod)
                                └─────────────────┘

   Cross-cutting (C1): Microsoft.Extensions.{Configuration,DependencyInjection,Logging},
   Serilog + OpenTelemetry → Azure Monitor, appsettings.json + env vars + Key Vault,
   System.Text.Json everywhere (no BinaryFormatter), health checks.

   Packaging (C8): one Dockerfile per service, docker-compose (web + api + grpc + mssql),
   GitHub Actions on ubuntu-latest: dotnet restore/build/test/publish → container registry.
```

- **Containerization**: Linux containers throughout; the only Windows artefact left is the optional WinForms client, which is published as a self-contained Windows executable outside the container story.
- **Database**: keep SQL Server (LocalDB → SQL Server container in dev, Azure SQL in prod) and move to EF Core 8 with migrations. The three HiLo sequences carry over as EF Core HiLo generators; no stored procedures or T-SQL business logic exist, so no rewrite of database logic is needed. Consolidate the two schemas (`Microsoft.eShopOnContainers.Services.CatalogDb` and `eShopDatabase`) into one catalog database.
- **Communication**: internal service-to-service over gRPC (replacing SOAP), external over REST/JSON with OpenAPI. SOAP is only preserved (via CoreWCF) if third-party SOAP consumers are discovered during C4 discovery.
- **Configuration**: `Web.config`/`App.config` → `appsettings.json` + environment variables + Key Vault for secrets; the existing `UseMockData`/`UseCustomizationData` switches become typed options, and the WCF service's `ConnectionString` env-var override becomes the standard `ConnectionStrings__Catalog` pattern.

## 3. Ranked migration risks

| Rank | Risk | Impact | Evidence | Mitigation |
| --- | --- | --- | --- | --- |
| R1 | **Web Forms has no migration path** | High — a full rewrite is the single largest work item (XL) | `eShopLegacyWebForms`: ASPX/ViewState/postbacks, ScriptManager, `<%$RouteUrl%>` | Decide early whether to retire it in favour of the MVC UI (both expose the same catalog CRUD); if kept, rewrite as Razor Pages and reuse C2/C3 |
| R2 | **WCF server is unsupported on .NET 8** | High — contract + client rework for 10 operations | `ICatalogService.cs:12`, `basicHttpBinding` + mex | gRPC/REST re-platform with CoreWCF as fallback; inventory all SOAP consumers before choosing |
| R3 | **`BinaryFormatter` is removed in .NET 9 and is a known RCE vector** | High — `GET /api/files` returns a BinaryFormatter payload today (verified at runtime) | `eShopLegacy.Utilities/Serializing.cs:11,19`; consumed by `WebApi/FilesController.cs` | Replace with `System.Text.Json` and version the endpoint; coordinate with any binary consumers |
| R4 | **Triplicated domain/EF model drift** | High — behaviour differences between the three apps are easy to miss | Three `CatalogDBContext`/`EntityModel` copies, EF 6.2.0 vs 6.1.3, different databases | Extract C2 first and force all three front-ends onto it; use the behavioral baseline as the parity oracle |
| R5 | **Windows-only dependencies** | Medium — blocks Linux containers for parts of the estate | IIS/WAS hosting for WCF, WinForms UI, LocalDB (`(localdb)\MSSQLLocalDB`), Integrated Security auth | Move to Kestrel + SQL auth/managed identity; keep WinForms as a Windows-published client or retire it |
| R6 | **Binding redirects masking version conflicts** | Medium — latent runtime failures | 10 redirects in MVC `Web.config`; `Views/Web.config` pins MVC 5.2.3.0 while binaries are 5.2.7.0 (broke immediately when hosted without redirect support) | SDK-style projects + single version per package; remove redirects entirely |
| R7 | **Out-of-date / vulnerable packages** | Medium | `NU1902` for log4net 2.0.10 (GHSA-4f7c-pmjv-c25w); Newtonsoft.Json 6.0.4 in WinForms; App Insights 2.9.1; Autofac 4.9.1 | Drop log4net for Serilog, Newtonsoft for System.Text.Json, App Insights SDK for OpenTelemetry, Autofac for built-in DI |
| R8 | **Hidden couplings in the build** | Medium — refactors silently break tests/clients | Test project *links* MVC source files instead of referencing the project; WinForms consumes a generated WCF proxy; only the MVC solution is in CI | Convert to real project references, generate clients from proto/OpenAPI, extend CI to all solutions before touching code |
| R9 | **Data-layer behaviour not runtime-verified** | Medium — EF6→EF Core parity is unproven | Baseline was captured with `UseMockData=true`; sequences/initializer never executed | Re-run the baseline on Windows + LocalDB (or SQL Server container) before starting C2; add integration tests around the HiLo/seed paths |
| R10 | **No authentication anywhere** | Low for migration, high for production | No `<authentication>`, no `[Authorize]`; all endpoints anonymous (verified) | Treat authn/authz as a net-new workstream after parity is achieved |
