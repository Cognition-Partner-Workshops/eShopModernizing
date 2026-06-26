# Migration Playbook: .NET Framework 4.7.2 → .NET 8 ASP.NET Core

This playbook guides the incremental migration of `eShopLegacyMVCSolution` from ASP.NET MVC 5 on .NET Framework 4.7.2 to ASP.NET Core on .NET 8. Each step is independently verifiable — run the test suite after each phase to confirm behavior parity.

---

## Prerequisites

- .NET 8 SDK (`dotnet --version` ≥ 8.0)
- Visual Studio 2022+ or VS Code with C# Dev Kit
- SQL Server LocalDB (or use mock data mode for database-free validation)
- Git (for branch-per-step workflow)

---

## Local Validation (run after every step)

```bash
# Unit tests (48 tests — pagination, CRUD, controller actions, seed data integrity)
dotnet test eShopLegacyMVCSolution/tests/eShopLegacyMVC.Tests/ --verbosity normal

# Integration smoke test (after step 4+, when the app can run on .NET 8)
dotnet run --project eShopLegacyMVCSolution/src/eShopLegacyMVC/
# Browse to https://localhost:5001 — verify catalog CRUD works
```

---

## Step 1: Convert to SDK-style Project File

**What:** Replace the verbose legacy `.csproj` with SDK-style format. Replace `packages.config` with `<PackageReference>` elements.

**Why:** SDK-style projects are required for `dotnet` CLI tooling, multi-targeting, and .NET 8 builds.

**Changes:**

```xml
<!-- BEFORE: eShopLegacyMVC.csproj (legacy, ~250 lines) -->
<Project ToolsVersion="15.0" ...>
  <Import Project="$(MSBuildExtensionsPath)\..." />
  <PropertyGroup>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
    ...
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="System.Web" />
    <Compile Include="Controllers\CatalogController.cs" />
    <!-- 100+ explicit Compile/Content items -->
  </ItemGroup>
</Project>

<!-- AFTER: eShopLegacyMVC.csproj (SDK-style, ~30 lines) -->
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNet.Mvc" Version="5.2.7" />
    <PackageReference Include="EntityFramework" Version="6.2.0" />
    <PackageReference Include="Autofac.Mvc5" Version="4.0.2" />
    <PackageReference Include="log4net" Version="2.0.10" />
    <!-- ... other packages from packages.config -->
  </ItemGroup>
</Project>
```

**Key actions:**
1. Delete `packages.config`
2. Replace `.csproj` contents with SDK-style format (still targeting `net472`)
3. Remove explicit `<Compile>` items (SDK-style auto-includes `**/*.cs`)
4. Run `dotnet restore` and `dotnet build` to verify

**Validation:** Tests still pass. No runtime behavior change — just tooling modernization.

---

## Step 2: Retarget to .NET 8

**What:** Change `<TargetFramework>` from `net472` to `net8.0`.

**Why:** This is the framework switch — enables cross-platform, modern runtime, and access to ASP.NET Core APIs.

**Changes:**

```xml
<!-- BEFORE -->
<TargetFramework>net472</TargetFramework>

<!-- AFTER -->
<TargetFramework>net8.0</TargetFramework>
```

**What breaks:**
- `System.Web.*` namespaces don't exist in .NET 8 → compilation errors
- `Global.asax.cs` won't compile (uses `HttpApplication`)
- `Web.config` is ignored by ASP.NET Core runtime
- `packages.config`-era packages (Autofac.Mvc5, Microsoft.AspNet.Mvc) won't resolve
- `BundleConfig`, `RouteConfig`, `FilterConfig` reference `System.Web`

**What survives:**
- Models (`CatalogItem`, `CatalogBrand`, `CatalogType`) — POCOs, no framework dependency
- `ICatalogService` interface — pure .NET
- `CatalogServiceMock` — uses only `System.Linq`, models, `PreconfiguredData`
- `PaginatedItemsViewModel` — pure .NET

