# WCF `ICatalogService` → gRPC

The legacy SOAP 1.1 / `basicHttpBinding` service `eShopWCFService.CatalogService`
(`eShopLegacyNTier/src/eShopWCFService/`) is ported to gRPC as
`eshop.catalog.v1.Catalog`, defined in `src/eShopWcf.Catalog.Contracts/Protos/catalog.proto` and
implemented by `CatalogGrpcService` on top of the self-contained EF Core 8 SQL Server data layer.
The protobuf file is byte-for-byte identical to PR #226; the C# wire namespace remains
`eShop.Catalog.Grpc.Protos` for compatibility with the independent WinForms port.

The legacy WCF project, `CatalogService.svc` and the `<system.serviceModel>` configuration are
**left untouched**. This tree has no dependency on the other modernized solutions or on mock data.

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
| Non-positive identifier in `FindCatalogItem` | no match | `NOT_FOUND` |
| Negative catalog filter | empty list | `OK` with empty list |
| Any id with no stock row | `0` | `OK` with `0` |
| Negative stock or empty item name/picture | accepted | accepted; no extra validation or default picture substitution |
| Invalid decimal nanos or mixed units/nanos signs | not representable by SOAP decimal | `INVALID_ARGUMENT` |
| Unhandled server error | opaque fault | `UNKNOWN` (ASP.NET Core default; details stay off the wire) |

Null-return operations use `NOT_FOUND` explicitly. Proto3 nested messages retain presence;
`GetCatalogItems` leaves brand/type messages unset, whereas `FindCatalogItem` populates them.
Reads are ordered by Id; stock lookup chooses the first StockId and discount lookup the first Id.

## Hosting

`src/eShopWcf.Catalog.Grpc/Program.cs` hosts Kestrel HTTP/2, gRPC reflection and `/health`.
The default URL is `http://localhost:5200`; override with `--urls` or `ASPNETCORE_URLS`.
Health requests on this plaintext listener also require HTTP/2. Health is a host liveness check,
not an ongoing SQL readiness probe.

## Data-layer additions

`eShopWcf.Catalog.Data.CatalogService` implements all ten operations asynchronously with EF Core.
`ConnectionString` in the environment takes precedence over `ConnectionStrings:CatalogDb`.
The fallback is LocalDB `eShopDatabase` with integrated security and a trusted server certificate.
Only an absent database is created and seeded. Existing databases are used unchanged, even if
empty or incompatible; no migrations, schema repair or reseeding run against them.
`Seed:Dataset` is `Legacy` by default, or `Csv` for the canonical MVC CSV dataset (7 brands,
6 types, 13 items). CSV files are byte-preserved; values are trimmed after quoted-field parsing.
The seed option is only interpreted when creating a database. Both datasets retain the exact
legacy float-to-double discount values. Item and stock keys use max+1, not SQL identity or HiLo;
like the legacy implementation this is not a concurrent-writer key allocation strategy.

The SQL model matches the five catalog tables observed after invoking the real EF6 service.
String nullability, varchar(50) brands/types, money prices, SQL date fields and identity settings
are preserved. The only FKs are CatalogItems → CatalogBrands/CatalogTypes (cascade delete).
Stock deliberately has no FK to items. New databases omit EF6's private `__MigrationHistory`;
existing history is untouched. EF Core PK/FK constraint names may differ; column types, nullability,
key generation and index columns are equivalent. See `schema-compatibility.md`.

## Local demo

```bash
dotnet run --project src/eShopWcf.Catalog.Grpc
grpcurl -plaintext localhost:5200 list
grpcurl -plaintext localhost:5200 list eshop.catalog.v1.Catalog
grpcurl -plaintext -d '{"id": 1}' localhost:5200 eshop.catalog.v1.Catalog/FindCatalogItem
```
