CREATE TABLE catalog_brands (
    id INT IDENTITY(1,1) PRIMARY KEY,
    brand NVARCHAR(50)
);

CREATE TABLE catalog_types (
    id INT IDENTITY(1,1) PRIMARY KEY,
    type NVARCHAR(50)
);

CREATE TABLE catalog_items (
    id INT IDENTITY(1,1) PRIMARY KEY,
    description NVARCHAR(255),
    name NVARCHAR(255),
    price DECIMAL(19, 4),
    picture_filename NVARCHAR(255),
    catalog_brand_id INT REFERENCES catalog_brands(id),
    catalog_type_id INT REFERENCES catalog_types(id)
);

CREATE TABLE catalog_items_stock (
    stock_id INT IDENTITY(1,1) PRIMARY KEY,
    date DATE,
    catalog_item_id INT,
    available_stock INT
);

CREATE TABLE discount_items (
    id INT IDENTITY(1,1) PRIMARY KEY,
    size FLOAT,
    start_date DATE,
    end_date DATE
);