**Key actions:**
1. Change `<TargetFramework>` to `net8.0`
2. Remove packages that have no .NET 8 equivalent (Application Insights legacy, TelemetryCorrelation)
3. Replace framework-specific packages (see Step 3)
4. Expect compilation failures — resolve in subsequent steps

**Validation:** Build will fail at this point. Tests targeting the mock service and view model should still pass if you temporarily exclude controller tests.

---

## Step 3: Replace Global.asax.cs with Program.cs

**What:** Replace the ASP.NET lifecycle (`HttpApplication`, `Application_Start`) with the ASP.NET Core minimal hosting model.

**Why:** ASP.NET Core uses `Program.cs` as the entry point, with explicit middleware pipeline configuration.

**Changes:**

```csharp
// BEFORE: Global.asax.cs
public class MvcApplication : HttpApplication
{
    protected void Application_Start()
    {
        container = RegisterContainer();         // Autofac DI
        GlobalConfiguration.Configure(...);      // WebAPI
        AreaRegistration.RegisterAllAreas();
        FilterConfig.RegisterGlobalFilters(...);
        RouteConfig.RegisterRoutes(...);
        BundleConfig.RegisterBundles(...);
        ConfigDataBase();
    }
}

// AFTER: Program.cs
var builder = WebApplication.CreateBuilder(args);

// Configuration (replaces ConfigurationManager)
var useMockData = builder.Configuration.GetValue<bool>("UseMockData");

// DI (replaces Autofac container)
if (useMockData)
    builder.Services.AddSingleton<ICatalogService, CatalogServiceMock>();
else
    builder.Services.AddScoped<ICatalogService, CatalogService>();

builder.Services.AddDbContext<CatalogDBContext>();
builder.Services.AddSingleton<CatalogItemHiLoGenerator>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute("default", "{controller=Catalog}/{action=Index}/{id?}");

app.Run();
```

**Key actions:**
1. Create `Program.cs` with the above pattern
2. Delete `Global.asax`, `Global.asax.cs`
3. Delete `App_Start/` folder (`BundleConfig.cs`, `RouteConfig.cs`, `FilterConfig.cs`, `WebApiConfig.cs`)
4. Create `appsettings.json` (see Step 4)

**Validation:** App should start with `dotnet run`. Controller tests need updating (see Step 6).

---

## Step 4: Replace Web.config with appsettings.json

**What:** Move connection strings and app settings from XML to JSON configuration.

**Why:** ASP.NET Core uses `appsettings.json` + `IConfiguration` instead of `ConfigurationManager`.

**Changes:**

```xml
<!-- BEFORE: Web.config -->
<connectionStrings>
  <add name="CatalogDBContext"
       connectionString="Data Source=(localdb)\MSSQLLocalDB; Initial Catalog=..."
       providerName="System.Data.SqlClient" />
</connectionStrings>
<appSettings>
  <add key="UseMockData" value="false" />
  <add key="UseCustomizationData" value="false" />
</appSettings>
```

```json
// AFTER: appsettings.json
{
  "ConnectionStrings": {
    "CatalogDBContext": "Data Source=(localdb)\\MSSQLLocalDB; Initial Catalog=Microsoft.eShopOnContainers.Services.CatalogDb; Integrated Security=True; MultipleActiveResultSets=True;"
  },
  "UseMockData": true,
  "UseCustomizationData": false,
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

**Code changes in services:**

```csharp
// BEFORE (in CatalogDBInitializer.cs):
useCustomizationData = bool.Parse(ConfigurationManager.AppSettings["UseCustomizationData"]);

