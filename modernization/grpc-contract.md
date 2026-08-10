# WCF `ICatalogService` → gRPC (NET-66)

The legacy SOAP 1.1 / `basicHttpBinding` service `eShopWCFService.CatalogService`
(`eShopLegacyNTier/src/eShopWCFService/`) is ported to gRPC as
`eshop.catalog.v1.Catalog`, defined in `src/eShop.Catalog.Grpc/Protos/catalog.proto` and
implemented by `CatalogGrpcService` on top of the EF Core 8 data layer (NET-64).

The legacy WCF project, `CatalogService.svc` and the `<system.serviceModel>` configuration are
**left in place**: the WinForms client is cut over in NET-68 and the SOAP endpoint stays available
until then. No CoreWCF shim is provided, because there is no external SOAP consumer besides that
client.

## Operation map (all ten `[OperationContract]` members)

| SOAP operation | gRPC method | Request → response |
| --- | --- | --- |
| `CatalogItem FindCatalogItem(int)` | `FindCatalogItem` | `FindCatalogItemRequest` → `FindCatalogItemResponse` |
| `List<CatalogBrand> GetCatalogBrands()` | `GetCatalogBrands` | `GetCatalogBrandsRequest` → `GetCatalogBrandsResponse` |
| `List<CatalogItem> GetCatalogItems(int,int)` | `GetCatalogItems` | `GetCatalogItemsRequest` → `GetCatalogItemsResponse` |
| `List<CatalogType> GetCatalogTypes()` | `GetCatalogTypes` | `GetCatalogTypesRequest` → `GetCatalogTypesResponse` |
| `int GetAvailableStock(DateTime,int)` | `GetAvailableStock` | `GetAvailableStockRequest` → `GetAvailableStockResponse` |
| `void CreateAvailableStock(CatalogItemsStock)` | `CreateAvailableStock` | `CreateAvailableStockRequest` → `CreateAvailableStockResponse` |
| `void CreateCatalogItem(CatalogItem)` | `CreateCatalogItem` | `CreateCatalogItemRequest` → `CreateCatalogItemResponse` |
| `void UpdateCatalogItem(CatalogItem)` | `UpdateCatalogItem` | `UpdateCatalogItemRequest` → `UpdateCatalogItemResponse` |
| `void RemoveCatalogItem(CatalogItem)` | `RemoveCatalogItem` | `RemoveCatalogItemRequest` → `RemoveCatalogItemResponse` |
| `DiscountItem GetDiscount(DateTime)` | `GetDiscount` | `GetDiscountRequest` → `GetDiscountResponse` |

`void` has no protobuf equivalent, so each of the four mutating operations returns a dedicated
empty message rather than `google.protobuf.Empty`; that leaves room to add fields later without a
breaking change.

## Type mapping

| `[DataContract]` member | proto type | Notes |
| --- | --- | --- |
| `int` | `int32` | |
| `string` | `string` | proto3 has no null; a null legacy member is sent as `""` |
| `decimal` (`CatalogItem.Price`) | `DecimalValue { int64 units; int32 nanos; }` | **Chosen over `double` and over `string`.** The column is `money` / `decimal(19,4)`: `double` cannot represent it exactly, and a string would push parsing (and culture handling) onto every client. `value == units + nanos / 1e9`, both parts carrying the sign — the exact-round-trip property is pinned by `CatalogProtoMapperTests`. |
| `double` (`DiscountItem.Size`) | `double` | The legacy member really is a CLR `double`, so it stays one. The seeded values were written as `float` literals (`0.3f`), which is why `GetDiscount` reports `0.30000001192092896` — that is the legacy value reproduced faithfully, not a mapping defect. |
| `DateTime` | `google.protobuf.Timestamp` | Sent as UTC. The legacy columns are SQL `date` and every legacy comparison uses `.Date`, so only the date component is significant; the service truncates incoming timestamps to their date. |
| `List<T>` | `repeated T` | |
| complex members (`CatalogItem.CatalogBrand` / `CatalogType`) | nested messages | Unset when the legacy member was null |

Field names follow protobuf conventions (`picture_filename` for the legacy `Picturefilename`); the
generated C# property names are the familiar PascalCase ones.

## SOAP faults → gRPC status codes

The legacy service performed no argument validation and had
`includeExceptionDetailInFaults="false"`, so a caller got either a value, `null`, or an opaque
`FaultException`. The gRPC service makes the outcomes explicit:

| Situation | Legacy behaviour | gRPC status |
| --- | --- | --- |
| `FindCatalogItem` / `GetDiscount` with no match | returned `null` | `NOT_FOUND` |
| `UpdateCatalogItem` / `RemoveCatalogItem` for an unknown id | `DbUpdateConcurrencyException` → opaque fault | `NOT_FOUND` |
| `GetAvailableStock` with no stock row for that date | returned `0` | `OK` with `available_stock = 0` (behaviour preserved) |
| Missing request payload (`catalog_item`, `catalog_items_stock`, a required timestamp) | `NullReferenceException` → opaque fault | `INVALID_ARGUMENT` |
| Non-positive identifier, negative filter, negative stock | undefined (empty result or fault) | `INVALID_ARGUMENT` |
| Unhandled server error | opaque fault | `UNKNOWN` (ASP.NET Core default; details stay off the wire) |

Returning `NOT_FOUND` instead of an empty response is the one deliberate contract change: proto3
cannot distinguish "absent message" from "default message", so the null-return operations need a
status code to stay unambiguous.

## Hosting

`src/eShop.Catalog.Grpc/Program.cs` uses the shared wiring from the earlier waves —
`UseEShopLogging` / `AddEShopTelemetry` (NET-62), `AddEShopConfiguration` (NET-61),
`AddEShopHealthChecks` + `MapEShopHealthChecks` — plus `AddGrpc()`, `MapGrpcService<CatalogGrpcService>()`
and gRPC **server reflection**, which is the moral equivalent of the legacy `?wsdl` / `mex`
metadata endpoints and is what lets `grpcurl` list and describe the service.

`appsettings.json` sets `Kestrel:EndpointDefaults:Protocols = Http2` so the service speaks h2c
(gRPC over plaintext HTTP/2) out of the box on Linux containers; the health endpoints are then also
HTTP/2 (`curl --http2-prior-knowledge http://localhost:5200/health`).

With `Catalog:UseMockData=true` (the default) the service runs with no database at all:
`MockCatalogService` (NET-64) serves the catalog and `MockCatalogStockService` serves the WCF-only
stock and discount data, both seeded from the legacy `PreconfiguredData` sets.

## Data-layer additions

The catalog CRUD operations reuse the shared `ICatalogService`. The three operations that only the
WCF service ever exposed are added on a **separate, additive** interface,
`eShop.Catalog.Data.ICatalogStockService` (`GetAvailableStockAsync`, `CreateAvailableStockAsync`,
`GetDiscountAsync`), with an EF Core implementation (`CatalogStockService`) and an in-memory one
(`MockCatalogStockService`), registered by `AddEShopCatalogWithStockServices`. `ICatalogService`
itself is left untouched so the tickets running in parallel do not conflict; it is still
synchronous (NET-64 ported it as-is), while everything added here is `async`/`SaveChangesAsync`.

## Local demo

```bash
ASPNETCORE_URLS=http://localhost:5200 dotnet run --project src/eShop.Catalog.Grpc
grpcurl -plaintext localhost:5200 list
grpcurl -plaintext localhost:5200 list eshop.catalog.v1.Catalog
grpcurl -plaintext -d '{"id": 1}' localhost:5200 eshop.catalog.v1.Catalog/FindCatalogItem
```
