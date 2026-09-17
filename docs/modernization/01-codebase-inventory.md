# .NET Codebase Inventory

**Repository:** [Cognition-Partner-Workshops/eShopModernizing](https://github.com/Cognition-Partner-Workshops/eShopModernizing) · **Commit analysed:** `c654bd3` · **Generated:** 2026-07-27 by Devin (.NET Discovery playbook, Phase 1).

Related pages: [.NET Behavioral Baseline](./02-behavioral-baseline.md) · [Proposed .NET Modernization Boundaries](./03-modernization-boundaries.md) · [.NET Modernization Roadmap](./04-modernization-roadmap.md)

## 1. Executive summary

The repository contains **three independent legacy application families** in three solutions and **six projects**. Everything is classic .NET Framework (4.6.1–4.7.2); **no project targets .NET Core / .NET 5+ / .NET Standard**. Five of six projects are legacy non-SDK `.csproj` with `packages.config`; only the MVC unit-test project is SDK-style. Total hand-written source ≈ **6,100 LOC** (≈ 7,600 including designer/generated files).

## 2. Solutions

| Solution | Projects | Purpose |
| --- | --- | --- |
| `eShopLegacyMVCSolution/eShopLegacyMVC.sln` | eShopLegacyMVC, eShopLegacy.Utilities, eShopLegacyMVC.Tests | ASP.NET MVC 5 + Web API 2 catalog manager (the CI-built solution) |
| `eShopLegacyWebFormsSolution/eShopLegacyWebForms.sln` | eShopLegacyWebForms | ASP.NET Web Forms catalog manager (same domain, different UI stack) |
| `eShopLegacyNTier/eShopLegacyNTier.sln` | eShopWCFService, eShopWinForms | WCF SOAP service + WinForms desktop client |

## 3. Project inventory

| Project | Type | Framework | Style | Key dependencies | LOC (hand-written / total) | Migration complexity |
| --- | --- | --- | --- | --- | --- | --- |
| `src/eShopLegacyMVC/eShopLegacyMVC.csproj` | ASP.NET MVC 5 + Web API 2 web app | net472 | Legacy non-SDK, packages.config (42 pkgs) | MVC 5.2.7, WebApi 5.2.7, EF 6.2.0, Autofac 6.1.0 (+ Mvc5/WebApi2 integration), log4net 2.0.10, App Insights 2.9.1, Web.Optimization 1.1.3, Newtonsoft.Json 12.0.1 | 2,011 / 2,048 | **High** |
| `eShopLegacy.Utilities/eShopLegacy.Utilities.csproj` | Class library | net472 | Legacy non-SDK | BCL only (`BinaryFormatter`) | 24 / 60 | **Low** (but security-relevant) |
| `tests/eShopLegacyMVC.Tests/eShopLegacyMVC.Tests.csproj` | MSTest unit tests | net472 | **SDK-style**, PackageReference | MSTest 3.x, Moq, MVC 5, log4net | 673 / 673 | **Low** |
| `src/eShopLegacyWebForms/eShopLegacyWebForms.csproj` | ASP.NET Web Forms web app | net472 | Legacy non-SDK, packages.config (44 pkgs) | EF 6.2.0, Autofac 4.9.1 + Autofac.Web, log4net 2.0.10, App Insights 2.9.1, FriendlyUrls 1.0.2, ScriptManager/MsAjax 5.0.0, Web.Optimization 1.1.3 | 1,936 / 2,363 | **High** (no ASP.NET Core equivalent for Web Forms) |
| `src/eShopWCFService/eShopWCFService.csproj` | WCF service (IIS-hosted .svc) | net461 | Legacy non-SDK, packages.config (1 pkg) | EF 6.1.3, `System.ServiceModel`, `System.ServiceModel.Web` | 692 / 728 | **High** (WCF server not supported on modern .NET) |
| `src/eShopWinForms/eShopWinForms.csproj` | WinForms desktop client | net47 | Legacy non-SDK, packages.config (3 pkgs) | EF 6.1.3, WebApi.Client 5.2.3, Newtonsoft.Json 6.0.4, generated WCF service reference | 776 / 1,758 | **Medium** (Windows-only by definition) |

LOC counts `.cs/.cshtml/.aspx/.ascx/.asax/.svc`; "hand-written" excludes `*.Designer.cs`, `AssemblyInfo.cs` and the generated service `Reference.cs`.

## 4. Configuration files

| Application | Files |
| --- | --- |
| MVC | `Web.config`, `Web.Debug.config`, `Web.Release.config`, `Views/Web.config`, `packages.config`, `log4Net.xml`, `ApplicationInsights.config` |
| Web Forms | `Web.config`, `Web.Debug.config`, `Web.Release.config`, `packages.config`, `Bundle.config`, `ApplicationInsights.config` |
| WCF | `Web.config`, `Web.Debug.config`, `Web.Release.config`, `packages.config` |
| WinForms | `App.config`, `packages.config` |
| Repo-wide | `.github/workflows/ci.yml` (windows-latest, msbuild + nuget restore + dotnet test) |

**Absent (confirmed):** no `.slnx`, no `Directory.Build.props`, no `Directory.Packages.props` (no central package management), no `global.json`, no `appsettings*.json`, no `launchSettings.json`, no `Dockerfile`/`docker-compose`, no `.fsproj`/`.vbproj`.

## 5. Modernization patterns present (file:line evidence)

| Pattern | Evidence | Target |
| --- | --- | --- |
| `System.Web.Mvc` controllers | `src/eShopLegacyMVC/Controllers/CatalogController.cs:10`, `Controllers/PicController.cs`, `Controllers/Api/CatalogController.cs` | ASP.NET Core MVC controllers / Razor Pages |
| `System.Web.Http.ApiController` | `Controllers/WebApi/BrandsController.cs:12`, `Controllers/WebApi/FilesController.cs:11` | ASP.NET Core controllers / minimal APIs |
| Web Forms pages + FriendlyUrls routing | `Default.aspx`, `Catalog/{Create,Edit,Details,Delete}.aspx`, `App_Start/RouteConfig.cs` | Razor Pages (rewrite; no in-place path) |
| `System.ServiceModel` (WCF) | `eShopWCFService/ICatalogService.cs:12` (`[ServiceContract]`), `:15,17,…` (`[OperationContract]` ×10), `Web.config` `<system.serviceModel>` | gRPC (internal) or REST minimal API (external) |
| `System.Data.Entity` (EF6) contexts ×3 | `eShopLegacyMVC/Models/CatalogDBContext.cs:8`, `eShopLegacyWebForms/Models/CatalogDBContext.cs:8`, `eShopWCFService/EntityModel.cs:10` | EF Core `DbContext` |
| `Global.asax` startup | `eShopLegacyMVC/Global.asax.cs:27`, `eShopLegacyWebForms/Global.asax.cs:29` | `Program.cs` + middleware pipeline |
| Autofac DI | `eShopLegacyMVC/Global.asax.cs:62,69` (`ContainerBuilder`, `ApplicationModule`), Web Forms `Global.asax.cs` property injection | `Microsoft.Extensions.DependencyInjection` |
| log4net + `Trace.CorrelationManager` | `eShopLegacyMVC/Global.asax.cs:7,99–104`, `log4Net.xml` | `Microsoft.Extensions.Logging` / Serilog + `Activity`/OpenTelemetry |
| `BinaryFormatter` serialization | `eShopLegacy.Utilities/Serializing.cs:11,19`, consumed by `Controllers/WebApi/FilesController.cs` | System.Text.Json (BinaryFormatter is removed in .NET 9) |
| Web.config connection strings + appSettings | MVC/Web Forms `Web.config` (`CatalogDBContext`, `UseMockData`, `UseCustomizationData`), WCF `Web.config` (`EntityModel`), WinForms `App.config` | `appsettings.json` + env vars / Key Vault |
| Assembly binding redirects (10 in MVC alone) | MVC `Web.config`: Newtonsoft.Json→12.0.0.0, Autofac→6.1.0.0, System.Web.Mvc→5.2.7.0, System.Web.Http→5.2.7.0, WebGrease, Antlr3, DiagnosticSource, Web.Helpers, Web.WebPages, Web.Optimization | Removed by SDK-style projects + unified package versions |
| ASP.NET bundling/minification | `App_Start/BundleConfig.cs` (both web apps), Web Forms `Bundle.config` | Static assets / WebOptimizer / bundler of choice |
| InProc session state | `Global.asax.cs Session_Start` (both web apps) storing `MachineName`, `SessionStartTime` | Stateless, or distributed cache session |
| SQL Server sequences / HiLo id generation | `Models/Infrastructure/dbo.catalog_hilo.Sequence.sql`, `dbo.catalog_brand_hilo…`, `dbo.catalog_type_hilo…` (MVC and Web Forms) | EF Core HiLo/identity strategy |
| Application Insights HTTP modules | `ApplicationInsights.config` + `<system.webServer>/<modules>` in both web apps | OpenTelemetry / Azure Monitor exporter |
| Windows-only surfaces | WCF server hosting (IIS), WinForms UI (`eShopWinForms/Program.cs`), LocalDB connection strings | Linux containers + web/desktop replacement decision |

**Explicitly absent:** no `.NET Remoting`, no COM interop, no MSMQ, no Windows Registry access, no stored procedures (only the three sequence scripts above), no gRPC/proto files, no Blazor/minimal API/worker services, no project already on modern .NET.

## 6. Project dependency graph

**Project references (DAG — no cycles)**

```
eShopLegacyMVC.sln
  eShopLegacyMVC (web)  ──ProjectReference──▶ eShopLegacy.Utilities (lib)
  eShopLegacyMVC.Tests  ──compile-links source files of eShopLegacyMVC──▶ (no ProjectReference)

eShopLegacyWebForms.sln
  eShopLegacyWebForms (web)   [no project references]

eShopLegacyNTier.sln
  eShopWinForms (desktop) ──WCF service reference (generated proxy, not a ProjectReference)──▶ eShopWCFService

Runtime coupling (not compile-time):
  eShopLegacyMVC ─┐
  eShopLegacyWebForms ─┼─▶ SQL Server / LocalDB "MSSQLLocalDB" (separate DBs, same schema shape)
  eShopWCFService ─┘
```

**Cycle analysis:** the graph is acyclic. Only one true `ProjectReference` edge exists (MVC → Utilities). The test project deliberately *links* source files from the web project instead of referencing it, and the WinForms client is coupled to WCF only through a generated proxy — both are hidden couplings that the migration must make explicit.

## 7. Notable inventory findings

- **Duplicated domain**: the catalog model (`CatalogItem`, `CatalogBrand`, `CatalogType`), EF mapping and seed/initializer code is copy-pasted across MVC, Web Forms and WCF — three divergent copies to converge on one shared library.
- **Version drift**: EF 6.2.0 (MVC/Web Forms) vs 6.1.3 (WCF/WinForms); Autofac 6.1.0 vs 4.9.1; Newtonsoft.Json 12.0.1 vs 6.0.4.
- **Known-vulnerable package**: `dotnet restore` reports `NU1902: log4net 2.0.10 has a known moderate severity vulnerability` (GHSA-4f7c-pmjv-c25w) — used by both web apps.
- **Config/binary mismatch masked by redirects**: `Views/Web.config` pins `System.Web.Mvc, Version=5.2.3.0` while the bin output is 5.2.7.0; on Windows the `bindingRedirect` hides this, and it breaks immediately on any runtime without that redirect support (observed during the runtime baseline).
- **Only the MVC solution is covered by CI** (`.github/workflows/ci.yml`, `windows-latest`); Web Forms and the N-Tier solution are not built or tested anywhere.
- **Test coverage**: 48 MSTest tests exist for the MVC app only (services, controllers, view models). They pass — see the Behavioral Baseline.
