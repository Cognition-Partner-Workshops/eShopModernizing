/*
  Consolidates the two legacy catalog databases into the single modernized catalog database.

    source A : Microsoft.eShopOnContainers.Services.CatalogDb  (MVC + Web Forms, EF6)
    source B : eShopDatabase                                   (WCF service, EF6)
    target   : the database the EF Core migrations created (defaults to source A's name)

  Source A already has the target shape (Catalog / CatalogBrand / CatalogType plus the three
  sequences), so consolidation means: run the EF Core migrations against it, then carry over the
  rows that only exist in source B — the WCF-only CatalogItemsStock and DiscountItems tables, and
  any catalog rows the WCF database has that the MVC one does not.

  The script is idempotent: rows are matched on their natural keys (brand, type, item name) and
  only inserted when missing, so re-running it changes nothing.

  Run it with sqlcmd, after `dotnet ef database update`:

    sqlcmd -S <server> -d master -b -i scripts/sql/consolidate-catalog-databases.sql \
      -v TargetDb="Microsoft.eShopOnContainers.Services.CatalogDb" \
         WcfDb="eShopDatabase"

  See modernization/database-consolidation.md for the full runbook.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_ID('$(TargetDb)') IS NULL
    THROW 50001, 'Target database does not exist. Run the EF Core migrations first.', 1;

IF DB_ID('$(WcfDb)') IS NULL
BEGIN
    PRINT 'WCF database $(WcfDb) not found - nothing to consolidate.';
    RETURN;
END;

USE [$(TargetDb)];
GO

IF OBJECT_ID('dbo.Catalog', 'U') IS NULL
    THROW 50002, 'Target database has no dbo.Catalog table. Run the EF Core migrations first.', 1;

IF OBJECT_ID('dbo.catalog_hilo', 'SO') IS NULL
    THROW 50003, 'Target database has no dbo.catalog_hilo sequence. Run the EF Core migrations first.', 1;

BEGIN TRANSACTION;

/* ---------------------------------------------------------------------------
   1. Brands and types. The WCF database uses the pluralized EF6 table names and
      varchar columns; the target keeps the MVC shape. Match on the name, and let
      the identity columns number whatever has to be inserted.
   --------------------------------------------------------------------------- */

INSERT INTO dbo.CatalogBrand (Brand)
SELECT CAST(src.Brand AS nvarchar(100))
FROM [$(WcfDb)].dbo.CatalogBrands AS src
WHERE NOT EXISTS (SELECT 1 FROM dbo.CatalogBrand AS dst WHERE dst.Brand = src.Brand);

INSERT INTO dbo.CatalogType (Type)
SELECT CAST(src.Type AS nvarchar(100))
FROM [$(WcfDb)].dbo.CatalogTypes AS src
WHERE NOT EXISTS (SELECT 1 FROM dbo.CatalogType AS dst WHERE dst.Type = src.Type);

/* ---------------------------------------------------------------------------
   2. Catalog items that only exist in the WCF database. Their ids come from the
      catalog_hilo sequence so the consolidated database keeps a single id
      authority. The WCF model spells the picture column Picturefilename and has
      no stock columns; the target defaults are used for those.
   --------------------------------------------------------------------------- */

DECLARE @missingItems TABLE
(
    RowNumber       int IDENTITY(1, 1) PRIMARY KEY,
    Name            nvarchar(50)   NOT NULL,
    Description     nvarchar(max)  NULL,
    Price           decimal(18, 2) NOT NULL,
    PictureFileName nvarchar(max)  NOT NULL,
    CatalogTypeId   int            NOT NULL,
    CatalogBrandId  int            NOT NULL
);

INSERT INTO @missingItems (Name, Description, Price, PictureFileName, CatalogTypeId, CatalogBrandId)
SELECT
    CAST(src.Name AS nvarchar(50)),
    src.Description,
    CAST(src.Price AS decimal(18, 2)),
    src.Picturefilename,
    dstType.Id,
    dstBrand.Id
