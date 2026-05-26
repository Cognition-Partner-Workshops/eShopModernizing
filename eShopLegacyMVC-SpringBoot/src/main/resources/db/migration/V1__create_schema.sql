-- V1: Create schema for eShop Catalog database
-- Ported from EF6 CatalogDBContext.OnModelCreating and HiLo sequence scripts

-- HiLo sequences for ID generation
CREATE SEQUENCE catalog_hilo START WITH 1 INCREMENT BY 10;
CREATE SEQUENCE catalog_brand_hilo START WITH 1 INCREMENT BY 10;
CREATE SEQUENCE catalog_type_hilo START WITH 1 INCREMENT BY 10;

-- CatalogType table
CREATE TABLE CatalogType (
    Id INT NOT NULL PRIMARY KEY,
    Type VARCHAR(100) NOT NULL
);

-- CatalogBrand table
CREATE TABLE CatalogBrand (
    Id INT NOT NULL PRIMARY KEY,
    Brand VARCHAR(100) NOT NULL
);

-- Catalog table (maps to CatalogItem entity)
CREATE TABLE Catalog (
    Id INT NOT NULL PRIMARY KEY,
    Name VARCHAR(50) NOT NULL,
    Description VARCHAR(2048),
    Price DECIMAL(18, 2) NOT NULL,
    PictureFileName VARCHAR(255) NOT NULL,
    CatalogTypeId INT NOT NULL,
    CatalogBrandId INT NOT NULL,
    AvailableStock INT NOT NULL DEFAULT 0,
    RestockThreshold INT NOT NULL DEFAULT 0,
    MaxStockThreshold INT NOT NULL DEFAULT 0,
    OnReorder BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Catalog_CatalogBrand FOREIGN KEY (CatalogBrandId) REFERENCES CatalogBrand(Id),
    CONSTRAINT FK_Catalog_CatalogType FOREIGN KEY (CatalogTypeId) REFERENCES CatalogType(Id)
);
