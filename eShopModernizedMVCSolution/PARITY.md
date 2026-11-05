# Side-by-side parity check: MVC 5 (.NET Framework 4.7.2) vs ASP.NET Core MVC (.NET 8)

Both catalog apps were built and run on Windows Server 2022 in IIS Express, side by side, with `UseMockData=true` (same 12 seeded in-memory items; no LocalDB on the host).

| App | Build | Host | URL |
|---|---|---|---|
| Legacy `eShopLegacyMVC` | `nuget restore` + `msbuild ... /p:Configuration=Release` (VS 2022 Build Tools) | IIS Express, `Clr4IntegratedAppPool` | http://localhost:52429/ |
| Port `eShopModernizedMVC` | `dotnet publish -c Release` (.NET SDK 8.0.4xx) | IIS Express + ASP.NET Core Module V2 (in-process), no-managed-code pool | http://localhost:5100/ |

## Tests

| Suite | Command | Result |
|---|---|---|
| `eShopLegacyMVC.Tests` (net472) | `dotnet test eShopLegacyMVCSolution/tests/eShopLegacyMVC.Tests -c Release` | 48 passed, 0 failed |
| `eShopModernizedMVC.Tests` (net8.0) | `dotnet test eShopModernizedMVCSolution/eShopModernizedMVC.sln -c Release` | 54 passed, 0 failed |

## UI flows (same inputs in both apps)

| Flow / step | Legacy MVC 5 | .NET 8 port | Match |
|---|---|---|---|
| Browse: index | 12 seeded items; ".NET Bot Black Hoodie" $19.50, stock 100 | Same | Yes |
| Browse: next page | Items 11-12, page 2 of 2 | Same | Yes |
| Browse: details (item 11) | "Cup&lt;T&gt; Sheet", .NET / Sheet, $8.50, stock 100 | Same | Yes |
| Create: validation | Empty Price blocked; "The Price field is required."; other inputs kept | Same | Yes |
| Create: save | "Parity Demo Mug" (.NET / Mug, $12.50, 25/5/100, dummy.png) listed; total 13 | Same | Yes |
| Edit: before | Item 1 price 19.50, stock 100 | Same | Yes |
| Edit: save | List and Details show $21.50, stock 50; other fields unchanged | Same | Yes |

Pre-existing in both apps (not a regression): the pager label reads "Showing 10 of N products" on a partial last page.

## Hosting the .NET 8 port in IIS Express

IIS Express does not register the ASP.NET Core Module by default. The hosting bundle (`dotnet-hosting-win.exe`) only installs ANCM when the IIS Web-Server role is present. After installing it, add to the IIS Express `applicationhost.config`:

- `<section name="aspNetCore" overrideModeDefault="Allow" />` under `system.webServer`, with `aspnetcore_schema_v2.xml` copied to `IIS Express\config\schema`
- `<add name="AspNetCoreModuleV2" image="C:\Program Files\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll" />` in `globalModules` and in the root `modules`
- an app pool with `managedRuntimeVersion=""` for the published folder
