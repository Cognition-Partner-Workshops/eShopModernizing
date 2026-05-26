-- V1__create_schema.sql
-- Flyway migration: creates CatalogType, CatalogBrand, and Catalog tables
-- with HiLo sequences for SQL Server (T-SQL).

-- HiLo sequences for ID generation
CREATE SEQUENCE catalog_hilo
    START WITH 100
    INCREMENT BY 10;

CREATE SEQUENCE catalog_brand_hilo
    START WITH 100
    INCREMENT BY 10;

CREATE SEQUENCE catalog_type_hilo
    START WITH 100
    INCREMENT BY 10;

-- CatalogType lookup table
CREATE TABLE CatalogType (
    Id    INT           NOT NULL IDENTITY(1,1),
    [Type] NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_CatalogType PRIMARY KEY (Id)
);

-- CatalogBrand lookup table
CREATE TABLE CatalogBrand (
    Id    INT           NOT NULL IDENTITY(1,1),
    Brand NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_CatalogBrand PRIMARY KEY (Id)
);

-- Catalog (CatalogItem) table
CREATE TABLE Catalog (
    Id               INT            NOT NULL,
    Name             NVARCHAR(50)   NOT NULL,
    Description      NVARCHAR(MAX)  NULL,
    Price            DECIMAL(18,2)  NOT NULL,
    PictureFileName  NVARCHAR(255)  NOT NULL,
    CatalogTypeId    INT            NOT NULL,
    CatalogBrandId   INT            NOT NULL,
    AvailableStock   INT            NULL,
    RestockThreshold INT            NULL,
    MaxStockThreshold INT           NULL,
    OnReorder        BIT            NULL,
    CONSTRAINT PK_Catalog PRIMARY KEY (Id),
    CONSTRAINT FK_Catalog_CatalogType FOREIGN KEY (CatalogTypeId)
        REFERENCES CatalogType (Id),
    CONSTRAINT FK_Catalog_CatalogBrand FOREIGN KEY (CatalogBrandId)
        REFERENCES CatalogBrand (Id)
);
