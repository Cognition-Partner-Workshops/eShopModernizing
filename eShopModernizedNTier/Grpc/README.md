# Standalone WCF-to-gRPC catalog (.NET 8)

This service ports the WCF contract of a legacy .NET line-of-business app.

Build and test from the repository root:

```sh
dotnet build eShopModernizedNTier/Grpc/eShopGrpc.sln -c Release
dotnet test eShopModernizedNTier/Grpc/eShopGrpc.sln -c Release
dotnet format eShopModernizedNTier/Grpc/eShopGrpc.sln --verify-no-changes
dotnet run --project eShopModernizedNTier/Grpc/src/eShop.Catalog.Grpc
grpcurl -plaintext localhost:5199 list
grpcurl -plaintext -d '{"id":1}' localhost:5199 eshop.catalog.v1.Catalog/FindCatalogItem
```

The folder-local SDK (8.0.427, latestFeature), build props and central package
versions isolate this solution from repository-wide configuration. No shared
host library or root solution is required. Source was extracted from PR #226.

## Configuration

UseMockData defaults to true in appsettings.json. Set UseMockData=false
(environment variable: UseMockData=false) and ConnectionStrings__Catalog for SQL
Server mode. Mock services are singleton, mutable in-memory stores reset on
restart. They use exactly the WCF seed: 12 items, 5 brands, 4 types, 6 stock rows,
and 6 discounts. Unlike the MVC seed in #226, picture filenames and item 12
match WCF; stock is date-specific, not a constant quantity on each item.
Discount IDs are 1..6 in seed order, matching legacy SQL identity assignment at runtime.

Kestrel listens on http://localhost:5199 using HTTP/2 only (h2c). Reflection is
enabled in all environments; grpcurl needs -plaintext. This local demo endpoint
has no TLS or authentication and should not be exposed publicly.

## Contract and parity

Protos/catalog.proto in eShop.Catalog.Grpc.Contracts exposes all ten operations:

- FindCatalogItem
- GetCatalogBrands
- GetCatalogItems
- GetCatalogTypes
- GetAvailableStock
- CreateAvailableStock
- CreateCatalogItem
- UpdateCatalogItem
- RemoveCatalogItem
- GetDiscount

GetCatalogItems uses 0 for no filter and combines nonzero brand/type filters.
CreateCatalogItem ignores the caller ID and uses max(Id)+1. Update and remove
persist until restart. CreateAvailableStock overwrites the first matching item
and calendar date; otherwise StockId is max+1. Missing stock returns 0.
Discount ranges include both endpoints. Dates use UTC protobuf timestamps;
only their calendar-date component is significant. Prices use exact units/nanos.

Explicit transport differences from WCF CatalogService.svc.cs:
- FindCatalogItem and GetDiscount return NotFound instead of legacy null.
- Missing update/remove targets return NotFound rather than an EF/WCF fault.
- Invalid/nonpositive IDs, negative filters/stock, missing payloads/dates and
  blank create names return InvalidArgument instead of generic WCF faults or
  permissive legacy behavior.
- Unhandled database/runtime failures use the standard gRPC Unknown status.

Remaining behavioral differences observed in the parity run:
- GetCatalogItems populates brand/type navigation properties on the port; the
  legacy operation returns null navigation properties. The underlying item IDs,
  names, prices, picture filenames and foreign keys match.
- Legacy RemoveCatalogItem always faults with InternalServiceFault because it
  removes a detached entity: EF6 reports that the "object cannot be deleted
  because it was not found in the ObjectStateManager". The port removes the
  item successfully. Consequently, the legacy item remains readable and its
  final count is 13; the port returns NotFound and its final count is 12.

Run parity/compare.ps1 against existing parity/out/legacy.json and port.json to
regenerate parity.md, parity-rows.json and fingerprint.json without calling
either service. Prices are compared numerically, so decimal scale alone is not
a difference. MATCH means equal full normalized results; MAPPED means the
documented FindCatalogItem/GetDiscount null-to-NotFound conversion; DIFF marks
other differences and includes a reason. The fingerprint compares brands
(id:name), types (id:name) and every initial item ID/name/price on both sides.
All generated parity/out files and build bin/obj directories are gitignored.

The retained EF Core backend and CSV seeding are from #226; the strict WCF seed
parity guarantee applies to mock mode. Database mode requires SQL Server and is
not exercised against a live server by this solution's tests.
