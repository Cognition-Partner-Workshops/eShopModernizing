# Modernized .NET 8 solution (`eShop.sln`)

This folder documents the modernized estate that replaces the legacy .NET Framework solutions. The
remaining legacy solutions stay in the repository and keep building on Windows until each component
has been cut over. The Web Forms solution has already been retired and deleted — see
[`webforms-retirement.md`](webforms-retirement.md).

## Layout

```
eShop.sln                       modernized solution (all projects are net8.0)
global.json                     pins the .NET 8 SDK feature band
Directory.Build.props           shared MSBuild settings for src/ and tests/
Directory.Packages.props        central package management: one version per package
src/
  eShop.Catalog.Domain          canonical domain model, no persistence dependencies
  eShop.Catalog.Data            ICatalogService + in-memory implementation (EF Core 8 in NET-64)
  eShop.Catalog.Api             ASP.NET Core HTTP API: brands, files, item pictures, OpenAPI
  eShop.Catalog.Grpc            gRPC port of the WCF ICatalogService (NET-66, grpc-contract.md)
  eShop.Web                     ASP.NET Core MVC skeleton       (UI ported in NET-69)
  eShop.Shared                  cross-cutting foundation: options, logging, serialization
                                (filled in by NET-61 / NET-62 / NET-63)
tests/
  eShop.Catalog.Domain.Tests    xUnit
  eShop.Catalog.Data.Tests      xUnit
  eShop.Catalog.Api.Tests       xUnit + WebApplicationFactory in-process host
  eShop.Catalog.Grpc.Tests      xUnit + in-process gRPC client over the test server
```

Project reference direction (never invert it):

```
Domain  ←  Data  ←  Api / Grpc / Web
   ↑        ↑
   └──── Shared ────┘
```

## Build and test on Linux

```bash
# .NET 8 SDK (any 8.0.x feature band ≥ 8.0.100)
dotnet build eShop.sln
dotnet test eShop.sln
```

Both commands must be run from the repository root and must be green before any PR is opened.
The legacy solutions are **not** part of `eShop.sln` and still require Windows + MSBuild.

## Conventions the follow-on tickets must respect

1. **One solution, one target framework.** `TargetFramework` is set centrally in
   `Directory.Build.props`; individual projects do not set it. The only expected exception is a
   future `net8.0-windows` WinForms client (NET-68), which overrides it locally.
2. **Central package management.** Every package version lives in `Directory.Packages.props`.
   Projects reference packages without a `Version` attribute. Never add a second version of a
   package; upgrade the single entry instead.
3. **No binding redirects, no `packages.config`, no `BinaryFormatter`** anywhere under `src/` or
   `tests/`.
4. **Nullable reference types and `TreatWarningsAsErrors` are on.** Fix warnings; do not suppress
   them project-wide.
5. **Root props files are scoped.** `Directory.Build.props` and `Directory.Packages.props` sit at
   the repository root (so MSBuild imports them for the legacy projects too) and therefore apply
   their settings only to projects under `src/` and `tests/`. Keep that guard in place while the
   legacy solutions exist.
6. **Configuration comes from `appsettings.json` + environment variables.** No connection strings
   or secrets in source; the legacy `ConnectionString` env-var override becomes
   `ConnectionStrings__Catalog`.
7. **New projects** go under `src/` or `tests/`, are added to `eShop.sln`, and follow the
   `eShop.<Area>[.<Layer>]` / `<project>.Tests` naming.
8. **Domain stays persistence-free.** `eShop.Catalog.Domain` must never reference EF Core, EF6 or
   ASP.NET types; mapping belongs in `eShop.Catalog.Data`.

## Serialization (NET-63)

`BinaryFormatter` is banned in the modernized estate (risk R3). Two gates enforce it:

* **Build-time:** `Microsoft.CodeAnalysis.BannedApiAnalyzers` is referenced from
  `Directory.Build.props` for every project under `src/` and `tests/`, with the banned symbols in
  `BannedSymbols.txt` at the repository root and `RS0030` escalated to an error.
* **CI:** `scripts/check-no-binaryformatter.sh` greps `src/` and `tests/` (defaults; accepts paths)
  and exits non-zero on any `BinaryFormatter` / `SoapFormatter` / `NetDataContractSerializer`
  reference, including in non-compiled files.

Replacement helpers live in `eShop.Shared`:

* `eShop.Shared.Serialization.JsonSerialization` — stream/string/UTF-8 `System.Text.Json` helpers
  mirroring the legacy `Serializing` shape (object in → rewound readable stream out).
