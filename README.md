# eShopModernizing - Legacy .NET Framework Baseline

This repository contains three legacy eShop applications built on the .NET Framework, serving as a baseline for demonstrating modernization to .NET 8+ / ASP.NET Core.

## Legacy Applications

| Application | Framework | Technology Stack |
|---|---|---|
| **eShopLegacyMVCSolution** | .NET Framework 4.7.2 | ASP.NET MVC 5, Entity Framework 6, Autofac, log4net |
| **eShopLegacyWebFormsSolution** | .NET Framework 4.7.2 | ASP.NET Web Forms, Entity Framework 6, Autofac |
| **eShopLegacyNTier** | .NET Framework 4.6.1 / 4.7 | WCF Service + WinForms Desktop Client |

All three apps are simple CRUD applications for managing a product catalog (brands, types, items with pricing and inventory) backed by SQL Server.

## Repository Structure

```
eShopLegacyMVCSolution/          # ASP.NET MVC 5 web app
  eShopLegacyMVC.sln
  src/eShopLegacyMVC/
    Controllers/                 # MVC + WebAPI controllers
    Models/                      # EF6 entities, DbContext, DB initializer
    Services/                    # Business logic (CatalogService)
    Views/                       # Razor views (CRUD pages)
    Global.asax.cs               # Application startup
    Web.config                   # Configuration + connection strings
    packages.config              # NuGet package references

eShopLegacyWebFormsSolution/     # ASP.NET Web Forms web app
  eShopLegacyWebForms.sln
  src/eShopLegacyWebForms/
    Catalog/                     # ASPX pages (Create, Edit, Details, Delete)
    Models/                      # EF6 entities, DbContext
    Services/                    # Business logic
    Global.asax.cs
    Web.config
    packages.config

eShopLegacyNTier/                # WCF + WinForms N-Tier app
  eShopLegacyNTier.sln
  src/eShopWCFService/           # WCF service backend
  src/eShopWinForms/             # WinForms desktop client
```

## Key Legacy Patterns (Migration Targets)

These are the patterns that would need to change during a modernization to .NET 8+ / ASP.NET Core:

- **`Global.asax.cs`** with `HttpApplication` lifecycle -> `Program.cs` / `WebApplicationBuilder`
- **`Web.config`** (XML configuration) -> `appsettings.json`
- **`packages.config`** (NuGet) -> SDK-style `.csproj` with `<PackageReference>`
- **Entity Framework 6** (`System.Data.Entity`) -> EF Core (`Microsoft.EntityFrameworkCore`)
- **`System.Web.Mvc`** controllers -> `Microsoft.AspNetCore.Mvc`
- **Autofac DI with `Global.asax` wiring** -> Built-in ASP.NET Core DI
- **`System.Web.Http` WebAPI** -> ASP.NET Core minimal APIs or controllers
- **WCF Service** -> gRPC or ASP.NET Core Web API
- **log4net** -> `Microsoft.Extensions.Logging` / Serilog
- **`BundleConfig.cs`** (JS/CSS bundling) -> Webpack, Vite, or ASP.NET Core bundling

## Quick Start

### Prerequisites

- Visual Studio 2019+ with ".NET desktop development" and "ASP.NET and web development" workloads
- SQL Server LocalDB (included with Visual Studio) or SQL Server instance
- .NET Framework 4.7.2 Developer Pack

### Running the MVC App

1. Open `eShopLegacyMVCSolution/eShopLegacyMVC.sln` in Visual Studio
2. Set `UseMockData` to `true` in `Web.config` if you don't have SQL Server configured
3. Press F5 to run

### Running with Mock Data (no database required)

Each app supports an in-memory mock data mode. Set `UseMockData` to `true` in `Web.config`:

```xml
<appSettings>
  <add key="UseMockData" value="true" />
</appSettings>
```

## Related Resources

- [Modernize existing .NET apps with Azure and Windows Containers (eBook)](https://aka.ms/liftandshiftwithcontainersebook)
- [Original Microsoft architecture guidance wiki](https://github.com/dotnet-architecture/eShopModernizing/wiki)
