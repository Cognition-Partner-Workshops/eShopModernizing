# Configuration inventory (NET-61)

Every `appSetting` and connection string in the legacy estate and where it lands in the modernized
solution. Nothing here carries a secret: values that differ per environment come from environment
variables.

## Connection strings

| Legacy source | Legacy name | Modernized key |
| --- | --- | --- |
| MVC `Web.config` | `CatalogDBContext` | `ConnectionStrings:Catalog` |
| Web Forms `Web.config` | `CatalogDBContext` | `ConnectionStrings:Catalog` |
| WCF `Web.config` | `EntityModel` | `ConnectionStrings:Catalog` |
| WCF `CatalogConfiguration` | `ConnectionString` environment variable overriding the above | `ConnectionStrings:Catalog`, set with `ConnectionStrings__Catalog` (the bare `ConnectionString` variable is still honoured as a fallback) |

The LocalDB values that were checked into the legacy `Web.config` files are deliberately **not**
carried over: the modernized hosts default to mock data and require
`ConnectionStrings__Catalog` to be supplied by the environment when `Catalog:UseMockData` is false.

## App settings

| Legacy key | MVC | Web Forms | WCF | Modernized key |
| --- | --- | --- | --- | --- |
| `UseMockData` | `false` | `true` | n/a | `Catalog:UseMockData` (`CatalogOptions.UseMockData`) |
| `UseCustomizationData` | `false` | `false` | n/a | `Catalog:UseCustomizationData` (`CatalogOptions.UseCustomizationData`) |
| `webpages:Version`, `webpages:Enabled`, `ClientValidationEnabled`, `UnobtrusiveJavaScriptEnabled` | set | n/a | n/a | dropped — ASP.NET Core has no WebPages/unobtrusive-validation switches |
| `aspnet:UseTaskFriendlySynchronizationContext` | n/a | n/a | `true` | dropped — no synchronization context in ASP.NET Core |
| WinForms `App.config` WCF client endpoint | n/a | n/a | n/a | out of scope here; the gRPC client configuration arrives with NET-66/NET-68 |

`Web.Debug.config` / `Web.Release.config` in all three applications contain only the Visual Studio
template comments — there are no transform deltas to port. `appsettings.Development.json` therefore
only carries development conveniences (`DetailedErrors`).

## Dependency injection

| Legacy Autofac registration | Lifetime | Modernized registration |
| --- | --- | --- |
| `CatalogServiceMock` as `ICatalogService` (when `UseMockData`) | `SingleInstance` | `AddSingleton<ICatalogService, MockCatalogService>` |
| `CatalogService` as `ICatalogService` (otherwise) | `InstancePerLifetimeScope` | `AddScoped<ICatalogService, …>` — currently `PendingDatabaseCatalogService`, replaced by the EF Core implementation in NET-64 |
| `CatalogDBContext` | `InstancePerLifetimeScope` | NET-64 (`AddDbContext` is scoped by default) |
| `CatalogDBInitializer` + `Database.SetInitializer` in `Application_Start` | `InstancePerLifetimeScope` | `ICatalogDatabaseInitializer` run once by `CatalogDatabaseInitializerHostedService`, registered only when not using mock data |
| `CatalogItemHiLoGenerator` | `SingleInstance` | NET-64, together with the EF Core sequence strategy |
| Web Forms `PropertyInjectionModule` | n/a | dropped — ASP.NET Core MVC/Razor uses constructor injection |

All hosts wire up identically:

```csharp
builder.AddEShopConfiguration();                                  // eShop.Shared
builder.Services.AddEShopCatalogServices(builder.Configuration);  // eShop.Catalog.Data
```

`AddEShopCatalogServices` calls `AddEShopServices` (options binding and validation) first. It lives
in `eShop.Catalog.Data` because `eShop.Shared` sits below the data layer in the reference direction
and must not know about `ICatalogService`.
