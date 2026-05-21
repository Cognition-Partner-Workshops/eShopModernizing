CREATE TABLE catalog_brands (
    id IDENTITY PRIMARY KEY,
    brand VARCHAR(50)
);

CREATE TABLE catalog_types (
    id IDENTITY PRIMARY KEY,
    type VARCHAR(50)
);

CREATE TABLE catalog_items (
    id IDENTITY PRIMARY KEY,
    description VARCHAR(255),
    name VARCHAR(255),
    price DECIMAL(19, 4),
    picture_filename VARCHAR(255),
    catalog_brand_id INT REFERENCES catalog_brands(id),
    catalog_type_id INT REFERENCES catalog_types(id)
);

CREATE TABLE catalog_items_stock (
    stock_id IDENTITY PRIMARY KEY,
    date DATE,
    catalog_item_id INT,
    available_stock INT
);

CREATE TABLE discount_items (
    id IDENTITY PRIMARY KEY,
    size DOUBLE,
    start_date DATE,
    end_date DATE
);
