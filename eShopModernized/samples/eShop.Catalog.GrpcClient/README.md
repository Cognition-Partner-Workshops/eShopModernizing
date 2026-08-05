# eShop.Catalog.GrpcClient

Cross-platform console client for `eShop.Catalog.Grpc`. It replaces the retired WinForms desktop client
(decision D-07) and has a verb for every operation of the legacy WCF `ICatalogService`, including the
stock-shipment and discount workflows the web UI does not implement — see
[`docs/winforms-retirement.md`](../../docs/winforms-retirement.md).

```
dotnet run --project samples/eShop.Catalog.GrpcClient -- [--address <url>] <command> [arguments]
```

`--address` defaults to `http://localhost:5095`.

| Command | Legacy operation |
| --- | --- |
| `get-brands` | `GetCatalogBrands` |
| `get-types` | `GetCatalogTypes` |
| `get-items [brandId] [typeId]` (`0` = no filter) | `GetCatalogItems` |
| `find-item <id>` | `FindCatalogItem` |
| `get-stock <itemId> <yyyy-MM-dd\|today>` | `GetAvailableStock` |
| `create-stock <itemId> <yyyy-MM-dd\|today> <quantity>` | `CreateAvailableStock` |
| `get-discount [yyyy-MM-dd\|today]` | `GetDiscount` |
| `create-item <name> <price> <brandId> <typeId> [description] [picture]` | `CreateCatalogItem` |
| `update-item <id> <name> <price> <brandId> <typeId> [description] [picture]` | `UpdateCatalogItem` |
| `remove-item <id>` | `RemoveCatalogItem` |
| `catalog [brandId] [typeId]` | the WinForms *Main Catalog* tab (brands, types, banner, discounted grid) |
| `inventory <itemId> <yyyy-MM-dd\|today> <quantity>` | the WinForms *Inventory* tab (picker, shipment, availability) |

Dates are `yyyy-MM-dd` (or `today`, UTC) and prices are invariant-culture decimals on every host locale — unlike the
desktop client, which parsed and rendered them with the operator's culture. Exit codes: `0` success, `1` RPC error
(the gRPC status is printed), `2` usage error.

Example:

```
$ dotnet run --project samples/eShop.Catalog.GrpcClient -- inventory 4 2026-08-06 12
Products:
  1 - .NET Bot Black Hoodie
  ...
Shipment has been added to the database.
2026-08-06  item 4  available 12
```

The host must be run against a real database: `Catalog__UseMockData=true` is not currently supported by
`eShop.Catalog.Grpc` (gap G-3 in the retirement document).
