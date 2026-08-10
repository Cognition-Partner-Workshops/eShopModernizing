# Catalog database consolidation (NET-65)

The legacy estate runs on **two** catalog databases:

| Database | Used by | Schema |
| --- | --- | --- |
| `Microsoft.eShopOnContainers.Services.CatalogDb` | eShopLegacyMVC, eShopLegacyWebForms (retired in NET-70) | `Catalog`, `CatalogBrand`, `CatalogType` + the three `catalog_*_hilo` sequences |
| `eShopDatabase` | eShopWCFService (and, through it, eShopWinForms) | EF6-pluralized `CatalogItems`, `CatalogBrands`, `CatalogTypes` + the WCF-only `CatalogItemsStock` and `DiscountItems` |

The modernized estate has **one**. The EF Core model (NET-64) already reconciles both shapes onto
the MVC/Web Forms schema and adds the two WCF-only tables, so consolidation is a data move, not a
schema change.

## Target

One database — keep the MVC name (`Microsoft.eShopOnContainers.Services.CatalogDb`) or pick a new
one; every host reaches it through `ConnectionStrings__Catalog`. The legacy `ConnectionString`
environment variable the WCF service read is still honoured as a fallback (NET-61).

## Runbook

1. **Back up both databases.** The script only inserts, but the migration step alters the MVC
   database in place.

   ```bash
   sqlcmd -S "$SERVER" -Q "BACKUP DATABASE [Microsoft.eShopOnContainers.Services.CatalogDb] TO DISK='/var/opt/mssql/backup/catalogdb.bak'"
   sqlcmd -S "$SERVER" -Q "BACKUP DATABASE [eShopDatabase] TO DISK='/var/opt/mssql/backup/eshopdatabase.bak'"
   ```

2. **Bring the schema up to date.** Run the EF Core migrations against the database you are keeping.
   `InitialCreate` creates the tables, `AddCatalogHiLoSequences` creates `dbo.catalog_hilo`,
   `dbo.catalog_brand_hilo` and `dbo.catalog_type_hilo` with the legacy `START WITH 1 INCREMENT BY 10`.

   ```bash
   export ConnectionStrings__Catalog='Server=…;Database=Microsoft.eShopOnContainers.Services.CatalogDb;…'
   dotnet ef database update --project src/eShop.Catalog.Data
   ```

   An existing MVC database already has the three sequences (they were created by hand by the legacy
   initializer). Drop them first, or mark the migration as applied and keep the existing ones — they
   are byte-for-byte the same definition.

3. **Move the WCF-only data.**

   ```bash
   sqlcmd -S "$SERVER" -d master -b -i scripts/sql/consolidate-catalog-databases.sql \
     -v TargetDb="Microsoft.eShopOnContainers.Services.CatalogDb" WcfDb="eShopDatabase"
   ```

   The script matches brands, types and items on their names, inserts only what is missing, remaps
   `CatalogItemsStock.CatalogItemId` onto the consolidated item ids, and finally restarts the three
   sequences past the highest id in each table. It is idempotent: running it twice is a no-op.

4. **Verify.** The script prints the row counts; compare them against the two source databases:

   ```sql
   SELECT COUNT(*) FROM dbo.Catalog;            -- ≥ MVC item count
   SELECT COUNT(*) FROM dbo.CatalogItemsStock;  -- = eShopDatabase stock count
   SELECT COUNT(*) FROM dbo.DiscountItems;      -- = eShopDatabase discount count
   SELECT NEXT VALUE FOR dbo.catalog_hilo;      -- > MAX(Catalog.Id)
   ```

5. **Repoint and retire.** Set `ConnectionStrings__Catalog` for every modernized host to the
   consolidated database and take `eShopDatabase` offline once the WCF service is cut over (NET-66).

## Fresh environments

Nothing to consolidate: start the host with `Catalog:UseMockData=false` and a connection string.
`CatalogDatabaseInitializer` applies the migrations and seeds the catalog on startup (disable with
`Catalog:InitializeDatabaseOnStartup=false`), producing exactly the legacy default data set:

* 4 catalog types (ids 1–4), 5 brands (ids 1–5), 12 items (ids 1–12);
* with `Catalog:UseCustomizationData=true`, the `Setup/*.csv` data instead: 6 types, 7 brands,
  13 items, still with sequence-allocated ids.

Seeding is idempotent — each table is filled only when it is empty — so restarts and multiple
replicas converge instead of duplicating rows.

### Pictures

`Catalog:UseCustomizationData=true` also extracts `Setup/CatalogItems.zip` into
`Catalog:PicturesDirectory` (default `<base directory>/Pics`), replacing its contents, exactly as the
legacy `AddCatalogItemPictures` did. The 1.9 MB archive is **not** duplicated into the modernized
projects: copy it from `eShopLegacyMVCSolution/src/eShopLegacyMVC/Setup/CatalogItems.zip` into the
setup directory of the deployment (or point `Catalog:SetupDirectory` at it). A missing archive is
logged as a warning and skipped rather than failing start-up.