// AFTER (inject IConfiguration):
public CatalogDBInitializer(IConfiguration config, CatalogItemHiLoGenerator indexGenerator)
{
    this.indexGenerator = indexGenerator;
    useCustomizationData = config.GetValue<bool>("UseCustomizationData");
}
```

**Key actions:**
1. Create `appsettings.json` and `appsettings.Development.json`
2. Delete `Web.config` (keep `web.config` stub if deploying to IIS)
3. Replace all `ConfigurationManager.AppSettings[...]` with injected `IConfiguration`
4. Update `CatalogDBContext` to accept connection string via DI

**Validation:** Set `"UseMockData": true` and run. Catalog should display the 12 preconfigured items without SQL Server.

---

## Step 5: Replace Entity Framework 6 with EF Core

**What:** Swap `System.Data.Entity` for `Microsoft.EntityFrameworkCore`.

**Why:** EF6 doesn't run on .NET 8. EF Core is the supported ORM with better performance and cross-platform support.

**Package changes:**

```xml
<!-- BEFORE -->
<PackageReference Include="EntityFramework" Version="6.2.0" />

<!-- AFTER -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
```

**Code changes:**

```csharp
// BEFORE: CatalogDBContext.cs
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;

public class CatalogDBContext : DbContext
{
    public CatalogDBContext() : base("name=CatalogDBContext") { }

    protected override void OnModelCreating(DbModelBuilder builder)
    {
        builder.Entity<CatalogItem>().ToTable("Catalog");
        builder.Entity<CatalogItem>().Property(ci => ci.Id)
            .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
        builder.Entity<CatalogItem>().Ignore(ci => ci.PictureUri);
        // ...
    }
}

// AFTER: CatalogDBContext.cs
using Microsoft.EntityFrameworkCore;

public class CatalogDBContext : DbContext
{
    public CatalogDBContext(DbContextOptions<CatalogDBContext> options) : base(options) { }

    public DbSet<CatalogItem> CatalogItems { get; set; }
    public DbSet<CatalogBrand> CatalogBrands { get; set; }
    public DbSet<CatalogType> CatalogTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<CatalogItem>().ToTable("Catalog");
        builder.Entity<CatalogItem>().Property(ci => ci.Id)
            .ValueGeneratedNever();
        builder.Entity<CatalogItem>().Ignore(ci => ci.PictureUri);
        // ...
    }
}
```

**Key API differences:**

| EF6 | EF Core |
|-----|---------|
| `DbModelBuilder` | `ModelBuilder` |
| `EntityTypeConfiguration<T>` | `IEntityTypeConfiguration<T>` or inline in `OnModelCreating` |
| `HasDatabaseGeneratedOption(None)` | `ValueGeneratedNever()` |
| `builder.Entity<T>().Ignore(...)` | Same API |
| `Include(c => c.Nav)` | Same API |
| `Database.SetInitializer<T>(...)` | Use migrations or `EnsureCreated()` |
| `db.Database.SqlQuery<T>(sql)` | `db.Database.SqlQueryRaw<T>(sql)` |

**Key actions:**
1. Replace EF6 NuGet packages with EF Core 8.0
2. Rewrite `CatalogDBContext` with EF Core APIs
3. Update `CatalogService.cs` — `Include()` syntax is the same, but `SqlQuery` becomes `SqlQueryRaw`
4. Replace `CatalogDBInitializer` (EF6 `CreateDatabaseIfNotExists`) with EF Core migrations or seed in `OnModelCreating`
5. Register `DbContext` in `Program.cs`: `builder.Services.AddDbContext<CatalogDBContext>(o => o.UseSqlServer(...))`

**Validation:** With `UseMockData=true`, all tests pass (mock service doesn't touch EF). For full validation, run with SQL Server and verify CRUD.

---

## Step 6: Replace Autofac with Built-in DI

**What:** Remove the Autofac container and use ASP.NET Core's built-in `IServiceCollection`.

**Why:** ASP.NET Core has a capable built-in DI container. Autofac adds complexity for this app's needs.

**Changes:**

```csharp
// BEFORE: ApplicationModule.cs (Autofac module)
public class ApplicationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        if (useMockData)
            builder.RegisterType<CatalogServiceMock>().As<ICatalogService>().SingleInstance();
        else
            builder.RegisterType<CatalogService>().As<ICatalogService>().InstancePerLifetimeScope();

        builder.RegisterType<CatalogDBContext>().InstancePerLifetimeScope();
        builder.RegisterType<CatalogItemHiLoGenerator>().SingleInstance();
    }
}