* `eShop.Shared.Serialization.JsonDefaults.Options` — the single shared options instance;
  property names keep their declared PascalCase so the JSON matches the legacy Web API 2 payloads.
* `eShop.Shared.Contracts.BrandDto` / `BrandDtoSerializer` — the JSON contract for the modernized
  `GET /api/files` response, matching the logical payload of the captured binary golden output.
  NET-67 implements the endpoint on top of it.

The legacy `eShopLegacyMVCSolution/eShopLegacy.Utilities/Serializing.cs` is deliberately left in
place: the still-live legacy `FilesController` uses it. It goes away when the legacy MVC app is
deleted.

## Domain reconciliation notes (NET-60)

The MVC, Web Forms and WCF applications each carried their own copy of the model. The canonical
types in `eShop.Catalog.Domain` resolve the differences as follows:

| Type | Difference between the legacy copies | Canonical decision |
| --- | --- | --- |
| `CatalogItem` | WCF names the picture field `Picturefilename`; MVC/Web Forms use `PictureFileName` | `PictureFileName` |
| `CatalogItem` | WCF has no `PictureUri`, `AvailableStock`, `RestockThreshold`, `MaxStockThreshold`, `OnReorder` | keep the MVC/Web Forms superset |
| `CatalogItem` | `Price` range is `[0, 1000000]` (MVC) vs `[0, 9999999999999999.99]` (Web Forms) | MVC range — the MVC app is the parity oracle |
| `CatalogItem` | `[Required]` on `Name` in MVC only | keep `[Required]` |
| `CatalogItem` | Web Forms reuses the "Stock" error message for restock/max stock | MVC's distinct messages |
| `CatalogItem` | WCF marks `Price` as `money`, ids as `DatabaseGeneratedOption.None` | dropped — persistence concerns move to EF Core mapping in NET-64 |
| `CatalogBrand` / `CatalogType` | `[StringLength(50)]` in the WCF copy only | kept, it matches the database schema |
| all | `[DataContract]` / `[DataMember]` (WCF), `System.Data.Entity.Spatial` usings | dropped — gRPC contracts are generated from `.proto` in NET-66 |
| `PaginatedItemsViewModel<T>` | MVC/Web Forms view-model | moved to the domain as `PaginatedItems<T>` with identical semantics |

`CatalogItemsStock` and `DiscountItem` exist only in the WCF service and were carried over as-is
(minus the EF6/WCF attributes).

## Data layer (NET-64)

`eShop.Catalog.Data` now owns the EF Core 8 port of the three EF6 contexts:

```
CatalogDbContext                       DbSets for the five entities, ApplyConfigurationsFromAssembly
Configurations/*Configuration.cs       IEntityTypeConfiguration<T>, one per entity
CatalogService.cs                      EF Core ICatalogService (LongCount/Include/OrderBy/Skip/Take)
MockCatalogService.cs                  in-memory ICatalogService (unchanged)
ICatalogItemIdGenerator.cs             seam for the HiLo generator (NET-65)
Migrations/…_InitialCreate.cs          SQL Server schema equivalent to the legacy database
CatalogDataServiceCollectionExtensions AddCatalogData(IConfiguration)
```

Hosts wire the layer up with a single call:

```csharp
builder.Services.AddCatalogData(builder.Configuration);
```

`UseMockData=true` selects `MockCatalogService`; otherwise the SQL Server context is registered
from the `Catalog` connection string (`ConnectionStrings__Catalog`), and a missing connection
string fails fast at startup.

### Legacy mapping divergences resolved

The MVC/Web Forms `CatalogDBContext` creates the database the behavioral baseline was captured
against, so its shape wins wherever the reverse-engineered WCF `EntityModel` disagrees:

| Mapping | MVC / Web Forms | WCF | Ported model |
| --- | --- | --- | --- |
| `Catalog.Price` | EF6 default for `decimal` → `decimal(18,2)` | `money`, precision (19,4) | `decimal(18,2)` |
| `CatalogBrand` / `CatalogType` table | `CatalogBrand` / `CatalogType` (explicit `ToTable`) | pluralized by convention | singular, as in MVC |
| `CatalogBrand.Brand` / `CatalogType.Type` | required `nvarchar(100)` | `varchar(50)` (`IsUnicode(false)`) | required `nvarchar(100)` |
| `CatalogBrand.Id` / `CatalogType.Id` | identity | `DatabaseGeneratedOption.None` | identity |
| `Catalog.Id` | `DatabaseGeneratedOption.None` | same | `ValueGeneratedNever()` |
| `CatalogItemsStock`, `DiscountItems` | absent | WCF-only entities | folded in unchanged (`date` columns, `StockId` application-assigned, `DiscountItems` pluralized as EF6 named it) |

