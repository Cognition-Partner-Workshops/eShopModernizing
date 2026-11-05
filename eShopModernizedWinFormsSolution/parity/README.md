# winforms-parity-v1

A component-specific **desktop comparison fixture**, not a replacement for the MVC customization CSVs or a schema migration. Legacy WCF `PreconfiguredData` and all application code remain unchanged. The legacy database starts from the unmodified EF6 WCF baseline; the modern database starts from PR #226's default EF Core seed (`Catalog:UseMockData=false`, `UseMockData=false`, `Catalog:UseCustomizationData=false`).

## Dataset mapping

| Aspect | Legacy WCF seed | Canonical MVC Setup CSVs | #226 default database seed | winforms-parity-v1 |
|---|---|---|---|---|
| Brands | 5: Azure, .NET, Visual Studio, SQL Server, Other | 7; adds CatalogBrandTestOne/Two; source has whitespace on some names | Same 5 as WCF | Same 5 as WCF |
| Types | 4: Mug, T-Shirt, Sheet, USB Memory Stick | 6; adds CatalogTypeTestOne/Two | Same 4 as WCF | Same 4 as WCF |
| Items | 12, application-assigned IDs 1..12 | 13 data rows; extra `pepito` / `De los Palotes` | 12, IDs 1..12 | WCF's 12 items on both servers |
| First description | .NET Bot Black Hoodie | .NET Bot Black Hoodie, and more | .NET Bot Black Hoodie | .NET Bot Black Hoodie |
| Item 12 name/description | Cup<T> TShirt | Prism White TShirt | Prism White TShirt | Cup<T> TShirt |
| Pictures | WCF mapping below | 1.png..12.png, then 12.png for extra row | 1.png..12.png | WCF mapping below |
| Dated stock | 6 rows | Not represented | CatalogItemsStock empty | Exact WCF 6 rows |
| Discounts | 6 historical 2017 rows | Not represented | DiscountItems empty | Exact WCF 6 rows + row 7, 2026-01-01..2030-12-31, size 0.15 |
| Modern-only item fields | Absent | Available stock varies, second item OnReorder=true | AvailableStock=100; thresholds=0; OnReorder=false | Modern values unchanged; excluded from shared comparison |

Canonical CSV row order is shown for comparison; CSVs do not contain explicit item IDs. Shared numeric prices and brand/type IDs match between the two default server seeds and are not updated by this fixture.

| WCF ID | WCF name and description / parity name and description | WCF / parity picture | MVC CSV / #226 picture |
|---:|---|---|---|
| 1 | .NET Bot Black Hoodie | 2.png | 1.png |
| 2 | .NET Black & White Mug | 11.png | 2.png |
| 3 | Prism White T-Shirt | 7.png | 3.png |
| 4 | .NET Foundation T-shirt | 5.png | 4.png |
| 5 | Roslyn Red Sheet | 9.png | 5.png |
| 6 | .NET Blue Hoodie | 1.png | 6.png |
| 7 | Roslyn Red T-Shirt | 6.png | 7.png |
| 8 | Kudu Purple Hoodie | 3.png | 8.png |
| 9 | Cup<T> White Mug | 12.png | 9.png |
| 10 | .NET Foundation Sheet | 8.png | 10.png |
| 11 | Cup<T> Sheet | 10.png | 11.png |
| 12 | Cup<T> TShirt | 4.png | 12.png |

Legacy stock `(StockId, date, item, quantity)`:

```text
1  2017-09-20  1  100
2  2017-09-21  1  120
3  2017-09-22  1   80
4  2017-09-20  2   45
5  2017-09-25  4   65
6  2017-09-28  5   22
```

Discount sizes preserve the actual WCF binary32-to-binary64 values, **not rounded decimal approximations**:

| ID | Start | End | Stored float(53), round-trip representation |
|---:|---|---|---|
| 1 | 2017-09-18 | 2017-09-21 | 0.30000001192092896 |
| 2 | 2017-09-22 | 2017-09-26 | 0.25 |
| 3 | 2017-09-27 | 2017-09-30 | 0.10000000149011612 |
| 4 | 2017-10-05 | 2017-10-20 | 0.5 |
| 5 | 2017-11-13 | 2017-11-25 | 0.30000001192092896 |
| 6 | 2017-12-20 | 2017-12-25 | 0.25 |
| 7 | 2026-01-01 | 2030-12-31 | 0.15 |