// AFTER: In Program.cs (already done in Step 3)
if (useMockData)
    builder.Services.AddSingleton<ICatalogService, CatalogServiceMock>();
else
    builder.Services.AddScoped<ICatalogService, CatalogService>();
```

**Key actions:**
1. Delete `Modules/ApplicationModule.cs`
2. Remove Autofac NuGet packages (`Autofac`, `Autofac.Mvc5`, `Autofac.Integration.WebApi`)
3. All DI is now in `Program.cs` (already added in Step 3)

**Validation:** All 48 tests pass. App runs with correct DI resolution.

---

## Step 7: Update Controllers for ASP.NET Core

**What:** Replace `System.Web.Mvc` controller base class and patterns with `Microsoft.AspNetCore.Mvc`.

**Why:** Different namespace, slightly different APIs for action results.

**Changes:**

```csharp
// BEFORE: CatalogController.cs
using System.Web.Mvc;

public class CatalogController : Controller
{
    public ActionResult Index(int pageSize = 10, int pageIndex = 0) { ... }
    public ActionResult Details(int? id)
    {
        if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        if (item == null) return HttpNotFound();
        ...
    }
}

// AFTER: CatalogController.cs
using Microsoft.AspNetCore.Mvc;

public class CatalogController : Controller
{
    public IActionResult Index(int pageSize = 10, int pageIndex = 0) { ... }
    public IActionResult Details(int? id)
    {
        if (id == null) return BadRequest();
        if (item == null) return NotFound();
        ...
    }
}
```

**Key API differences:**

| ASP.NET MVC 5 | ASP.NET Core |
|---------------|-------------|
| `System.Web.Mvc.Controller` | `Microsoft.AspNetCore.Mvc.Controller` |
| `ActionResult` | `IActionResult` |
| `new HttpStatusCodeResult(HttpStatusCode.BadRequest)` | `BadRequest()` |
| `HttpNotFound()` | `NotFound()` |
| `new SelectList(...)` | Same API (in `Microsoft.AspNetCore.Mvc.Rendering`) |
| `[ValidateAntiForgeryToken]` | Same (works with Tag Helpers) |
| `Server.MapPath("~/Pics")` | `IWebHostEnvironment.WebRootPath + "/Pics"` |

**Key actions:**
1. Replace `using System.Web.Mvc` with `using Microsoft.AspNetCore.Mvc`
2. Replace `ActionResult` return types with `IActionResult`
3. Replace `HttpNotFound()` → `NotFound()`, `HttpStatusCodeResult` → `BadRequest()`/`StatusCode()`
4. Inject `IWebHostEnvironment` into `PicController` for file path resolution
5. Replace `Url.RouteUrl(..., Request.Url.Scheme)` with ASP.NET Core equivalent

**Validation:** All controller tests pass after updating test assertions for new result types.

---

## Step 8: Migrate Views (Razor)

**What:** Update Razor views from MVC 5 syntax to ASP.NET Core Tag Helpers.

**Why:** Tag Helpers are the idiomatic approach in ASP.NET Core (though `@Html.*` helpers still work).

**Changes (optional — Html helpers still work):**

```html
<!-- BEFORE: @Html.ActionLink("Create", "Create") -->
<a asp-action="Create">Create</a>

<!-- BEFORE: @Html.BeginForm() -->
<form asp-action="Create" method="post">

<!-- BEFORE: @Html.EditorFor(m => m.Name) -->
<input asp-for="Name" class="form-control" />

