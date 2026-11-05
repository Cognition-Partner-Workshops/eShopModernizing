# EF6 schema compatibility

The production legacy WCF service was hosted under IIS Express with `ConnectionString` pointing
to LocalDB database `eShopWcf_LegacySchema`. A SOAP 1.1 `GetCatalogBrands` request (not just WSDL)
returned HTTP 200 and caused EF6 `CreateDatabaseIfNotExists` to create and seed the database.
`GetCatalogItems(0,0)` was also invoked; its response contained null brand/type navigations.

Evidence on the verification VM is under `C:\Users\Administrator\evidence`:
`capture-legacy.ps1`, `legacy-brands.xml`, `legacy-items.xml`, and `legacy-schema.txt`.
The schema dump queries `INFORMATION_SCHEMA.COLUMNS`, `sys.foreign_keys`, and `sys.indexes`.
`compare-schema.sql` and `schema-comparison.txt` compare that database with a database created
by the EF Core initializer: both directions of the catalog column, index-column and FK comparisons
returned no differences (ignoring constraint names and excluding EF6 migration metadata).

## Catalog tables

| Table | Columns (SQL type; nullable only where marked) |
| --- | --- |
| dbo.CatalogBrands | Id int PK, non-identity; Brand varchar(50) NULL |
| dbo.CatalogTypes | Id int PK, non-identity; Type varchar(50) NULL |
| dbo.CatalogItems | Id int PK, non-identity; Description nvarchar(max) NULL; Name nvarchar(max) NULL; Price money (19,4); Picturefilename nvarchar(max) NULL; CatalogBrandId int; CatalogTypeId int |
| dbo.CatalogItemsStock | StockId int PK, non-identity; Date date; CatalogItemId int; AvailableStock int |
| dbo.DiscountItems | Id int IDENTITY PK; Size float(53); Start date; End date |

All other columns are NOT NULL. `CatalogDbContext` configures these exact table/column names,
types, nullability and key-generation strategies. In particular, stock has **no item foreign key**.

The two observed FKs are:

- `FK_dbo.CatalogItems_dbo.CatalogBrands_CatalogBrandId` → CatalogBrands.Id, ON DELETE CASCADE.
- `FK_dbo.CatalogItems_dbo.CatalogTypes_CatalogTypeId` → CatalogTypes.Id, ON DELETE CASCADE.

All five primary keys are unique clustered indexes. CatalogItems has nonunique nonclustered
`IX_CatalogBrandId` and `IX_CatalogTypeId`. EF Core creates the same index columns and delete rules.
Primary-key and foreign-key constraint names are EF Core conventions on newly created databases;
they differ from EF6 names and do not affect reads or writes.

EF6 additionally created `dbo.__MigrationHistory` with its clustered composite primary key
(MigrationId, ContextKey). This is framework metadata, not a catalog entity. EF Core neither maps
nor modifies it, and does not create it in a new database. No EF Core migrations/history table are used.
These metadata/constraint-name differences are deliberate; the five catalog table shapes match.

The initializer checks database existence before `EnsureCreated`, so even an existing empty or
incompatible database is used as-is. It must never apply `EnsureCreated` to an existing database.
The unit/integration tests pin that no-reseed behavior. Full cross-protocol parity on an
existing EF6-created database is a separate follow-up, not claimed by these tests.

## Seeds

`PreconfiguredData` is adapted directly from the legacy WCF source, retaining all IDs, prices,
pictures, stock dates/counts and float literals widened to double. The default has 5 brands,
4 types, 12 items, 6 stock rows and 6 discounts.

The MVC CSV files are copied byte-for-byte and marked `-text`. The CSV dataset has **7 brands,
6 types, 13 items**. IDs use one-based row order, all values are trimmed, quoted descriptions
remain one field, and `onreorder` is ignored. Stock uses 2017-09-20 and one row per item.
Discounts are the same six WCF discounts in both modes.
