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
    Description NVARCHAR(MAX),
    Price DECIMAL(18,2) NOT NULL,
    PictureFileName NVARCHAR(MAX) NOT NULL,
    CatalogTypeId INT NOT NULL REFERENCES CatalogType(Id),
    CatalogBrandId INT NOT NULL REFERENCES CatalogBrand(Id),
    AvailableStock INT NOT NULL DEFAULT 0,
    RestockThreshold INT NOT NULL DEFAULT 0,
    MaxStockThreshold INT NOT NULL DEFAULT 0,
    OnReorder BIT NOT NULL DEFAULT 0
);
