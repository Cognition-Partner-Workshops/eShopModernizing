-- Sequences for HiLo ID generation
CREATE SEQUENCE catalog_hilo START WITH 1 INCREMENT BY 10;
CREATE SEQUENCE catalog_brand_hilo START WITH 1 INCREMENT BY 10;
CREATE SEQUENCE catalog_type_hilo START WITH 1 INCREMENT BY 10;

-- CatalogType table
CREATE TABLE CatalogType (
    Id   INT          NOT NULL PRIMARY KEY,
    Type VARCHAR(100) NOT NULL
);

-- CatalogBrand table
CREATE TABLE CatalogBrand (
    Id    INT          NOT NULL PRIMARY KEY,
    Brand VARCHAR(100) NOT NULL
);

-- Catalog (CatalogItem) table
CREATE TABLE Catalog (
    Id               INT            NOT NULL PRIMARY KEY,
    Name             VARCHAR(50)    NOT NULL,
    Description      VARCHAR(2147483647),
    Price            DECIMAL(18,2)  NOT NULL,
    PictureFileName  VARCHAR(2147483647) NOT NULL,
    CatalogTypeId    INT            NOT NULL,
    CatalogBrandId   INT            NOT NULL,
    AvailableStock   INT,
    RestockThreshold INT,
    MaxStockThreshold INT,
    OnReorder        BOOLEAN,
    CONSTRAINT FK_Catalog_CatalogType  FOREIGN KEY (CatalogTypeId)  REFERENCES CatalogType(Id),
    CONSTRAINT FK_Catalog_CatalogBrand FOREIGN KEY (CatalogBrandId) REFERENCES CatalogBrand(Id)
);