`CatalogItem.PictureUri` stays unmapped (`Ignore`), exactly as in EF6.

### Migration and seeding

`InitialCreate` is the SQL Server migration for the schema above. It was applied to an empty
SQL Server 2022 database and the resulting tables/columns/foreign keys match the legacy schema.

Seeding and the HiLo sequences landed in NET-65 (below).

### Tests

`eShop.Catalog.Data.Tests` covers the mock service, the EF Core service against a SQLite
in-memory database (CRUD, eager-loaded navigations, pagination), the model metadata (table names,
key generation, lengths, column types, required FKs), `AddCatalogData`, and the NET-65 id
allocation and seeding behaviour. SQLite is used rather
than a SQL Server testcontainer so the suite runs unattended on a Linux CI agent; the SQL Server
migration is verified out-of-band as described above.

## Id generation, seeding and database consolidation (NET-65)

```
Sequences/CatalogSequences.cs               the three legacy sequence definitions (start 1, increment 10)
Sequences/ICatalogSequenceProvider.cs       NEXT VALUE FOR, per provider
Sequences/SqlServerCatalogSequenceProvider  the real thing
Sequences/SqliteCatalogSequenceProvider     table-backed stand-in (SQLite has no sequence objects)
Sequences/HiLoCatalogItemIdGenerator.cs     port of the legacy CatalogItemHiLoGenerator
Seeding/CatalogDatabaseInitializer.cs       port of CatalogDBInitializer, idempotent
Seeding/CatalogCsvSeedData.cs               port of the Setup/*.csv readers
Seeding/CatalogSeedOptions.cs               Catalog:UseCustomizationData / SetupDirectory / PicturesDirectory
Setup/*.csv                                 the customization data, copied from the MVC application
Migrations/…_AddCatalogHiLoSequences.cs     CREATE SEQUENCE dbo.catalog_{,brand_,type_}hilo
```

`Catalog.Id` stays `ValueGeneratedNever`: the application allocates it, taking one
`NEXT VALUE FOR dbo.catalog_hilo` per block of ten and counting up in memory in between, under a
lock — the same block-of-ten, gap-tolerant behaviour as the legacy generator, so a restart abandons
the tail of the current block. `MaxCatalogItemIdGenerator` is gone.

`CatalogBrand.Id` / `CatalogType.Id` are identity columns (that is the legacy MVC schema), so the
store numbers them. The legacy initializer *did* read `catalog_brand_hilo` / `catalog_type_hilo`
and assign the values, but EF6 discarded them on the way to an identity column and SQL Server
rejects them outright; the seeded ids are 1..n either way. Both sequences are still created by the
migration so the schema matches the legacy database.

Seeding runs from `CatalogDatabaseInitializerHostedService` at startup (migrate → seed → extract
pictures) and is idempotent: each table is seeded only when it is empty. Set
`Catalog:InitializeDatabaseOnStartup=false` to boot without touching the database.
`Catalog:UseCustomizationData=true` seeds from `Setup/*.csv` and extracts `Setup/CatalogItems.zip`
into `Catalog:PicturesDirectory` instead of using the hard-coded data.

Consolidating the two legacy databases onto one is
`scripts/sql/consolidate-catalog-databases.sql`; the runbook is
[modernization/database-consolidation.md](database-consolidation.md).

Verified out-of-band against a SQL Server 2022 container (the SQLite suite cannot cover sequence
objects or identity inserts): the migration creates the three sequences as `bigint START WITH 1
INCREMENT BY 10`, a fresh database seeds types 1–4 / brands 1–5 / items 1–12 (or 6 / 7 / 13 with
the customization data), re-running the initializer changes nothing, the next item id is 13, and
the consolidation script is idempotent and leaves `catalog_hilo` past `MAX(Catalog.Id)`.

## HTTP API (NET-67)

`eShop.Catalog.Api` now hosts the port of the legacy Web API 2 / `PicController` surface. Every
response was matched against the golden outputs in section 6.1 of the Behavioral Baseline.