FROM [$(WcfDb)].dbo.CatalogItems AS src
JOIN [$(WcfDb)].dbo.CatalogTypes  AS srcType  ON srcType.Id  = src.CatalogTypeId
JOIN [$(WcfDb)].dbo.CatalogBrands AS srcBrand ON srcBrand.Id = src.CatalogBrandId
JOIN dbo.CatalogType  AS dstType  ON dstType.Type   = srcType.Type
JOIN dbo.CatalogBrand AS dstBrand ON dstBrand.Brand = srcBrand.Brand
WHERE NOT EXISTS (SELECT 1 FROM dbo.Catalog AS dst WHERE dst.Name = src.Name);

DECLARE @missingItemCount int = (SELECT COUNT(*) FROM @missingItems);

IF @missingItemCount > 0
BEGIN
    /* The block of ids starts at a value taken from the sequence, exactly like the
       application's HiLo generator; step 4 then moves the sequence past the block. */
    DECLARE @firstId int = CAST(NEXT VALUE FOR dbo.catalog_hilo AS int);

    INSERT INTO dbo.Catalog
        (Id, Name, Description, Price, PictureFileName, CatalogTypeId, CatalogBrandId,
         AvailableStock, RestockThreshold, MaxStockThreshold, OnReorder)
    SELECT
        @firstId + RowNumber - 1,
        Name,
        Description,
        Price,
        PictureFileName,
        CatalogTypeId,
        CatalogBrandId,
        0,
        0,
        0,
        0
    FROM @missingItems;
END;

/* ---------------------------------------------------------------------------
   3. WCF-only tables. CatalogItemsStock references catalog items by id, so the
      ids are remapped through the item name.
   --------------------------------------------------------------------------- */

INSERT INTO dbo.CatalogItemsStock (StockId, Date, CatalogItemId, AvailableStock)
SELECT src.StockId, src.Date, dstItem.Id, src.AvailableStock
FROM [$(WcfDb)].dbo.CatalogItemsStock AS src
JOIN [$(WcfDb)].dbo.CatalogItems AS srcItem ON srcItem.Id = src.CatalogItemId
JOIN dbo.Catalog AS dstItem ON dstItem.Name = srcItem.Name
WHERE NOT EXISTS (SELECT 1 FROM dbo.CatalogItemsStock AS dst WHERE dst.StockId = src.StockId);

INSERT INTO dbo.DiscountItems (Size, Start, [End])
SELECT src.Size, src.Start, src.[End]
FROM [$(WcfDb)].dbo.DiscountItems AS src
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.DiscountItems AS dst
    WHERE dst.Size = src.Size AND dst.Start = src.Start AND dst.[End] = src.[End]);

/* ---------------------------------------------------------------------------
   4. Move the item sequence past every id in the consolidated table, so the
      application never allocates an id that is already taken. CatalogBrand.Id and
      CatalogType.Id are identity columns and keep their own counters.
   --------------------------------------------------------------------------- */

DECLARE @maxItemId int = ISNULL((SELECT MAX(Id) FROM dbo.Catalog), 0);

DECLARE @restartSql nvarchar(max) =
    N'ALTER SEQUENCE dbo.catalog_hilo RESTART WITH ' + CAST(@maxItemId + 1 AS nvarchar(20)) + N';';

EXEC sp_executesql @restartSql;

COMMIT TRANSACTION;

DECLARE @items int = (SELECT COUNT(*) FROM dbo.Catalog);
DECLARE @brands int = (SELECT COUNT(*) FROM dbo.CatalogBrand);
DECLARE @types int = (SELECT COUNT(*) FROM dbo.CatalogType);
DECLARE @stock int = (SELECT COUNT(*) FROM dbo.CatalogItemsStock);
DECLARE @discounts int = (SELECT COUNT(*) FROM dbo.DiscountItems);

PRINT 'Consolidation complete.';
PRINT '  catalog items  : ' + CAST(@items AS varchar(20));
PRINT '  catalog brands : ' + CAST(@brands AS varchar(20));
PRINT '  catalog types  : ' + CAST(@types AS varchar(20));
PRINT '  stock rows     : ' + CAST(@stock AS varchar(20));
PRINT '  discount rows  : ' + CAST(@discounts AS varchar(20));
GO
