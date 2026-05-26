-- Seed CatalogType data
INSERT INTO CatalogType (Id, Type) VALUES (1, 'Mug');
INSERT INTO CatalogType (Id, Type) VALUES (2, 'T-Shirt');
INSERT INTO CatalogType (Id, Type) VALUES (3, 'Sheet');
INSERT INTO CatalogType (Id, Type) VALUES (4, 'USB Memory Stick');

-- Seed CatalogBrand data
INSERT INTO CatalogBrand (Id, Brand) VALUES (1, 'Azure');
INSERT INTO CatalogBrand (Id, Brand) VALUES (2, '.NET');
INSERT INTO CatalogBrand (Id, Brand) VALUES (3, 'Visual Studio');
INSERT INTO CatalogBrand (Id, Brand) VALUES (4, 'SQL Server');
INSERT INTO CatalogBrand (Id, Brand) VALUES (5, 'Other');

-- Seed CatalogItem data
INSERT INTO Catalog (Id, CatalogTypeId, CatalogBrandId, AvailableStock, Description, Name, Price, PictureFileName, RestockThreshold, MaxStockThreshold, OnReorder)
VALUES (1, 2, 2, 100, '.NET Bot Black Hoodie', '.NET Bot Black Hoodie', 19.50, '1.png', 0, 0, FALSE);
INSERT INTO Catalog (Id, CatalogTypeId, CatalogBrandId, AvailableStock, Description, Name, Price, PictureFileName, RestockThreshold, MaxStockThreshold, OnReorder)
VALUES (2, 1, 2, 100, '.NET Black & White Mug', '.NET Black & White Mug', 8.50, '2.png', 0, 0, FALSE);
INSERT INTO Catalog (Id, CatalogTypeId, CatalogBrandId, AvailableStock, Description, Name, Price, PictureFileName, RestockThreshold, MaxStockThreshold, OnReorder)
VALUES (3, 2, 5, 100, 'Prism White T-Shirt', 'Prism White T-Shirt', 12.00, '3.png', 0, 0, FALSE);
INSERT INTO Catalog (Id, CatalogTypeId, CatalogBrandId, AvailableStock, Description, Name, Price, PictureFileName, RestockThreshold, MaxStockThreshold, OnReorder)
VALUES (4, 2, 2, 100, '.NET Foundation T-shirt', '.NET Foundation T-shirt', 12.00, '4.png', 0, 0, FALSE);
INSERT INTO Catalog (Id, CatalogTypeId, CatalogBrandId, AvailableStock, Description, Name, Price, PictureFileName, RestockThreshold, MaxStockThreshold, OnReorder)
VALUES (5, 3, 5, 100, 'Roslyn Red Sheet', 'Roslyn Red Sheet', 8.50, '5.png', 0, 0, FALSE);
INSERT INTO Catalog (Id, CatalogTypeId, CatalogBrandId, AvailableStock, Description, Name, Price, PictureFileName, RestockThreshold, MaxStockThreshold, OnReorder)
VALUES (6, 2, 2, 100, '.NET Blue Hoodie', '.NET Blue Hoodie', 12.00, '6.png', 0, 0, FALSE);
INSERT INTO Catalog (Id, CatalogTypeId, CatalogBrandId, AvailableStock, Description, Name, Price, PictureFileName, RestockThreshold, MaxStockThreshold, OnReorder)
VALUES (7, 2, 5, 100, 'Roslyn Red T-Shirt', 'Roslyn Red T-Shirt', 12.00, '7.png', 0, 0, FALSE);
INSERT INTO Catalog (Id, CatalogTypeId, CatalogBrandId, AvailableStock, Description, Name, Price, PictureFileName, RestockThreshold, MaxStockThreshold, OnReorder)
VALUES (8, 2, 5, 100, 'Kudu Purple Hoodie', 'Kudu Purple Hoodie', 8.50, '8.png', 0, 0, FALSE);
INSERT INTO Catalog (Id, CatalogTypeId, CatalogBrandId, AvailableStock, Description, Name, Price, PictureFileName, RestockThreshold, MaxStockThreshold, OnReorder)
VALUES (9, 1, 5, 100, 'Cup<T> White Mug', 'Cup<T> White Mug', 12.00, '9.png', 0, 0, FALSE);
INSERT INTO Catalog (Id, CatalogTypeId, CatalogBrandId, AvailableStock, Description, Name, Price, PictureFileName, RestockThreshold, MaxStockThreshold, OnReorder)
VALUES (10, 3, 2, 100, '.NET Foundation Sheet', '.NET Foundation Sheet', 12.00, '10.png', 0, 0, FALSE);
INSERT INTO Catalog (Id, CatalogTypeId, CatalogBrandId, AvailableStock, Description, Name, Price, PictureFileName, RestockThreshold, MaxStockThreshold, OnReorder)
VALUES (11, 3, 2, 100, 'Cup<T> Sheet', 'Cup<T> Sheet', 8.50, '11.png', 0, 0, FALSE);
INSERT INTO Catalog (Id, CatalogTypeId, CatalogBrandId, AvailableStock, Description, Name, Price, PictureFileName, RestockThreshold, MaxStockThreshold, OnReorder)
VALUES (12, 2, 5, 100, 'Prism White TShirt', 'Prism White TShirt', 12.00, '12.png', 0, 0, FALSE);

-- Advance sequences past the seeded IDs
ALTER SEQUENCE catalog_hilo RESTART WITH 13;
ALTER SEQUENCE catalog_brand_hilo RESTART WITH 6;
ALTER SEQUENCE catalog_type_hilo RESTART WITH 5;