<!-- BEFORE: @Html.ValidationMessageFor(m => m.Name) -->
<span asp-validation-for="Name" class="text-danger"></span>
```

**Key actions:**
1. Add `_ViewImports.cshtml` with `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`
2. Replace `@Scripts.Render("~/bundles/...")` with direct `<script>` tags or a bundler (webpack/vite)
3. Replace `@Styles.Render("~/Content/css")` with `<link>` tags
4. Optionally convert `@Html.*` to Tag Helpers for idiomatic code

**Validation:** Run app, navigate all CRUD pages — same UI, same behavior.

---

## Step 9: Replace log4net with Built-in Logging

**What:** Replace `log4net` with `Microsoft.Extensions.Logging`.

**Why:** Built-in logging integrates with ASP.NET Core's DI and configuration system.

```csharp
// BEFORE
private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
_log.Info($"Now loading...");

// AFTER
private readonly ILogger<CatalogController> _logger;
public CatalogController(ICatalogService service, ILogger<CatalogController> logger)
{
    this.service = service;
    _logger = logger;
}
_logger.LogInformation("Now loading...");
```

**Key actions:**
1. Remove `log4net` NuGet package and `log4net.config`
2. Inject `ILogger<T>` via constructor
3. Replace `_log.Info/Debug/Error` with `_logger.LogInformation/Debug/Error`

**Validation:** Tests pass. Logging output appears in console.

---

## Step 10: Final Cleanup

**What:** Remove legacy artifacts and verify everything works end-to-end.

**Delete:**
- `Global.asax`, `Global.asax.cs`
- `App_Start/` folder
- `Web.config`, `Web.Debug.config`, `Web.Release.config`
- `packages.config`
- `log4net.config`
- `ApplicationInsights.config`
- `BundleConfig.cs`, `RouteConfig.cs`, `FilterConfig.cs`

**Update CI:**
```yaml
# .github/workflows/ci.yml — simplified for .NET 8
jobs:
  build-and-test:
    runs-on: ubuntu-latest  # No longer needs Windows!
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet build eShopLegacyMVCSolution/eShopLegacyMVC.sln
      - run: dotnet test eShopLegacyMVCSolution/tests/eShopLegacyMVC.Tests/
```

**Validation:**
- `dotnet build` succeeds (no msbuild needed!)
- `dotnet test` — all 48 tests pass
- `dotnet run` — app starts, catalog CRUD works
- CI runs on Linux (faster, cheaper than Windows runners)

---

## Migration Summary

| Aspect | Before (.NET Framework 4.7.2) | After (.NET 8) |
|--------|-------------------------------|----------------|
| Entry point | `Global.asax.cs` (HttpApplication) | `Program.cs` (minimal hosting) |
| Configuration | `Web.config` + `ConfigurationManager` | `appsettings.json` + `IConfiguration` |
| Dependencies | `packages.config` (41 packages) | `<PackageReference>` (~8 packages) |
| DI container | Autofac (`ApplicationModule`) | Built-in `IServiceCollection` |
| ORM | Entity Framework 6 | EF Core 8 |
| Logging | log4net | `Microsoft.Extensions.Logging` |
| Controllers | `System.Web.Mvc.Controller` | `Microsoft.AspNetCore.Mvc.Controller` |
| Views | `@Html.*` helpers + BundleConfig | Tag Helpers + direct asset refs |
| Build tool | msbuild + nuget.exe | `dotnet` CLI |
| CI runner | `windows-latest` (required) | `ubuntu-latest` (cross-platform) |
| Project file | ~250-line legacy csproj | ~30-line SDK-style csproj |

---

## Recommended Branch Strategy for Demo

```
main (legacy baseline, .NET Framework 4.7.2)
  └── migration/step-1-sdk-csproj
       └── migration/step-2-retarget-net8
            └── migration/step-3-program-cs
                 └── migration/step-4-appsettings
                      └── migration/step-5-ef-core
                           └── migration/step-6-remove-autofac
                                └── migration/step-7-controllers
                                     └── migration/step-8-views
                                          └── migration/step-9-logging
                                               └── migration/step-10-cleanup (final .NET 8 app)
```

Each branch builds on the previous. PRs between adjacent branches show exactly what changed per step. Attendees can `git diff migration/step-N..migration/step-N+1` to see each transformation.
