-- V2__seed_data.sql
-- Seed catalog types, brands, and items matching PreconfiguredData.cs

-- CatalogTypes (4 rows)
INSERT INTO catalog_type (id, type) VALUES (1, 'Mug');
INSERT INTO catalog_type (id, type) VALUES (2, 'T-Shirt');
INSERT INTO catalog_type (id, type) VALUES (3, 'Sheet');
INSERT INTO catalog_type (id, type) VALUES (4, 'USB Memory Stick');

-- CatalogBrands (5 rows)
INSERT INTO catalog_brand (id, brand) VALUES (1, 'Azure');
INSERT INTO catalog_brand (id, brand) VALUES (2, '.NET');
INSERT INTO catalog_brand (id, brand) VALUES (3, 'Visual Studio');
INSERT INTO catalog_brand (id, brand) VALUES (4, 'SQL Server');
INSERT INTO catalog_brand (id, brand) VALUES (5, 'Other');

-- CatalogItems (12 rows)
INSERT INTO catalog (id, name, description, price, picture_file_name, catalog_type_id, catalog_brand_id, available_stock, restock_threshold, max_stock_threshold, on_reorder)
VALUES (1, '.NET Bot Black Hoodie', '.NET Bot Black Hoodie', 19.50, '1.png', 2, 2, 100, 0, 0, false);

INSERT INTO catalog (id, name, description, price, picture_file_name, catalog_type_id, catalog_brand_id, available_stock, restock_threshold, max_stock_threshold, on_reorder)
VALUES (2, '.NET Black & White Mug', '.NET Black & White Mug', 8.50, '2.png', 1, 2, 100, 0, 0, false);

INSERT INTO catalog (id, name, description, price, picture_file_name, catalog_type_id, catalog_brand_id, available_stock, restock_threshold, max_stock_threshold, on_reorder)
VALUES (3, 'Prism White T-Shirt', 'Prism White T-Shirt', 12.00, '3.png', 2, 5, 100, 0, 0, false);

INSERT INTO catalog (id, name, description, price, picture_file_name, catalog_type_id, catalog_brand_id, available_stock, restock_threshold, max_stock_threshold, on_reorder)
VALUES (4, '.NET Foundation T-shirt', '.NET Foundation T-shirt', 12.00, '4.png', 2, 2, 100, 0, 0, false);

INSERT INTO catalog (id, name, description, price, picture_file_name, catalog_type_id, catalog_brand_id, available_stock, restock_threshold, max_stock_threshold, on_reorder)
VALUES (5, 'Roslyn Red Sheet', 'Roslyn Red Sheet', 8.50, '5.png', 3, 5, 100, 0, 0, false);

INSERT INTO catalog (id, name, description, price, picture_file_name, catalog_type_id, catalog_brand_id, available_stock, restock_threshold, max_stock_threshold, on_reorder)
VALUES (6, '.NET Blue Hoodie', '.NET Blue Hoodie', 12.00, '6.png', 2, 2, 100, 0, 0, false);

INSERT INTO catalog (id, name, description, price, picture_file_name, catalog_type_id, catalog_brand_id, available_stock, restock_threshold, max_stock_threshold, on_reorder)
VALUES (7, 'Roslyn Red T-Shirt', 'Roslyn Red T-Shirt', 12.00, '7.png', 2, 5, 100, 0, 0, false);

INSERT INTO catalog (id, name, description, price, picture_file_name, catalog_type_id, catalog_brand_id, available_stock, restock_threshold, max_stock_threshold, on_reorder)
VALUES (8, 'Kudu Purple Hoodie', 'Kudu Purple Hoodie', 8.50, '8.png', 2, 5, 100, 0, 0, false);

INSERT INTO catalog (id, name, description, price, picture_file_name, catalog_type_id, catalog_brand_id, available_stock, restock_threshold, max_stock_threshold, on_reorder)
VALUES (9, 'Cup<T> White Mug', 'Cup<T> White Mug', 12.00, '9.png', 1, 5, 100, 0, 0, false);

INSERT INTO catalog (id, name, description, price, picture_file_name, catalog_type_id, catalog_brand_id, available_stock, restock_threshold, max_stock_threshold, on_reorder)
VALUES (10, '.NET Foundation Sheet', '.NET Foundation Sheet', 12.00, '10.png', 3, 2, 100, 0, 0, false);

INSERT INTO catalog (id, name, description, price, picture_file_name, catalog_type_id, catalog_brand_id, available_stock, restock_threshold, max_stock_threshold, on_reorder)
VALUES (11, 'Cup<T> Sheet', 'Cup<T> Sheet', 8.50, '11.png', 3, 2, 100, 0, 0, false);

INSERT INTO catalog (id, name, description, price, picture_file_name, catalog_type_id, catalog_brand_id, available_stock, restock_threshold, max_stock_threshold, on_reorder)
VALUES (12, 'Prism White TShirt', 'Prism White TShirt', 12.00, '12.png', 2, 5, 100, 0, 0, false);
