CREATE TABLE CatalogBrand (
    Id INT NOT NULL PRIMARY KEY,
    Brand NVARCHAR(100) NOT NULL
);

CREATE TABLE CatalogType (
    Id INT NOT NULL PRIMARY KEY,
    Type NVARCHAR(100) NOT NULL
);

CREATE TABLE Catalog (
    Id INT NOT NULL PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Price DECIMAL(18, 2) NOT NULL,
    PictureFileName NVARCHAR(255) NOT NULL,
    CatalogTypeId INT NOT NULL,
    CatalogBrandId INT NOT NULL,
    AvailableStock INT NOT NULL DEFAULT 0,
    RestockThreshold INT NOT NULL DEFAULT 0,
    MaxStockThreshold INT NOT NULL DEFAULT 0,
    OnReorder BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Catalog_CatalogBrand FOREIGN KEY (CatalogBrandId) REFERENCES CatalogBrand(Id),
    CONSTRAINT FK_Catalog_CatalogType FOREIGN KEY (CatalogTypeId) REFERENCES CatalogType(Id)
);
