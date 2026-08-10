# WinForms client on .NET 8 (NET-68)

Decision D-07 (Jira NET-58) retains the WinForms client, so it is ported rather than retired.

## Layout

The modernized client is a new SDK-style project in `eShop.sln`:

```
src/eShop.Catalog.Grpc.Contracts   Protos/catalog.proto + generated messages and client/server stubs
src/eShop.Catalog.Grpc.Client      ICatalogServiceClient + CatalogServiceGrpcClient (the service layer)
src/eShop.WinForms.Client          net8.0-windows WinForms application (WinExe)
tests/eShop.Catalog.Grpc.Client.Tests  the wrapper against the real gRPC service, mock-data mode
```

The legacy `eShopLegacyNTier/src/eShopWinForms` (.NET Framework 4.7, SOAP) is left untouched and
keeps building on Windows next to the legacy WCF service until the N-tier solution is deleted, the
same way the other legacy applications are treated during the migration.

`catalog.proto` moved out of `src/eShop.Catalog.Grpc` into `src/eShop.Catalog.Grpc.Contracts` so
the service and the client share **one** generated copy of the contract. The proto itself, the
`eShop.Catalog.Grpc.Protos` C# namespace and the wire format are unchanged.

## What changed relative to the legacy client

| Legacy | Modernized |
| --- | --- |
| `net47`, non-SDK `.csproj`, `packages.config` | `net8.0-windows`, SDK-style, `UseWindowsForms=true` |
| `Connected Services\eShopServiceReference` (generated SOAP proxy), `System.ServiceModel` | `CatalogServiceGrpcClient` over the generated gRPC stub — no `System.ServiceModel` anywhere |
| endpoint in `App.config` `<system.serviceModel><client>` | `CatalogService:Address` in `appsettings.json`, overridable with the `CatalogService__Address` environment variable |
| EntityFramework 6.1.3, Newtonsoft.Json 6.0.4, `Microsoft.AspNet.WebApi.Client` (all unused) | dropped |
| synchronous SOAP calls on the UI thread | `async` wrapper; the presenter awaits and the form loads its data on `Form.Load` |
| images embedded through `Properties/Resources.resx` | `Assets/` copied next to the executable and loaded by `AssetImages` (ResX image entries cannot be compiled on Linux) |
| UWP leftovers in `Helpers/` (`Windows.UI.Xaml`, toast notifications) — never compiled | not carried over |

Behaviour of the form is otherwise unchanged: same two tabs, same grid columns, same discount
banner, same stock lookup and shipment flows.

### Null vs `NOT_FOUND`

The SOAP proxy returned `null` from `FindCatalogItem` and `GetDiscount` when there was no match;
the gRPC service reports `NOT_FOUND` (see `grpc-contract.md`). `CatalogServiceGrpcClient`
translates that status back to `null`, so the presenter keeps the legacy control flow. Every other
status (e.g. `INVALID_ARGUMENT`) surfaces as an `RpcException`.

## Configuration

```json
{
  "CatalogService": {
    "Address": "http://localhost:5200",
    "AllowUnencryptedHttp2": true
  }
}
```

The gRPC service is h2c-only (NET-66), hence `AllowUnencryptedHttp2`; point `Address` at an
`https://` endpoint and set it to `false` once the service is fronted by TLS.

## Build, test, publish

```bash
dotnet build eShop.sln          # includes the client: EnableWindowsTargeting makes it compile on Linux
dotnet test eShop.sln           # includes the client-wrapper tests
```

```powershell
dotnet publish src/eShop.WinForms.Client/eShop.WinForms.Client.csproj `
  --configuration Release --runtime win-x64 --self-contained true -p:PublishSelfContained=true `
  --output publish/winforms-client
```

The `winforms-client` job in `.github/workflows/ci.yml` runs that publish on `windows-latest` and
uploads the result as the `eShop.WinForms.Client-win-x64` artifact. The client stays outside the
Linux container story — it is a desktop application shipped as an executable.

## Verification status

* **Compile-verified** on Linux (`dotnet build eShop.sln`) and cross-published self-contained for
  `win-x64`; the Windows CI job publishes it on a real Windows agent.
* **Runtime-verified** at the service-layer boundary: `tests/eShop.Catalog.Grpc.Client.Tests`
  drives all ten operations through `CatalogServiceGrpcClient` against the real gRPC service
  running in mock-data mode.
* **Not verified**: the WinForms UI itself (form rendering, grid, event wiring) — that requires an
  interactive Windows desktop, which no agent in this pipeline has.