| Endpoint | Behaviour |
| --- | --- |
| `GET /api/brands` | 200, `application/json`, `[{"Id":1,"Brand":"Azure"},…]` |
| `GET /api/brands/{id}` | 200 with the brand, 404 with an **empty body** when it does not exist |
| `GET /api/files` | 200, `application/json`, `BrandDto[]` via `BrandDtoSerializer` (NET-63) |
| `GET /items/{catalogItemId:int}/pic` | 200 with the picture's content type, 400 for `id <= 0`, 404 for an unknown item or a missing file, `application/octet-stream` for unknown extensions |
| `GET /swagger/v1/swagger.json`, `GET /swagger` | OpenAPI document and UI (Swashbuckle) |

Contract notes:

* **Property names stay PascalCase.** `AddControllers().AddJsonOptions(…)` copies
  `JsonDefaults.Options`, so the ASP.NET Core camelCase default does not change the payloads.
* **No `ProblemDetails`.** `ApiBehaviorOptions.SuppressMapClientErrors = true` keeps the legacy
  empty 400/404 bodies instead of the `[ApiController]` problem+json bodies.
* **`GET /api/files` is JSON, not `BinaryFormatter`.** Intentional, documented contract change
  (risk R3); the logical payload — the same five `{Id, Brand}` pairs — is unchanged.
* **`DELETE /api/brands/{id}` was dropped, not ported.** The legacy action never deleted anything
  (it returned 200/404 for a demo) and `ICatalogService` has no brand-removal operation, so porting
  it would either ship a lying endpoint or push a write path into the data layer that no consumer
  asks for. The route now answers 405; a real delete can be added with the data-layer support when
  a consumer needs it.
* **Pictures no longer need `System.Web`.** `Server.MapPath("~/Pics")` becomes
  `CatalogPictureStore` over the `Pictures:RootPath` setting (default `Pics`, relative paths resolve
  against the content root). The images live in `src/eShop.Catalog.Api/Pics` and are copied to the
  output/publish folder. The legacy copy under `eShopLegacyMVCSolution/…/Pics` stays until the
  legacy MVC app is deleted.

Legacy dead code: `eShopLegacyMVCSolution/src/eShopLegacyMVC/Controllers/Api/CatalogController.cs`
(`CatalogController2`, `[Route("api")]`) is verified unreachable — the baseline records `GET /api`
returning 404 because the Web API `api/{controller}/{id}` route shadows it. It is deliberately **not**
ported, and it should be deleted together with the legacy MVC application in the NET-69/NET-70
cutover.

## Logging, telemetry and health (NET-62)

`eShop.Shared` provides the cross-cutting observability helpers used by every host
(`eShop.Web`, `eShop.Catalog.Api`, `eShop.Catalog.Grpc`):

| Extension | Effect |
| --- | --- |
| `builder.UseEShopLogging("<service>")` | Serilog behind `Microsoft.Extensions.Logging`: compact JSON to the console plus an optional rolling file sink |
| `app.UseEShopRequestLogging()` | one structured completion event per HTTP request |
| `builder.AddEShopTelemetry("<service>")` | OpenTelemetry traces + metrics (ASP.NET Core, HttpClient), Azure Monitor exporter only when configured |
| `services.AddEShopHealthChecks()` / `app.MapEShopHealthChecks()` | `/health` (liveness) and `/ready` (readiness, checks tagged `ready`) |

Legacy mapping: `log4Net.xml`'s `RollingFileAppender` (`logFiles\myapp.log`, 10 MB, 5 backups)
becomes the Serilog file sink with the same defaults, and the
`LogicalThreadContext.Properties["activityid"] / ["requestinfo"]` correlation becomes W3C trace
context taken from `Activity.Current` (`CorrelationId`, `TraceId`, `SpanId`, `TraceParent` on every
log event). The Application Insights 2.9.1 `System.Web` HTTP modules are replaced by OpenTelemetry.

Configuration (all optional, `appsettings.json` or environment variables):

| Key | Default | Notes |
| --- | --- | --- |
| `EShopLogging:MinimumLevel` | `Information` | `ALL` / `Verbose` reproduces the legacy log4net root level |
| `EShopLogging:MicrosoftMinimumLevel` | `Warning` | level for `Microsoft.*` / `System.*` |
| `EShopLogging:FilePath` | `logFiles/myapp.log` | empty disables the file sink (console-only containers) |
| `EShopLogging:FileSizeLimitBytes` | `10485760` | legacy `maximumFileSize` |
| `EShopLogging:RetainedFileCountLimit` | `5` | legacy `maxSizeRollBackups` |
| `Telemetry:AzureMonitorConnectionString` or `APPLICATIONINSIGHTS_CONNECTION_STRING` | unset | when absent no exporter is registered and startup still succeeds |
