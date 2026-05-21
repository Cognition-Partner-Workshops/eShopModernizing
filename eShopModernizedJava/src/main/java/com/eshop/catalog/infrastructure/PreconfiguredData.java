package com.eshop.catalog.infrastructure;

import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogType;

import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.List;

public final class PreconfiguredData {

    private PreconfiguredData() {
    }

    public static List<CatalogItem> getPreconfiguredCatalogItems() {
        List<CatalogItem> items = new ArrayList<>();
        items.add(createItem(1, 2, 2, 100, ".NET Bot Black Hoodie", ".NET Bot Black Hoodie", new BigDecimal("19.50"), "1.png"));
        items.add(createItem(2, 1, 2, 100, ".NET Black & White Mug", ".NET Black & White Mug", new BigDecimal("8.50"), "2.png"));
        items.add(createItem(3, 2, 5, 100, "Prism White T-Shirt", "Prism White T-Shirt", new BigDecimal("12"), "3.png"));
        items.add(createItem(4, 2, 2, 100, ".NET Foundation T-shirt", ".NET Foundation T-shirt", new BigDecimal("12"), "4.png"));
        items.add(createItem(5, 3, 5, 100, "Roslyn Red Sheet", "Roslyn Red Sheet", new BigDecimal("8.50"), "5.png"));
        items.add(createItem(6, 2, 2, 100, ".NET Blue Hoodie", ".NET Blue Hoodie", new BigDecimal("12"), "6.png"));
        items.add(createItem(7, 2, 5, 100, "Roslyn Red T-Shirt", "Roslyn Red T-Shirt", new BigDecimal("12"), "7.png"));
        items.add(createItem(8, 2, 5, 100, "Kudu Purple Hoodie", "Kudu Purple Hoodie", new BigDecimal("8.50"), "8.png"));
        items.add(createItem(9, 1, 5, 100, "Cup<T> White Mug", "Cup<T> White Mug", new BigDecimal("12"), "9.png"));
        items.add(createItem(10, 3, 2, 100, ".NET Foundation Sheet", ".NET Foundation Sheet", new BigDecimal("12"), "10.png"));
        items.add(createItem(11, 3, 2, 100, "Cup<T> Sheet", "Cup<T> Sheet", new BigDecimal("8.50"), "11.png"));
        items.add(createItem(12, 2, 5, 100, "Prism White TShirt", "Prism White TShirt", new BigDecimal("12"), "12.png"));
        return items;
    }

    public static List<CatalogBrand> getPreconfiguredCatalogBrands() {
        List<CatalogBrand> brands = new ArrayList<>();
        brands.add(createBrand(1, "Azure"));
        brands.add(createBrand(2, ".NET"));
        brands.add(createBrand(3, "Visual Studio"));
        brands.add(createBrand(4, "SQL Server"));
        brands.add(createBrand(5, "Other"));
        return brands;
    }

    public static List<CatalogType> getPreconfiguredCatalogTypes() {
        List<CatalogType> types = new ArrayList<>();
        types.add(createType(1, "Mug"));
        types.add(createType(2, "T-Shirt"));
        types.add(createType(3, "Sheet"));
        types.add(createType(4, "USB Memory Stick"));
        return types;
    }

    private static CatalogItem createItem(int id, int typeId, int brandId, int stock,
                                           String description, String name, BigDecimal price,
                                           String pictureFileName) {
        CatalogItem item = new CatalogItem();
        item.setId(id);
        item.setCatalogTypeId(typeId);
        item.setCatalogBrandId(brandId);
        item.setAvailableStock(stock);
        item.setDescription(description);
        item.setName(name);
        item.setPrice(price);
        item.setPictureFileName(pictureFileName);
        return item;
    }

    private static CatalogBrand createBrand(int id, String brand) {
        CatalogBrand b = new CatalogBrand();
        b.setId(id);
        b.setBrand(brand);
        return b;
    }

    private static CatalogType createType(int id, String type) {
        CatalogType t = new CatalogType();
        t.setId(id);
        t.setType(type);
        return t;
    }
}
