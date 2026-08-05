# WinForms desktop client — retirement record

**Decision:** D-07 ([NET-58](https://cognition-partner-workshops.atlassian.net/browse/NET-58)) — the WinForms client
is retired. It is not rebuilt as a `net8.0-windows` application; its workflow is served by the web UI plus the
cross-platform gRPC CLI sample.
**Ticket:** NET-68 (I-09).
**Legacy source:** `eShopLegacyNTier/src/eShopWinForms` (stays on `main` until cutover; nothing was deleted).
**Replacement:** `eShopModernized/samples/eShop.Catalog.GrpcClient` against `eShopModernized/src/eShop.Catalog.Grpc`.

Per D-07 and D-08 (with contradiction C-13 reconciled) this ticket adds **no `net8.0-windows` project and no Windows
CI job**: the modernized estate is Linux containers, and the existing `windows-latest` job stays only as long as the
legacy MVC solution is on `main`.

## 1. What the retired client was

| Aspect | Value |
| --- | --- |
| Project | `eShopWinForms.csproj`, non-SDK, `net472`, WinForms |
| Entry point | `Program.cs` — constructs `CatalogView`, a generated `CatalogServiceClient` and `CatalogController`, then `ShowDialog()` |
| Architecture | Model-View-Presenter: `Controllers/CatalogController.cs` + `Controllers/ICatalogView.cs` + `Views/CatalogView.cs` |
| Service access | Generated WCF proxy in `Connected Services/eShopServiceReference/Reference.cs`, `basicHttpBinding` to `http://localhost:62314/CatalogService.svc` (hard-coded in `App.config`) |
| Windows | One form, `eShop WinForms`, with two tabs: `Main Catalog ` and `Inventory` |
| Assets | Catalog images loaded from disk relative to the executable (`..\..\Assets\Images\Catalog\<PictureFileName>`) |
| Dead weight | References EntityFramework 6.1.3, `EntityFramework.SqlServer`, Newtonsoft.Json 6.0.4 and `System.Net.Http.Formatting` that the client never uses; `Helpers/UploadImageHelper.cs` is UWP code (`Windows.Storage`) that is not compiled in |

### 1.1 Screens and interactions

| Screen / control | User action | Presenter method | SOAP operation |
| --- | --- | --- | --- |
| App start | launch | `CatalogController.LoadView` | `GetDiscount`, `GetCatalogItems(0,0)`, `GetCatalogBrands`, `GetCatalogTypes`, `GetCatalogItems(0,0)` |
| Main Catalog → discount banner | — (on load) | `CheckForDiscounts` | `GetDiscount(DateTime.Now)` |
| Main Catalog → product grid (thumbnail, id, name, description, discounted price) | — (on load / on filter) | `LoadCatalogItems` | `GetCatalogItems(brandId, typeId)` + `GetDiscount(DateTime.Now)` |
| Main Catalog → `Brand` drop-down | select | `filterChanged` → `LoadCatalogItems` | `GetCatalogBrands` (fill), `GetCatalogItems` (apply) |
| Main Catalog → `Type` drop-down | select | `filterChanged` → `LoadCatalogItems` | `GetCatalogTypes` (fill), `GetCatalogItems` (apply) |
| Inventory → product list box (`"{id} - {name}"`) and `Product Id` combo | — (on load) | `SetShipmentView` | `GetCatalogItems(0,0)` |
| Inventory → month calendar + `Search` | click | `searchStockAvailable` → `ShowStockAvailability` | `GetAvailableStock(date, itemId)` |
| Inventory → `Results` list view (date, id, availability) | — (after Search) | `ShowStockAvailability` | — (render only) |
| Inventory → `Add new shipments here:` (`Product Id`, `Quantity`, `Shipment Arrival Date`) + `Add Shipment` | click | `addAvailability` → `NotifyAvailabilityUpdated` | `CreateAvailableStock(stock)`, then a message box |

The client therefore called **6** of the 10 WCF operations. `FindCatalogItem`, `CreateCatalogItem`,
`UpdateCatalogItem` and `RemoveCatalogItem` had no desktop consumer (contradiction C-08).

## 2. Capability → replacement map

CLI verbs below are `eShop.Catalog.GrpcClient <verb>`; web UI routes are the catalog MVC routes that NET-69 ports
into `eShopModernized/src/eShop.Web` (at the time of writing that project is still the bare MVC template, so only
the CLI replacements are verified here).

| # | Legacy WinForms capability | SOAP operation | Modernized replacement | Gap? |
| --- | --- | --- | --- | --- |
| 1 | Browse the catalog | `GetCatalogItems` | Web UI `GET /Catalog` (paginated grid) · CLI `get-items [brandId] [typeId]` · RPC `CatalogService.GetCatalogItems` | No |
| 2 | Filter by brand | `GetCatalogBrands` + `GetCatalogItems` | CLI `get-brands`, then `get-items <brandId> 0` · RPC `GetCatalogBrands` | Partial — the modernized web UI paginates instead of filtering; the filter survives in the CLI (gap G-1) |
| 3 | Filter by type | `GetCatalogTypes` + `GetCatalogItems` | CLI `get-types`, then `get-items 0 <typeId>` · RPC `GetCatalogTypes` | Partial — same as #2 (gap G-1) |
| 4 | See the running discount as a banner | `GetDiscount` | CLI `get-discount [yyyy-MM-dd\|today]` · RPC `GetDiscount` | **Yes — no web UI equivalent** (contradiction C-14, gap G-1) |
| 5 | See prices with the discount applied | `GetDiscount` + `GetCatalogItems` | CLI `catalog [brandId] [typeId]` (banner + discounted grid, the whole Main Catalog tab) | **Yes — no web UI equivalent** (C-14, gap G-1) |
| 6 | Look up the stock available for an item on a date | `GetAvailableStock` | CLI `get-stock <itemId> <yyyy-MM-dd\|today>` · RPC `GetAvailableStock` | **Yes — no web UI equivalent** (C-14, gap G-1) |
| 7 | Record a shipment (item, date, quantity), overwriting the row for that date | `CreateAvailableStock` | CLI `create-stock <itemId> <date> <quantity>` · CLI `inventory <itemId> <date> <quantity>` (the whole Inventory tab) · RPC `CreateAvailableStock` | **Yes — no web UI equivalent** (C-14, gap G-1) |
| 8 | Pick a product for a shipment from a `"{id} - {name}"` list | `GetCatalogItems` | CLI `inventory` prints the same list before recording | No |
| 9 | See a confirmation that the shipment was stored | — (message box) | CLI prints `Shipment has been added to the database.` and exits 0 | No |
| 10 | See catalog thumbnails in the grid | — (local files) | Web UI catalog page · API `GET /items/{id}/pic`; the CLI prints `PictureFileName` only | Yes — text UI cannot render images (gap G-4, accepted) |
| 11 | Point the client at a service endpoint | `App.config` `<endpoint address>` | CLI `--address <url>` (default `http://localhost:5095`) | No — and no longer hard-coded |
| 12 | (not in the desktop UI) look up one item | `FindCatalogItem` | CLI `find-item <id>` · Web UI `GET /Catalog/Details/{id}` | No |
| 13 | (not in the desktop UI) create / edit / delete an item | `CreateCatalogItem`, `UpdateCatalogItem`, `RemoveCatalogItem` | CLI `create-item`, `update-item`, `remove-item` · Web UI `Create`/`Edit`/`Delete` | No |

Every operation the WinForms client called has a verb; the CLI additionally covers the four operations it never
called, so the full legacy contract stays exercisable from Linux, macOS and Windows.

## 3. Contradiction C-14 — how it is resolved

C-14: *"the WinForms client exposes stock-shipment and discount features (`CreateAvailableStock`,
`GetAvailableStock`, `GetDiscount`) that the MVC UI does not implement at all"*, so retiring the desktop client
without a replacement would be a functional reduction.

Resolution: those three operations are first-class verbs of the gRPC CLI (`create-stock`, `get-stock`,
`get-discount`), plus two composite verbs that reproduce the two desktop tabs end-to-end (`catalog`, `inventory`).
They were exercised against a live service (section 6). **The reduction is therefore a UI reduction, not a
capability reduction:** the workflows survive, but as a command line rather than a Windows desktop screen, and no
browser-based screen exists for them. If a browser UI for inventory and discounts is required, it is new scope and
needs its own ticket — it is *not* covered by NET-69, which ports the existing MVC views.

## 4. Accepted deltas

| Delta | Legacy | Replacement | Rationale |
| --- | --- | --- | --- |
| Presentation | Windows desktop form, thumbnails, message boxes | Console text, one line per record | D-07: cross-platform, scriptable |
| Culture | `int.Parse` / `Convert.ToDateTime` / `ToString("F")` / `ToShortDateString()` on the operator's culture — `04/03/2026` meant different days on different desktops | Invariant everywhere; dates are `yyyy-MM-dd` (or `today`), prices are invariant decimals | Removes a real ambiguity; the strict parser rejects `04/03/2026` rather than guessing |
| Discount banner text | `"{0}% sale endson {1}!"` (missing space, short date) | `"15% sale ends on 2026-08-31!"` | Fixes the typo; ISO date. Percentage rounding keeps the legacy `Math.Round` midpoint-to-even behaviour (12.5% → 12%) |
| Discounted price | `double` arithmetic, `"$" + ToString("F")` | `decimal` arithmetic, `"$" + ToString("F2", invariant)` | Same displayed values without a `double` round-trip |
| Null results | `GetDiscount`/`FindCatalogItem` returned `null`; the form silently left the banner empty | `NOT_FOUND` (D-04); the CLI prints `No discount is running.` for the discount and exits 1 with the status for a missing item | Explicit, scriptable |
| Transport | SOAP 1.1 `basicHttpBinding`, generated proxy, hard-coded endpoint | gRPC over HTTP/2, contract compiled from `catalog.proto`, `--address` | D-04 |
| Packaging | Self-contained Windows executable (the old plan) | `dotnet` console sample inside the repository | D-07/D-08: no Windows publish job (gap G-5) |

## 5. Open gaps

| ID | Gap | Impact | Owner / next step |
| --- | --- | --- | --- |
| G-1 | No browser UI for discounts, stock look-up or shipments | Operators who used the desktop Inventory tab must use the CLI | Product decision; a new ticket if a web screen is wanted (out of scope for NET-69) |
| G-2 | The modernized seeder seeds no `DiscountItems` and no `CatalogItemsStock` rows | `get-discount` answers `No discount is running.` on a freshly seeded database; verification below had to insert a discount row by hand | Seeding owner (NET-65) — decide whether the legacy `PreconfiguredData` discount rows (all dated 2017) belong in the seed set |
| G-3 | `eShop.Catalog.Grpc` cannot run with `Catalog__UseMockData=true` | Every RPC fails with `Unable to resolve service for type 'eShop.Catalog.Data.CatalogDbContext'` because `AddCatalogData` registers no `DbContext` in mock mode while `CatalogGrpcService` requires one; the service must be run against SQL Server | gRPC host / data owner (NET-66, NET-65) — either register an in-memory `DbContext` for mock mode or drop the direct `CatalogDbContext` dependency |
| G-4 | Catalog images are not rendered by a console client | Cosmetic | Accepted; images are available from `GET /items/{id}/pic` and the web UI |
| G-5 | No packaged desktop/self-contained distribution | Operators need the .NET runtime or a container | Accepted per D-07/D-08 |

## 6. Sign-off evidence

Verified on Linux (.NET 8.0.423 SDK) against `eShop.Catalog.Grpc` on `http://localhost:5095`, backed by SQL Server
2022 in Docker, migrated and seeded by `eShop.Catalog.Api` (12 catalog items, 5 brands, 4 types) with one discount
row inserted by hand (`Size = 0.15`, `2026-08-01` → `2026-08-31`) because of gap G-2.

| Legacy capability | Command | Observed |
| --- | --- | --- |
| Brand filter list | `get-brands` | 5 brands (`Azure`, `.NET`, `Visual Studio`, `SQL Server`, `Other`) |
| Type filter list | `get-types` | 4 types (`Mug`, `T-Shirt`, `Sheet`, `USB Memory Stick`) |
| Catalog grid | `get-items` | 12 items, ordered by id, prices `$19.50`… |
| Brand + type filter | `get-items 2 2` | 3 items (ids 1, 4, 6) |
| Discount banner | `get-discount 2026-08-05` | `15% sale ends on 2026-08-31!` |
| No discount | `get-discount 2020-01-01` | `No discount is running.` (exit 0) |
| Stock look-up, no row | `get-stock 3 2026-08-05` | `2026-08-05  item 3  available 0` (legacy answered 0, not an error) |
| Add shipment | `create-stock 3 2026-08-05 25` → `get-stock 3 2026-08-05` | `available 25` |
| Overwrite the same date | `create-stock 3 2026-08-05 40` → `get-stock 3 today` | `available 40` (legacy overwrite semantics) |
| Whole Main Catalog tab | `catalog` | brands, types, banner, and the 12 items with the 15% discount applied (`$19.50` → `$16.58`) |
| Whole Inventory tab | `inventory 4 2026-08-06 12` | product picker, `Shipment has been added to the database.`, `2026-08-06  item 4  available 12` |
| Item CRUD (no desktop screen) | `create-item` → `get-items 1 1` → `update-item` → `find-item` → `remove-item` → `find-item` | created id 13, updated to `$11.25`, removed, then `NotFound` (exit 1) |
| Culture-safe parsing | `create-stock 3 05/08/2026 25` | rejected with `'date' must be a yyyy-MM-dd date or 'today'` (exit 2) |
| Argument validation | `create-stock 3` / `create-stock 3 2026-08-05 abc` / `frobnicate` | named error + usage, exit 2 |

The full transcript is in the pull request for NET-68. Unit tests for the argument parsing and formatting live in
`eShopModernized/tests/eShop.Catalog.GrpcClient.Tests` (the network calls are not mocked; they are covered by the
transcript above and by the `eShop.Catalog.Grpc.Tests` integration tests).

**Sign-off:** the WinForms workflow is reproduced cross-platform, with G-1 (no browser screen for discounts and
inventory) recorded as the one accepted functional reduction.
