# System Brief: eShopModernizing (Legacy .NET Framework eShop)

*Audience: CTO / board. Source: direct code reading (no DeepWiki index exists for this repo). Inferences are marked "(inference)".*

## Purpose

An internal back-office tool for maintaining a product catalog: brands, types, and items with price, stock levels and restock thresholds ([README.md](../README.md#L15), [CatalogItem.cs](../eShopLegacyMVCSolution/src/eShopLegacyMVC/Models/CatalogItem.cs#L25-L52)). The repo has three versions of the same CRUD app and calls itself a "baseline for demonstrating modernization to .NET 8+" ([README.md](../README.md#L3)). Users would be catalog/merchandising staff (inference). No shopper-facing checkout or payment code is present.

## Architecture

| Component | Stack | Evidence |
|---|---|---|
| MVC web app (+ Web API) | ASP.NET MVC 5.2.7, Web API, EF 6.2, Autofac 4.9, log4net; .NET Framework 4.7.2 | [packages.config](../eShopLegacyMVCSolution/src/eShopLegacyMVC/packages.config#L4-L18), [Global.asax.cs](../eShopLegacyMVCSolution/src/eShopLegacyMVC/Global.asax.cs#L27-L36) |
| Web Forms web app | ASP.NET Web Forms, EF 6, Autofac; .NET 4.7.2 | [Web.config](../eShopLegacyWebFormsSolution/src/eShopLegacyWebForms/Web.config#L27) |
| N-Tier: WCF service | WCF (`.svc`), EF 6; .NET 4.6.1 | [eShopWCFService.csproj](../eShopLegacyNTier/src/eShopWCFService/eShopWCFService.csproj#L15), [ICatalogService.cs](../eShopLegacyNTier/src/eShopWCFService/ICatalogService.cs#L12-L35) |
| N-Tier: WinForms desktop client | WinForms; .NET 4.7 | [eShopWinForms.csproj](../eShopLegacyNTier/src/eShopWinForms/eShopWinForms.csproj#L11) |

- Each web app can run on an in-memory mock (`UseMockData`) instead of a database ([Global.asax.cs](../eShopLegacyMVCSolution/src/eShopLegacyMVC/Global.asax.cs#L68-L90)). The Web Forms config ships with mock mode on ([Web.config](../eShopLegacyWebFormsSolution/src/eShopLegacyWebForms/Web.config#L15)); MVC ships with it off ([Web.config](../eShopLegacyMVCSolution/src/eShopLegacyMVC/Web.config#L19)).
- **Deployment model:** IIS-hosted Windows apps plus a desktop client. The repo has no Dockerfiles or infrastructure-as-code. CI runs on GitHub Actions on `windows-latest`, and it builds and tests **only the MVC app** ([ci.yml](../.github/workflows/ci.yml#L10-L41)). How production is hosted can't be determined from the repo (inference).

## Data stores

- **SQL Server** (LocalDB in the checked-in config), accessed through EF 6. The connection strings are named `CatalogDBContext` ([MVC Web.config](../eShopLegacyMVCSolution/src/eShopLegacyMVC/Web.config#L12), [Web Forms Web.config](../eShopLegacyWebFormsSolution/src/eShopLegacyWebForms/Web.config#L12)) and `EntityModel` ([WCF Web.config](../eShopLegacyNTier/src/eShopWCFService/Web.config#L63)). The web apps use Windows integrated auth to the database. These are config entries only; this brief doesn't reproduce their values.
- On first start, the app creates and seeds the schema (`CreateDatabaseIfNotExists`, HiLo sequence SQL scripts, CSV seed data) ([CatalogDBInitializer.cs](../eShopLegacyMVCSolution/src/eShopLegacyMVC/Models/Infrastructure/CatalogDBInitializer.cs#L15-L43), [Setup/](../eShopLegacyMVCSolution/src/eShopLegacyMVC/Setup/CatalogItems.csv)).
- **Local filesystem:** product images are unzipped into `Pics/` ([CatalogDBInitializer.cs](../eShopLegacyMVCSolution/src/eShopLegacyMVC/Models/Infrastructure/CatalogDBInitializer.cs#L346-L353)), and logs go to a rolling file `logFiles\myapp.log` at level ALL ([log4Net.xml](../eShopLegacyMVCSolution/src/eShopLegacyMVC/log4Net.xml#L3-L7)).

## Integrations

- **Inbound HTTP APIs (MVC):** Web API routes `api/{controller}/{id}` ([WebApiConfig.cs](../eShopLegacyMVCSolution/src/eShopLegacyMVC/App_Start/WebApiConfig.cs#L12-L16)). They expose brands, including a `DELETE` that is currently a no-op ([BrandsController.cs](../eShopLegacyMVCSolution/src/eShopLegacyMVC/Controllers/WebApi/BrandsController.cs#L38-L50)), and a binary-serialized brand file ([FilesController.cs](../eShopLegacyMVCSolution/src/eShopLegacyMVC/Controllers/WebApi/FilesController.cs#L21-L36)).
- **Inbound SOAP (WCF):** create, update and delete for items, stock and discounts over `basicHttpBinding`, with a public metadata (`mex`) endpoint ([Web.config](../eShopLegacyNTier/src/eShopWCFService/Web.config#L27-L40), [ICatalogService.cs](../eShopLegacyNTier/src/eShopWCFService/ICatalogService.cs#L15-L34)). The WinForms client calls it over plain HTTP ([App.config](../eShopLegacyNTier/src/eShopWinForms/App.config#L26-L27)).
- **Outbound:** Azure Application Insights telemetry modules are wired in ([Web.config](../eShopLegacyMVCSolution/src/eShopLegacyMVC/Web.config#L35-L36), [ApplicationInsights.config](../eShopLegacyMVCSolution/src/eShopLegacyMVC/ApplicationInsights.config)). No instrumentation key is checked in.
- **Auth:** none found. There are no `[Authorize]` attributes, no authentication config, and no WCF security mode. The only security control in the code is anti-forgery tokens on MVC form posts ([CatalogController.cs](../eShopLegacyMVCSolution/src/eShopLegacyMVC/Controllers/CatalogController.cs#L60-L61)).

## Top 3 operational risks

1. **High: No authentication on catalog write paths.**
   - *Business impact:* anyone who can reach the apps or the WCF service can change prices, stock and discounts ([ICatalogService.cs](../eShopLegacyNTier/src/eShopWCFService/ICatalogService.cs#L26-L34)). That puts revenue and data integrity at risk.
   - *Mitigation:* put the apps and APIs behind SSO (e.g. Entra ID) with role-based authorization and HTTPS-only bindings.
2. **High: End-of-life runtime and insecure serialization.**
   - *Business impact:* the system targets .NET Framework 4.6.1, 4.7 and 4.7.2 with 2019-era packages ([packages.config](../eShopLegacyMVCSolution/src/eShopLegacyMVC/packages.config#L4-L29)), and it uses `BinaryFormatter` ([Serializing.cs](../eShopLegacyMVCSolution/eShopLegacy.Utilities/Serializing.cs#L8-L22)). 4.6.1 is past Microsoft support, and Microsoft considers `BinaryFormatter` unsafe (external fact, not from the repo). Unpatched vulnerabilities and a shrinking hiring pool push up cost and breach exposure.
   - *Mitigation:* fund the .NET 8 migration the repo is staged for, and remove `BinaryFormatter` first.
3. **Medium: Configuration isn't production-ready and the safety net is thin.**
   - *Business impact:* checked-in configs have `debug="true"`, directory browsing and public service metadata turned on ([WCF Web.config](../eShopLegacyNTier/src/eShopWCFService/Web.config#L19-L50)). The schema auto-creates at startup, and CI builds and tests only one of the three apps ([ci.yml](../.github/workflows/ci.yml#L28-L41)). Releases can leak internals or ship untested regressions.
   - *Mitigation:* add Release config transforms with hardened settings, managed DB migrations, and CI coverage for the Web Forms and N-Tier solutions.