## Apply and reset on the comparison VM

Stop both servers first. Restore `eShopWinForms_Legacy` from `C:\Users\Administrator\ext\db\eShopWinForms_Legacy_baseline.bak` using `reset-legacy.cmd`. Then, from this directory:

```powershell
$sql = 'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\180\Tools\Binn\SQLCMD.EXE'
& $sql -S '(localdb)\MSSQLLocalDB' -d eShopWinForms_Legacy -b -l 30 -t 60 -i fixtures\winforms-parity-v1.legacy.sql
& $sql -S '(localdb)\MSSQLLocalDB' -d eShopWinForms_Grpc -b -l 30 -t 60 -i fixtures\winforms-parity-v1.modern.sql
```

Check each `$LASTEXITCODE` before continuing. Scripts are transactional and database-name guarded. Legacy replaces only discount ID 7; modern replaces stock IDs 1..6 and discount IDs 1..7, and updates the twelve items' desktop text/pictures. Explicit discount IDs support either identity or nonidentity keys. Unrelated rows are not deleted: start from the stated baselines for deterministic results. Reapplication is idempotent. Modern-only columns, IDs, prices, brand/type IDs, and all source seed files are untouched.

VM parity snapshots are `C:\Users\Administrator\ext\db\eShopWinForms_Legacy_parity_v1.bak` and `eShopWinForms_Grpc_parity_v1.bak`. `reset-parity.cmd` in that directory stops the copied-site IIS Express and catalog DLL processes and restores both snapshots WITH REPLACE. It leaves servers stopped. Run `start-servers.ps1` there to launch IIS Express on 62314 and gRPC on 5200; it waits for a successful SOAP GetCatalogBrands response and gRPC reflection, with a bounded startup deadline. Logs and requests/responses are under `C:\Users\Administrator\evidence\servers`.

## Normalized comparison

`normalize-parity.ps1` in the evidence directory produces one complete JSON object per database with brands, types, desktop-visible item fields, dated stock, and discounts. Arrays sort by primary key; object fields have fixed order; prices are invariant four-place decimal strings; dates are `yyyy-MM-dd`; discount Size is an invariant round-trip double **string** to preserve exact stored float values. Encoding is UTF-8 without BOM, compact JSON plus one LF. Migration metadata, identity/sequence state, navigation objects and modern-only fields are intentionally excluded.

Both shared datasets SHA256:

```text
82ED9FFC8320B06D7F7CB5CE7D898B49DE90B1DCF73CB5C04C41D2F5F24E186A
```

This establishes data parity, not schema compatibility or identical SOAP/gRPC serialization. SQL money(19,4) versus decimal(18,2), plural versus singular tables, identity/sequence differences and navigation population still differ. Banner/price rendering in desktop clients requires a separate visual check.

## File SHA256

Canonical sources are byte hashes of `eShopLegacyMVCSolution/src/eShopLegacyMVC/Setup/` files (including original whitespace and line endings), not hashes of normalized CSV values.

| File | SHA256 |
|---|---|
| CatalogBrands.csv | 5902017931EB5ADA935BC42A08189D547DC27A889F2BA7C1ACCADAA1506BB76E |
| CatalogItems.csv | 630D09D9F9956640CC4A0E04EF8D7BE75E8119405C74598AAF2B5EB62415BDD2 |
| CatalogTypes.csv | E6BE222C6C20DFAE2AB66399AB58B62033B6E5173872C02D9DDDE57E57FFBF95 |
| fixtures/winforms-parity-v1.legacy.sql | 532763BAB6AB98C9450C6F0B59F9A0FA59A989D8E27B2B3B5BCEB132A12D215D |
| fixtures/winforms-parity-v1.modern.sql | 79FEF6CE8A87D6A166B7DDE2609E969BABB5477EDF3969BA6F0829577591C5AA |

Recompute SQL hashes if editing the fixtures or changing their line endings.
