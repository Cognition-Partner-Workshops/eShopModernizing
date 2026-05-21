SET IDENTITY_INSERT catalog_brands ON;
INSERT INTO catalog_brands (id, brand) VALUES (1, 'Azure');
INSERT INTO catalog_brands (id, brand) VALUES (2, '.NET');
INSERT INTO catalog_brands (id, brand) VALUES (3, 'Visual Studio');
INSERT INTO catalog_brands (id, brand) VALUES (4, 'SQL Server');
INSERT INTO catalog_brands (id, brand) VALUES (5, 'Other');
SET IDENTITY_INSERT catalog_brands OFF;

SET IDENTITY_INSERT catalog_types ON;
INSERT INTO catalog_types (id, type) VALUES (1, 'Mug');
INSERT INTO catalog_types (id, type) VALUES (2, 'T-Shirt');
INSERT INTO catalog_types (id, type) VALUES (3, 'Sheet');
INSERT INTO catalog_types (id, type) VALUES (4, 'USB Memory Stick');
SET IDENTITY_INSERT catalog_types OFF;

SET IDENTITY_INSERT catalog_items ON;
INSERT INTO catalog_items (id, catalog_type_id, catalog_brand_id, description, name, price, picture_filename) VALUES (1, 2, 2, '.NET Bot Black Hoodie', '.NET Bot Black Hoodie', 19.5000, '2.png');
INSERT INTO catalog_items (id, catalog_type_id, catalog_brand_id, description, name, price, picture_filename) VALUES (2, 1, 2, '.NET Black & White Mug', '.NET Black & White Mug', 8.5000, '11.png');
INSERT INTO catalog_items (id, catalog_type_id, catalog_brand_id, description, name, price, picture_filename) VALUES (3, 2, 5, 'Prism White T-Shirt', 'Prism White T-Shirt', 12.0000, '7.png');
INSERT INTO catalog_items (id, catalog_type_id, catalog_brand_id, description, name, price, picture_filename) VALUES (4, 2, 2, '.NET Foundation T-shirt', '.NET Foundation T-shirt', 12.0000, '5.png');
INSERT INTO catalog_items (id, catalog_type_id, catalog_brand_id, description, name, price, picture_filename) VALUES (5, 3, 5, 'Roslyn Red Sheet', 'Roslyn Red Sheet', 8.5000, '9.png');
INSERT INTO catalog_items (id, catalog_type_id, catalog_brand_id, description, name, price, picture_filename) VALUES (6, 2, 2, '.NET Blue Hoodie', '.NET Blue Hoodie', 12.0000, '1.png');
INSERT INTO catalog_items (id, catalog_type_id, catalog_brand_id, description, name, price, picture_filename) VALUES (7, 2, 5, 'Roslyn Red T-Shirt', 'Roslyn Red T-Shirt', 12.0000, '6.png');
INSERT INTO catalog_items (id, catalog_type_id, catalog_brand_id, description, name, price, picture_filename) VALUES (8, 2, 5, 'Kudu Purple Hoodie', 'Kudu Purple Hoodie', 8.5000, '3.png');
INSERT INTO catalog_items (id, catalog_type_id, catalog_brand_id, description, name, price, picture_filename) VALUES (9, 1, 5, 'Cup<T> White Mug', 'Cup<T> White Mug', 12.0000, '12.png');
INSERT INTO catalog_items (id, catalog_type_id, catalog_brand_id, description, name, price, picture_filename) VALUES (10, 3, 2, '.NET Foundation Sheet', '.NET Foundation Sheet', 12.0000, '8.png');
INSERT INTO catalog_items (id, catalog_type_id, catalog_brand_id, description, name, price, picture_filename) VALUES (11, 3, 2, 'Cup<T> Sheet', 'Cup<T> Sheet', 8.5000, '10.png');
INSERT INTO catalog_items (id, catalog_type_id, catalog_brand_id, description, name, price, picture_filename) VALUES (12, 2, 5, 'Cup<T> TShirt', 'Cup<T> TShirt', 12.0000, '4.png');
SET IDENTITY_INSERT catalog_items OFF;

SET IDENTITY_INSERT catalog_items_stock ON;
INSERT INTO catalog_items_stock (stock_id, catalog_item_id, date, available_stock) VALUES (1, 1, '2017-09-20', 100);
INSERT INTO catalog_items_stock (stock_id, catalog_item_id, date, available_stock) VALUES (2, 1, '2017-09-21', 120);
INSERT INTO catalog_items_stock (stock_id, catalog_item_id, date, available_stock) VALUES (3, 1, '2017-09-22', 80);
INSERT INTO catalog_items_stock (stock_id, catalog_item_id, date, available_stock) VALUES (4, 2, '2017-09-20', 45);
INSERT INTO catalog_items_stock (stock_id, catalog_item_id, date, available_stock) VALUES (5, 4, '2017-09-25', 65);
INSERT INTO catalog_items_stock (stock_id, catalog_item_id, date, available_stock) VALUES (6, 5, '2017-09-28', 22);
SET IDENTITY_INSERT catalog_items_stock OFF;

INSERT INTO discount_items (size, start_date, end_date) VALUES (0.3, '2017-09-18', '2017-09-21');
INSERT INTO discount_items (size, start_date, end_date) VALUES (0.25, '2017-09-22', '2017-09-26');
INSERT INTO discount_items (size, start_date, end_date) VALUES (0.1, '2017-09-27', '2017-09-30');
INSERT INTO discount_items (size, start_date, end_date) VALUES (0.5, '2017-10-05', '2017-10-20');
INSERT INTO discount_items (size, start_date, end_date) VALUES (0.3, '2017-11-13', '2017-11-25');
INSERT INTO discount_items (size, start_date, end_date) VALUES (0.25, '2017-12-20', '2017-12-25');
