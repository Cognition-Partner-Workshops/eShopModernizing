-- V2__seed_data.sql
-- Seed data matching PreconfiguredData.cs from eShopLegacyMVC

-- CatalogType seed data
SET IDENTITY_INSERT [CatalogType] ON;

INSERT INTO [CatalogType] ([Id], [Type]) VALUES (1, 'Mug');
INSERT INTO [CatalogType] ([Id], [Type]) VALUES (2, 'T-Shirt');
INSERT INTO [CatalogType] ([Id], [Type]) VALUES (3, 'Sheet');
INSERT INTO [CatalogType] ([Id], [Type]) VALUES (4, 'USB Memory Stick');

SET IDENTITY_INSERT [CatalogType] OFF;

-- CatalogBrand seed data
SET IDENTITY_INSERT [CatalogBrand] ON;

INSERT INTO [CatalogBrand] ([Id], [Brand]) VALUES (1, 'Azure');
INSERT INTO [CatalogBrand] ([Id], [Brand]) VALUES (2, '.NET');
INSERT INTO [CatalogBrand] ([Id], [Brand]) VALUES (3, 'Visual Studio');
INSERT INTO [CatalogBrand] ([Id], [Brand]) VALUES (4, 'SQL Server');
INSERT INTO [CatalogBrand] ([Id], [Brand]) VALUES (5, 'Other');

SET IDENTITY_INSERT [CatalogBrand] OFF;

-- CatalogItem seed data (Catalog table does not use IDENTITY)
INSERT INTO [Catalog] ([Id], [CatalogTypeId], [CatalogBrandId], [AvailableStock], [Description], [Name], [Price], [PictureFileName])
VALUES (1, 2, 2, 100, '.NET Bot Black Hoodie', '.NET Bot Black Hoodie', 19.50, '1.png');

INSERT INTO [Catalog] ([Id], [CatalogTypeId], [CatalogBrandId], [AvailableStock], [Description], [Name], [Price], [PictureFileName])
VALUES (2, 1, 2, 100, '.NET Black & White Mug', '.NET Black & White Mug', 8.50, '2.png');

INSERT INTO [Catalog] ([Id], [CatalogTypeId], [CatalogBrandId], [AvailableStock], [Description], [Name], [Price], [PictureFileName])
VALUES (3, 2, 5, 100, 'Prism White T-Shirt', 'Prism White T-Shirt', 12.00, '3.png');

INSERT INTO [Catalog] ([Id], [CatalogTypeId], [CatalogBrandId], [AvailableStock], [Description], [Name], [Price], [PictureFileName])
VALUES (4, 2, 2, 100, '.NET Foundation T-shirt', '.NET Foundation T-shirt', 12.00, '4.png');

INSERT INTO [Catalog] ([Id], [CatalogTypeId], [CatalogBrandId], [AvailableStock], [Description], [Name], [Price], [PictureFileName])
VALUES (5, 3, 5, 100, 'Roslyn Red Sheet', 'Roslyn Red Sheet', 8.50, '5.png');

INSERT INTO [Catalog] ([Id], [CatalogTypeId], [CatalogBrandId], [AvailableStock], [Description], [Name], [Price], [PictureFileName])
VALUES (6, 2, 2, 100, '.NET Blue Hoodie', '.NET Blue Hoodie', 12.00, '6.png');

INSERT INTO [Catalog] ([Id], [CatalogTypeId], [CatalogBrandId], [AvailableStock], [Description], [Name], [Price], [PictureFileName])
VALUES (7, 2, 5, 100, 'Roslyn Red T-Shirt', 'Roslyn Red T-Shirt', 12.00, '7.png');

INSERT INTO [Catalog] ([Id], [CatalogTypeId], [CatalogBrandId], [AvailableStock], [Description], [Name], [Price], [PictureFileName])
VALUES (8, 2, 5, 100, 'Kudu Purple Hoodie', 'Kudu Purple Hoodie', 8.50, '8.png');

INSERT INTO [Catalog] ([Id], [CatalogTypeId], [CatalogBrandId], [AvailableStock], [Description], [Name], [Price], [PictureFileName])
VALUES (9, 1, 5, 100, 'Cup<T> White Mug', 'Cup<T> White Mug', 12.00, '9.png');

INSERT INTO [Catalog] ([Id], [CatalogTypeId], [CatalogBrandId], [AvailableStock], [Description], [Name], [Price], [PictureFileName])
VALUES (10, 3, 2, 100, '.NET Foundation Sheet', '.NET Foundation Sheet', 12.00, '10.png');

INSERT INTO [Catalog] ([Id], [CatalogTypeId], [CatalogBrandId], [AvailableStock], [Description], [Name], [Price], [PictureFileName])
VALUES (11, 3, 2, 100, 'Cup<T> Sheet', 'Cup<T> Sheet', 8.50, '11.png');

INSERT INTO [Catalog] ([Id], [CatalogTypeId], [CatalogBrandId], [AvailableStock], [Description], [Name], [Price], [PictureFileName])
VALUES (12, 2, 5, 100, 'Prism White TShirt', 'Prism White TShirt', 12.00, '12.png');
