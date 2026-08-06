# DJ-84 — locked catalog query contract

This file is a temporary coordination artifact for DJ-84 and is deleted before the
pull request is opened. It records the contract frozen by the coordinating session;
no workstream may change it without going back to that session.

## Query

`ViewModel/CatalogQuery.cs` — bound from the query string of `GET /Catalog/Index`:

| query string | property | type | default | meaning |
|---|---|---|---|---|
| `search`   | `Search`    | `string` | `null` | case-insensitive match on `CatalogItem.Name` OR `CatalogItem.Description`; null/whitespace means "no search" |
| `brandId`  | `BrandId`   | `int?`   | `null` | exact match on `CatalogItem.CatalogBrandId` |
| `typeId`   | `TypeId`    | `int?`   | `null` | exact match on `CatalogItem.CatalogTypeId` |
| `sort`     | `Sort`      | `string` | `name-asc` | one of `name-asc`, `name-desc`, `price-asc`, `price-desc`; unknown values fall back to `name-asc` via `CatalogSortOptions.Normalize` |
| `pageSize` | `PageSize`  | `int`    | `10` | |
| `pageIndex`| `PageIndex` | `int`    | `0`  | |

## Service

`Services/ICatalogService.cs` (frozen):

```csharp
CatalogItem FindCatalogItem(int id);
IEnumerable<CatalogBrand> GetCatalogBrands();
PaginatedItemsViewModel<CatalogItem> GetCatalogItemsPaginated(int pageSize, int pageIndex);
PaginatedItemsViewModel<CatalogItem> GetCatalogItemsPaginated(CatalogQuery query);
IEnumerable<CatalogType> GetCatalogTypes();
void CreateCatalogItem(CatalogItem catalogItem);
void UpdateCatalogItem(CatalogItem catalogItem);
void RemoveCatalogItem(CatalogItem catalogItem);
```

Semantics of the `CatalogQuery` overload:

- filter (search, brand, type) and sort are applied **before** `Skip`/`Take`;
- the returned `PaginatedItemsViewModel<CatalogItem>` reports `TotalItems` /
  `TotalPages` for the **filtered** set;
- sort is stable — ties break on `Id` ascending;
- the existing `(pageSize, pageIndex)` overload keeps its current behaviour
  (`OrderBy(Id)`, unfiltered) so nothing else in the app changes.

## View

`ViewModel/CatalogIndexViewModel.cs` is the model of `Views/Catalog/Index.cshtml`:

```csharp
PaginatedItemsViewModel<CatalogItem> Items;   // filtered + sorted page
IEnumerable<CatalogBrand> Brands;             // lookup list for the brand dropdown
IEnumerable<CatalogType> Types;               // lookup list for the type dropdown
CatalogQuery Query;                           // echoed query, for re-rendering the controls
```

`Views/Catalog/CatalogTable.cshtml` keeps `IEnumerable<CatalogItem>` as its model.

## Controller

`CatalogController.Index(CatalogQuery query)` on the existing route: normalizes
`Sort`, trims `Search`, calls the `CatalogQuery` overload, and returns a
`CatalogIndexViewModel` populated with the lookup lists and the echoed query.
With no query string the page must behave exactly as it does today.
