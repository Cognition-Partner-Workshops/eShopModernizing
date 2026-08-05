# eShop Modernized (.NET 8)

Target-state solution for the eShop modernization. Everything under `eShopModernized/` is
SDK-style, cross-platform and targets `net8.0`. The legacy solutions
(`eShopLegacyMVCSolution/`, `eShopLegacyWebFormsSolution/`, `eShopLegacyNTier/`) are untouched
and keep building side by side until each component is cut over.

## Layout

```
eShopModernized/
  eShop.sln
  Directory.Build.props           shared MSBuild settings (net8.0, nullable, implicit usings,
                                  LangVersion=latest, TreatWarningsAsErrors)
  Directory.Packages.props        central package management — one version per package
  src/eShop.Catalog.Domain/       entities + paging model, no EF/System.Data.Entity dependency
  src/eShop.Shared/               cross-cutting concerns (configuration, DI, logging, JSON,
                                  health checks) — placeholder, filled in by later tickets
  src/eShop.Catalog.Data/         EF Core 8 data access — placeholder, filled in by later tickets
  src/eShop.Catalog.Api/          ASP.NET Core 8 REST API (+ Swagger/OpenAPI)
  src/eShop.Catalog.Grpc/         gRPC service replacing the WCF endpoint
  src/eShop.Web/                  ASP.NET Core 8 MVC UI
  tests/                          one xunit project per src project
```

## Domain model

`eShop.Catalog.Domain` holds the single canonical copy of the model that the legacy estate
duplicated three times (MVC, Web Forms and the WCF service):

| Type | Source of the canonical shape |
| --- | --- |
| `CatalogItem` | `eShopLegacyMVC/Models/CatalogItem.cs` (superset of the other two copies) |
| `CatalogBrand`, `CatalogType` | identical in all three copies |
| `CatalogItemsStock`, `DiscountItem` | only existed in the WCF model |
| `PaginatedItemsViewModel<T>` | `eShopLegacyMVC/ViewModel/PaginatedItemsViewModel.cs` |

Property names and types are unchanged so serialized shapes stay identical to the behavioral
baseline (for example `{"Id":1,"Brand":"Azure"}`). `[DataContract]`/`[DataMember]` and EF6
mapping attributes (`[Table]`, `[Column]`, `[Key]`, `[DatabaseGenerated]`) are dropped —
persistence mapping belongs to `eShop.Catalog.Data`; validation/display annotations are kept.

## Build, test, run

Requires the .NET 8 SDK (`dotnet --list-sdks` must show an `8.0.x`):

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0
export PATH="$HOME/.dotnet:$PATH"
```

```bash
dotnet build eShopModernized/eShop.sln -c Release
dotnet test  eShopModernized/eShop.sln -c Release
dotnet format eShopModernized/eShop.sln --verify-no-changes   # lint gate

dotnet run --project eShopModernized/src/eShop.Catalog.Api    # REST API + /health + Swagger UI
dotnet run --project eShopModernized/src/eShop.Catalog.Grpc   # gRPC host
dotnet run --project eShopModernized/src/eShop.Web            # MVC UI
```

## Containers

The whole stack — SQL Server 2022 plus the three services — runs in Linux containers with no local
.NET SDK and no LocalDB:

```bash
cd eShopModernized
docker compose build
docker compose up -d --wait      # waits until every container is healthy

curl http://localhost:8080/health                            # web
curl http://localhost:8081/health                            # catalog API
curl --http2-prior-knowledge http://localhost:8082/health    # gRPC (HTTP/2 only)
curl http://localhost:8081/api/brands                        # seeded data
grpcurl -plaintext localhost:8082 list

docker compose down -v           # stop and drop the database volume
```

| Service | Host port | Notes |
| --- | --- | --- |
| `web` | 8080 | ASP.NET Core 8 MVC UI |
| `catalog-api` | 8081 | REST API; the **only** service that migrates and seeds the database |
| `catalog-grpc` | 8082 | HTTP/2 only — probe it with `--http2-prior-knowledge` |
| `sqlserver` | 1433 | `mcr.microsoft.com/mssql/server:2022-latest`, data in the `catalog-db` volume |

Each image is multi-stage (`sdk:8.0` build → `aspnet:8.0` runtime, no SDK in the final layer), runs
as the non-root `app` user, listens on `http://+:8080` and declares a `HEALTHCHECK` against
`/health`. The dev SA password defaults to `Pass@word1` and is overridden with `MSSQL_SA_PASSWORD`.

Ports, environment variables, seeding order and troubleshooting: [`docs/containers.md`](docs/containers.md).

## Conventions

