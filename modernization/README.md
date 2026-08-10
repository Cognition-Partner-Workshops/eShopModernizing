# Modernized .NET 8 solution (`eShop.sln`)

This folder documents the modernized estate that replaces the three legacy .NET Framework
solutions. The legacy solutions stay in the repository and keep building on Windows until each
component has been cut over.

## Layout

```
eShop.sln                       modernized solution (all projects are net8.0)
global.json                     pins the .NET 8 SDK feature band
Directory.Build.props           shared MSBuild settings for src/ and tests/
Directory.Packages.props        central package management: one version per package
src/
  eShop.Catalog.Domain          canonical domain model, no persistence dependencies
  eShop.Catalog.Data            ICatalogService + in-memory implementation (EF Core 8 in NET-64)
  eShop.Catalog.Api             ASP.NET Core HTTP API skeleton  (endpoints in NET-67)
  eShop.Catalog.Grpc            ASP.NET Core gRPC skeleton      (contract in NET-66)
  eShop.Web                     ASP.NET Core MVC skeleton       (UI ported in NET-69)
  eShop.Shared                  cross-cutting foundation: options, logging, serialization
                                (filled in by NET-61 / NET-62 / NET-63)
tests/
  eShop.Catalog.Domain.Tests    xUnit
  eShop.Catalog.Data.Tests      xUnit
  eShop.Catalog.Api.Tests       xUnit + WebApplicationFactory in-process host
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
