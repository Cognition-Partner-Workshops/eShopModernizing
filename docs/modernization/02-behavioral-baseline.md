# .NET Behavioral Baseline

**Repository:** [Cognition-Partner-Workshops/eShopModernizing](https://github.com/Cognition-Partner-Workshops/eShopModernizing) · **Commit:** `c654bd3` · **Generated:** 2026-07-27 by Devin (.NET Discovery playbook, Phase 2).

Related pages: [.NET Codebase Inventory](./01-codebase-inventory.md) · [Proposed .NET Modernization Boundaries](./03-modernization-boundaries.md) · [.NET Modernization Roadmap](./04-modernization-roadmap.md)

This page is the **parity oracle** for the migration: the observed responses in section 6 are what the modernized application must reproduce.

## 1. Verification status per component

| Component | Status | Evidence / reason |
| --- | --- | --- |
| eShopLegacyMVC (MVC 5 + Web API 2) | **Runtime-verified** | Built and hosted on this Linux VM under Mono; all documented routes exercised over HTTP, full CRUD round-trip (section 6) |
| eShopLegacyMVC.Tests | **Runtime-verified** | `dotnet test … -c Release` → `Passed! Failed: 0, Passed: 48` |
| eShopLegacy.Utilities | **Runtime-verified (indirectly)** | Its `BinaryFormatter` output was captured through `GET /api/files` |
| eShopLegacyWebForms | **Static-only** (compiles, cannot be hosted here) | Compiles under Mono, but hosting fails on framework gaps: `System.Web.UI.ScriptResourceDefinition`/`ScriptResourceMapping` missing, and the ASPX parser rejects `<%$RouteUrl:… %>` expression builders. Needs Windows + IIS Express. |
| eShopWCFService | **Static-only** | WCF server hosting requires IIS/WAS on Windows, and the service has no mock mode — it needs a live SQL Server/LocalDB instance. |
| eShopWinForms | **Static-only** | Windows-only desktop UI; also depends on a running WCF endpoint at `http://localhost:62314/CatalogService.svc`. |

The repository's own CI (`.github/workflows/ci.yml`) runs on `windows-latest` with `msbuild` + `nuget restore`, confirming Windows is the intended toolchain. To promote the three static-only components to runtime-verified, re-run this phase on a Windows agent with IIS Express and LocalDB.

## 2. Service contracts

### 2.1 WCF — `eShopWCFService.ICatalogService` (static-only)

Contract: `src/eShopWCFService/ICatalogService.cs:12` `[ServiceContract]`; implementation `CatalogService.cs`; host `CatalogService.svc`. Binding from `Web.config`:

```
<service name="eShopWCFService.CatalogService">
  <endpoint address="" binding="basicHttpBinding" contract="eShopWCFService.ICatalogService"/>
  <endpoint address="mex" binding="mexHttpBinding" contract="IMetadataExchange"/>
</service>
<serviceMetadata httpGetEnabled="true" httpsGetEnabled="true"/>
<serviceDebug includeExceptionDetailInFaults="false"/>
```

Transport/serialization: SOAP 1.1 over HTTP (`basicHttpBinding`, text/xml, `DataContractSerializer`). Client endpoint (WinForms `App.config`): `http://localhost:62314/CatalogService.svc`, binding `BasicHttpBinding_ICatalogService`. Metadata: `?wsdl` and `/mex`.

| Operation (SOAPAction `…/ICatalogService/{op}`) | Signature | Returns |
| --- | --- | --- |
| FindCatalogItem | `CatalogItem FindCatalogItem(int id)` | single item or null |
| GetCatalogBrands | `List<CatalogBrand> GetCatalogBrands()` | all brands |
| GetCatalogItems | `List<CatalogItem> GetCatalogItems(int brandIdFilter, int typeIdFilter)` | filtered items |
| GetCatalogTypes | `List<CatalogType> GetCatalogTypes()` | all types |
| GetAvailableStock | `int GetAvailableStock(DateTime date, int catalogItemId)` | stock count |
| CreateAvailableStock | `void CreateAvailableStock(CatalogItemsStock stock)` | void |
| CreateCatalogItem | `void CreateCatalogItem(CatalogItem item)` | void |
| UpdateCatalogItem | `void UpdateCatalogItem(CatalogItem item)` | void |
| RemoveCatalogItem | `void RemoveCatalogItem(CatalogItem item)` | void |
| GetDiscount | `DiscountItem GetDiscount(DateTime day)` | discount for a day |

Data contracts (`[DataContract]`/`[DataMember]`): `CatalogBrand`, `CatalogItem`, `CatalogItemsStock`, `CatalogType`, `DiscountItem`.

### 2.2 HTTP surface — eShopLegacyMVC (runtime-verified)

Routing: `App_Start/RouteConfig.cs` (`MapMvcAttributeRoutes()` + default `{controller}/{action}/{id}` → `Catalog/Index`) and `App_Start/WebApiConfig.cs` (`MapHttpAttributeRoutes()` + `api/{controller}/{id}`).

| Verb + route | Handler | Attributes / notes |
| --- | --- | --- |
| GET `/Catalog/Index?pageSize&pageIndex` (also `/`, `/Catalog`) | `CatalogController.Index` | Default route; paginated view model, defaults pageSize=10, pageIndex=0 |
| GET `/Catalog/Details/{id}` | `CatalogController.Details` | 400 when id missing, 404 when not found |
| GET `/Catalog/Create` | `CatalogController.Create` | ViewBag `CatalogBrandId`/`CatalogTypeId` SelectLists |
| POST `/Catalog/Create` | `CatalogController.Create` | `[HttpPost]` `[ValidateAntiForgeryToken]`; redirect on success |
| GET `/Catalog/Edit/{id}` | `CatalogController.Edit` | 400/404 as above |
| POST `/Catalog/Edit` | `CatalogController.Edit` | `[HttpPost]` `[ValidateAntiForgeryToken]` |
| GET `/Catalog/Delete/{id}` | `CatalogController.Delete` | confirmation view |
| POST `/Catalog/Delete` | `CatalogController.DeleteConfirmed` | `[HttpPost, ActionName("Delete")]` `[ValidateAntiForgeryToken]` |
| GET `/items/{catalogItemId:int}/pic` | `PicController.Index` | Attribute route named `GetPicRouteName`; serves bytes from `~/Pics`; 400 for id ≤ 0, 404 if absent; unknown extension → `application/octet-stream` |
| GET `api/Brands` | `WebApi/BrandsController.Get` | JSON list |
| GET `api/Brands/{id}` | `WebApi/BrandsController.Get(int)` | JSON object; 404 when absent |
| DELETE `api/Brands/{id}` | `WebApi/BrandsController.Delete` | **Demo-only: returns success without deleting anything** |
| GET `api/Files` | `WebApi/FilesController.Get` | Maps brands → `BrandDTO` and returns a **`BinaryFormatter` stream** |
| GET `/api` (`[Route("api")] CatalogController2`) | `Controllers/Api/CatalogController.cs` | Declared as an MVC `Controller` (not `ApiController`); **observed 404** — dead code shadowed by the Web API `api/{controller}/{id}` route |

### 2.3 HTTP surface — eShopLegacyWebForms (static-only)

Routes registered in `App_Start/RouteConfig.cs` (FriendlyUrls + page routes):

| Route name | URL | Physical page |
| --- | --- | --- |
| Default | `Default` (and `/`) | `~/Default.aspx` |
| DefaultPaginated | `Default/index/{index}/size/{size}` | `~/Default.aspx` |
| CreateProductRoute | `Catalog/Create` | `~/Catalog/Create.aspx` |
| EditProductRoute | `Catalog/Edit/{id}` | `~/Catalog/Edit.aspx` |
| DetailsProductRoute | `Catalog/Details/{id}` | `~/Catalog/Details.aspx` |
| DeleteProductRoute | `Catalog/Delete/{id}` | `~/Catalog/Delete.aspx` |

Page behaviour: `Page_Load` + `IsPostBack`, `ListView`/data-binding against `ICatalogService` (Autofac property injection), `Response.Redirect` after mutations, `<%$RouteUrl:RouteName=… %>` expression builders for links. Additional static pages: `About.aspx`, `Contact.aspx`, `Site.Master`.

## 3. Data access

| Context | DbSets | Connection string | Initialization |
| --- | --- | --- | --- |
| `eShopLegacyMVC.Models.CatalogDBContext` (`:8`) | `CatalogItems`, `CatalogBrands`, `CatalogTypes` | `name=CatalogDBContext` → `(localdb)\MSSQLLocalDB; Initial Catalog=Microsoft.eShopOnContainers.Services.CatalogDb; Integrated Security=True; MultipleActiveResultSets=True` | `CatalogDBInitializer : CreateDatabaseIfNotExists<CatalogDBContext>`, registered only when `UseMockData=false` |
| `eShopLegacyWebForms.Models.CatalogDBContext` (`:8`) | same three sets | same LocalDB string (same database) | same initializer pattern |
| `eShopWCFService.EntityModel` (`:10`) | `CatalogBrands`, `CatalogItems`, `CatalogItemsStocks`, `CatalogTypes`, `DiscountItems` | env var `ConnectionString` if set, else `EntityModel` → `(localdb)\MSSQLLocalDB; Initial Catalog=eShopDatabase` | `Database.SetInitializer(new CatalogDBInitializer())` in the ctor (always on) |

**Mappings** (`OnModelCreating`, MVC/Web Forms): `CatalogItem`→table `Catalog`, `CatalogBrand`→`CatalogBrand`, `CatalogType`→`CatalogType`; `Id` `DatabaseGeneratedOption.None` (ids come from SQL sequences); required `Name` (maxLength 50), `Description`, `Price`; `PictureUri` is `Ignore`d (computed at runtime).

**Relationships**: `CatalogItem.CatalogBrandId` → `CatalogBrand.Id` (required) and `CatalogItem.CatalogTypeId` → `CatalogType.Id` (required), both with navigation properties; WCF adds `CatalogItemsStock` and `DiscountItem` keyed by item/date.

**Seeding**: `Models/Infrastructure/CatalogDBInitializer.cs` executes the three HiLo sequence scripts (`dbo.catalog_hilo`, `dbo.catalog_brand_hilo`, `dbo.catalog_type_hilo`), seeds types/brands/items (optionally from `Setup/*.csv` when `UseCustomizationData=true`) and extracts `Setup/CatalogItems.zip` into `~/Pics`. Ids are allocated with `CatalogItemHiLoGenerator` via `NEXT VALUE FOR`.

## 4. Cross-cutting concerns

- **Authentication/authorization: none.** No `<authentication>` element, no `[Authorize]` anywhere — every endpoint in all three apps is anonymous. (Confirmed at runtime: all MVC routes answered without credentials.)
- **CSRF**: MVC mutating actions use `[ValidateAntiForgeryToken]`; Web Forms relies on ViewState/EventValidation.
- **Logging**: log4net 2.0.10 configured by `log4Net.xml` (MVC) / `Web.config` (Web Forms); per-request `LogicalThreadContext` properties `activityid` (from `Trace.CorrelationManager.ActivityId`, `Global.asax.cs:99–104`) and `requestinfo`; DEBUG-level entries at `Application_BeginRequest`/`EndRequest`. Application Insights 2.9.1 modules are wired in both web apps (instrumentation key empty by default).
- **Error handling**: MVC registers `HandleErrorAttribute` globally (`App_Start/FilterConfig.cs:10`) and ships `Views/Shared/Error.cshtml`; no `Application_Error` handler and no `<customErrors>` element in any app — unhandled errors surface as the default ASP.NET error page. WCF sets `includeExceptionDetailInFaults="false"`.
- **Caching**: none. No `OutputCache`, `HttpRuntime.Cache` or `MemoryCache` usage; only static-asset bundling (`BundleConfig`, `Bundle.config`).
- **State**: `<sessionState mode="InProc"/>` in both web apps; `Session_Start` stores `MachineName` and `SessionStartTime`; MVC uses `ViewBag` for brand/type SelectLists. A session cookie is issued on every request (`Set-Cookie: ASP.NET_SessionId=…`).
- **Configuration**: `UseMockData` (swap EF6 for `CatalogServiceMock`), `UseCustomizationData` (CSV seed data), connection strings — all in `Web.config`/`App.config`; the WCF service additionally honours the `ConnectionString` environment variable.

## 5. Run configuration used for verification

**Exact commands (Linux VM, disposable copy outside the repo)**

```
# toolchain: mono 6.8.0.105, nuget.exe 6.11.1, .NET SDK (for the SDK-style test project)
mono nuget.exe restore eShopLegacyMVC.sln -Source https://api.nuget.org/v3/index.json
xbuild src/eShopLegacyMVC/eShopLegacyMVC.csproj /p:Configuration=Debug

# app config for the smoke run
Web.config: UseMockData=true          (no SQL Server on this VM)
Web.config: UseCustomizationData=false

# host: System.Web.Hosting.ApplicationHost + HttpListener bridge on http://localhost:8080
mono SmokeHost.exe <web project dir> 8080

# unit tests (repository checkout, unmodified)
dotnet test eShopLegacyMVCSolution/tests/eShopLegacyMVC.Tests/eShopLegacyMVC.Tests.csproj -c Release
```

**Harness deviations from a Windows/IIS Express run** (must be kept in mind when comparing parity results):

- Mono does not apply `<bindingRedirect>`s, so `Views/Web.config` had to be repointed from `System.Web.Mvc, Version=5.2.3.0` to `5.2.7.0` — on Windows the redirect handles this. *This is a real latent config/binary mismatch in the repo.*
- The Application Insights modules were removed from the smoke copy (Mono cannot load `Microsoft.ApplicationInsights.Web.RequestTrackingTelemetryModule`).
- The harness serves the empty root path `""`, which Mono's `VirtualPathUtility` cannot resolve during URL generation, so `GET /` returned 500 in the harness. The same handler reached through its explicit route (`GET /Catalog`, `GET /Catalog/Index`) returns 200 — the 500 is a harness artefact, not app behaviour.
- `UseMockData=true` means data comes from `CatalogServiceMock`/`PreconfiguredData`, so EF6 SQL generation, the HiLo sequences and the DB initializer were **not** exercised at runtime.

## 6. Verified Runtime Behavior

Golden outputs captured 2026-07-27 against `eShopLegacyMVC` (mock-data mode, port 8080). These are the parity assertions for the migrated application.

### 6.1 Endpoint → observed status / response

| Request | Status | Observed response |
| --- | --- | --- |
| `GET /Catalog` | **200** | `text/html`, 14,402 bytes, `<title>Index - Catalog manager (MVC)</title>`, 10 catalog rows, image links `items/{id}/pic`; headers `X-AspNetMvc-Version: 5.2`, `X-AspNet-Version: 4.0.30319`, `Cache-Control: private`, `Set-Cookie: ASP.NET_SessionId=…` |
| `GET /Catalog/Index` | **200** | byte-identical to `/Catalog` (14,402 bytes) |
| `GET /Catalog/Index?pageSize=2&pageIndex=1` | **200** | 5,377 bytes — pagination honoured (2 rows, second page) |
| `GET /Catalog/Details/1` | **200** | 3,220 bytes detail view |
| `GET /Catalog/Details/999` | **404** | unknown id |
| `GET /Catalog/Details` (no id) | **400** | `HttpStatusCodeResult(BadRequest)` |
| `GET /Catalog/Create` | **200** | 7,830 bytes form incl. `__RequestVerificationToken` hidden field |
| `GET /Catalog/Edit/1` | **200** | 9,032 bytes prefilled form |
| `GET /Catalog/Delete/1` | **200** | 3,639 bytes confirmation view |
| `GET /items/1/pic` | **200** | `Content-Type: image/png`, 151,640 bytes (file served from `~/Pics/1.png`) |
| `GET /items/0/pic` | **400** | non-positive id rejected |
| `GET /api/brands` | **200** | `application/json`: `[{"Id":1,"Brand":"Azure"},{"Id":2,"Brand":".NET"},{"Id":3,"Brand":"Visual Studio"},{"Id":4,"Brand":"SQL Server"},{"Id":5,"Brand":"Other"}]` |
| `GET /api/brands/1` | **200** | `{"Id":1,"Brand":"Azure"}` |
| `GET /api/brands/99` | **404** | empty body |
| `GET /api/files` | **200** | 719 bytes of **BinaryFormatter payload** — starts `00 01 00 00 00 FF FF FF FF … "eShopLegacyMVC, Version=1…"`; response is served with `Content-Type: text/html` and `Cache-Control: no-cache` |
| `GET /api` | **404** | the `[Route("api")]` action in `Controllers/Api/CatalogController.cs` is unreachable (dead code) |
| `GET /Content/site.css` | **404** | static files are not served by the managed pipeline (IIS serves them on Windows) — harness limitation, not an app rule |
| `GET /nope` | **404** | default not-found page |

### 6.2 Write path (CRUD round-trip, verified end-to-end)

| Step | Status | Observed |
| --- | --- | --- |
| POST `/Catalog/Create` with valid `__RequestVerificationToken` + cookie | **302** | `Location: /`; the new item ("Smoke Test Item") then appears in `GET /Catalog/Index?pageSize=20` |
| POST `/Catalog/Create` *without* the anti-forgery cookie | **500** | `System.Web.Mvc.HttpAntiForgeryException: The required anti-forgery cookie "__RequestVerificationToken" is not present.` — CSRF protection is active |
| POST `/Catalog/Edit` (id=1, Name="Renamed Hoodie") | **302** | `Location: /`; `GET /Catalog/Details/1` then renders "Renamed Hoodie" |
| POST `/Catalog/Delete` (id=1) | **302** | `Location: /`; `GET /Catalog/Details/1` subsequently returns **404** |

### 6.3 Unit tests

```
$ dotnet test eShopLegacyMVCSolution/tests/eShopLegacyMVC.Tests/eShopLegacyMVC.Tests.csproj -c Release
Passed!  - Failed: 0, Passed: 48, Skipped: 0, Total: 48, Duration: 464 ms - eShopLegacyMVC.Tests.dll (net472)
warning NU1902: Package 'log4net' 2.0.10 has a known moderate severity vulnerability (GHSA-4f7c-pmjv-c25w)
```

### 6.4 Rendered UI

The catalog list page was also loaded in a browser against the running instance: the grid renders product images (served by `/items/{id}/pic`), Name/Description/Brand/Type/Price/Picture name/Stock/Restock/Max stock columns and per-row *Edit | Details | Delete* links, with a "Create New" link above the table — matching `Views/Catalog/Index.cshtml` + `CatalogTable.cshtml`.

### 6.5 Static-analysis findings confirmed or corrected at runtime

- **Confirmed**: anti-forgery enforcement on all three mutating actions; 400/404 argument handling in `CatalogController` and `PicController`; pagination semantics; brand JSON shape; `BinaryFormatter` on `/api/files`; anonymous access everywhere.
- **Corrected**: `Controllers/Api/CatalogController.cs` (`[Route("api")]`) is *not* reachable — it returns 404 and should be deleted rather than migrated.
- **New**: the `Views/Web.config` MVC 5.2.3.0 pin vs. 5.2.7.0 binaries only works because of a binding redirect; this class of masked mismatch disappears once the app is rebuilt as SDK-style ASP.NET Core.