- **Central package management.** Add packages with `<PackageReference Include="X" />` in the
  project and a single `<PackageVersion Include="X" Version="…" />` in
  `Directory.Packages.props`. No versions in project files, no `packages.config`, no binding
  redirects.
- **Shared settings** live in `Directory.Build.props`; do not re-declare `TargetFramework`,
  `Nullable` or `ImplicitUsings` per project.
- **Warnings are errors** (NuGet audit warnings excepted, see `WarningsNotAsErrors`).

## Configuration

All settings are bound to typed options in `eShop.Shared` and validated at start-up
(`ValidateDataAnnotations().ValidateOnStart()`), so a bad or missing setting fails the host before
it serves a request. There is no static `System.Configuration` access anywhere in the modernized
code and a test enforces that. Each host opts in with one line:

```csharp
builder.AddEShopConfiguration();
```

| Setting | Key | Environment variable | Default | Legacy origin |
| --- | --- | --- | --- | --- |
| Catalog database connection string | `ConnectionStrings:Catalog` | `ConnectionStrings__Catalog` | *(none)* | `CatalogDBContext` (MVC / Web Forms `Web.config`) and `EntityModel` (WCF `Web.config`), consolidated into one database |
| Serve mock data instead of the database | `Catalog:UseMockData` | `Catalog__UseMockData` | `false` | `appSettings/UseMockData` |
| Seed the database from the CSV files | `Catalog:UseCustomizationData` | `Catalog__UseCustomizationData` | `false` | `appSettings/UseCustomizationData` |
| Content-root-relative picture folder | `Catalog:PicsFolder` | `Catalog__PicsFolder` | `Pics` | `Server.MapPath("~/Pics")` in `PicController` |
| Content-root-relative seed-data folder | `Catalog:SetupFolder` | `Catalog__SetupFolder` | `Setup` | `HostingEnvironment.ApplicationPhysicalPath` + `"Setup"` in `CatalogDBInitializer` |

Validation rules:

- `Catalog:PicsFolder` and `Catalog:SetupFolder` must be non-empty.
- `ConnectionStrings:Catalog` is **required unless `Catalog:UseMockData` is `true`**. Starting a
  host with `UseMockData=false` and no connection string throws `OptionsValidationException` at
  start-up rather than failing on the first request.

### Secrets

No connection string or key is committed. `appsettings.json` ships the setting *shape* with an
empty connection string; supply the real value per environment through `ConnectionStrings__Catalog`
(process environment variable, container/Kubernetes secret or Key Vault reference), or with
`dotnet user-secrets` locally. `appsettings.Development.json` sets `Catalog:UseMockData=true`, so a
fresh clone runs with no database and no secret at all.

```bash
ConnectionStrings__Catalog='Server=…;Database=Catalog;User Id=…;Password=…' \
  dotnet run --project eShopModernized/src/eShop.Catalog.Api
```

### Breaking change against the legacy WCF service (C-01)

The legacy WCF service read an environment variable named literally `ConnectionString`
(`eShopWCFService/Models/Infrastructure/CatalogConfiguration.cs`). The modernized name is
`ConnectionStrings__Catalog`; the old name is deliberately **not** honoured as a fallback, so any
deployment setting `ConnectionString` must be updated at cutover.

## Dependency injection

The Autofac `ApplicationModule` is replaced by `Microsoft.Extensions.DependencyInjection`
registrations with equivalent lifetimes:

| Legacy (Autofac) | Modernized |
| --- | --- |
| `CatalogServiceMock` as `ICatalogService`, `SingleInstance()` | registered by `AddCatalogData(configuration)` behind `Catalog:UseMockData` |
| `CatalogService` as `ICatalogService`, `InstancePerLifetimeScope()` | registered by `AddCatalogData(configuration)` as scoped |
| `CatalogDBContext`, `InstancePerLifetimeScope()` | `AddDbContext<…>()` (scoped by default) |
| `CatalogDBInitializer`, `InstancePerLifetimeScope()` | an `IDataInitializer` implementation, run once at start-up by a hosted service and only when `UseMockData=false` |
| `CatalogItemHiLoGenerator`, `SingleInstance()` | dropped — EF Core `UseHiLo(…)` |
| `RegisterControllers` / `RegisterApiControllers` | `AddControllersWithViews()` / `AddControllers()` |
| Web Forms property-injection module | n/a — the Web Forms UI is retired |
| `FilterConfig` `HandleErrorAttribute` | `app.UseExceptionHandler("/Home/Error")` + `UseHsts()` |
| `Global.asax` `RouteConfig` | `app.MapControllerRoute(…)` and attribute routes |
| `BundleConfig` | static files served from `wwwroot` |

`ICatalogService` and its implementations belong to the catalog data ticket; `eShop.Shared` only
provides the configuration and the `IDataInitializer` seam those registrations plug into.
