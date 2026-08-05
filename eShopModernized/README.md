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

## Conventions

- **Central package management.** Add packages with `<PackageReference Include="X" />` in the
  project and a single `<PackageVersion Include="X" Version="…" />` in
  `Directory.Packages.props`. No versions in project files, no `packages.config`, no binding
  redirects.
- **Shared settings** live in `Directory.Build.props`; do not re-declare `TargetFramework`,
  `Nullable` or `ImplicitUsings` per project.
- **Warnings are errors** (NuGet audit warnings excepted, see `WarningsNotAsErrors`).
