package com.eshop.catalog.model.infrastructure;

import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogType;

import java.math.BigDecimal;
import java.util.List;

public final class PreconfiguredData {

    private PreconfiguredData() {
    }

    public static List<CatalogBrand> getCatalogBrands() {
        return List.of(
                createBrand(1, "Azure"),
                createBrand(2, ".NET"),
                createBrand(3, "Visual Studio"),
                createBrand(4, "SQL Server"),
                createBrand(5, "Other")
        );
    }

    public static List<CatalogType> getCatalogTypes() {
        return List.of(
                createType(1, "Mug"),
                createType(2, "T-Shirt"),
                createType(3, "Sheet"),
                createType(4, "USB Memory Stick")
        );
    }

    public static List<CatalogItem> getCatalogItems() {
        return List.of(
                createItem(1, 2, 2, ".NET Bot Black Hoodie", ".NET Bot Black Hoodie", new BigDecimal("19.50"), "1.png", 100),
                createItem(2, 1, 2, ".NET Black & White Mug", ".NET Black & White Mug", new BigDecimal("8.50"), "2.png", 100),
                createItem(3, 2, 5, "Prism White T-Shirt", "Prism White T-Shirt", new BigDecimal("12.00"), "3.png", 100),
                createItem(4, 2, 2, ".NET Foundation T-shirt", ".NET Foundation T-shirt", new BigDecimal("12.00"), "4.png", 100),
                createItem(5, 3, 5, "Roslyn Red Sheet", "Roslyn Red Sheet", new BigDecimal("8.50"), "5.png", 100),
                createItem(6, 2, 2, ".NET Blue Hoodie", ".NET Blue Hoodie", new BigDecimal("12.00"), "6.png", 100),
                createItem(7, 2, 5, "Roslyn Red T-Shirt", "Roslyn Red T-Shirt", new BigDecimal("12.00"), "7.png", 100),
                createItem(8, 2, 5, "Kudu Purple Hoodie", "Kudu Purple Hoodie", new BigDecimal("8.50"), "8.png", 100),
                createItem(9, 1, 5, "Cup<T> White Mug", "Cup<T> White Mug", new BigDecimal("12.00"), "9.png", 100),
                createItem(10, 3, 2, ".NET Foundation Sheet", ".NET Foundation Sheet", new BigDecimal("12.00"), "10.png", 100),
                createItem(11, 3, 2, "Cup<T> Sheet", "Cup<T> Sheet", new BigDecimal("8.50"), "11.png", 100),
                createItem(12, 2, 5, "Prism White TShirt", "Prism White TShirt", new BigDecimal("12.00"), "12.png", 100)
        );
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

    private static CatalogItem createItem(int id, int typeId, int brandId, String name,
                                          String description, BigDecimal price,
                                          String pictureFileName, int stock) {
        CatalogItem item = new CatalogItem();
        item.setId(id);
        item.setCatalogTypeId(typeId);
        item.setCatalogBrandId(brandId);
        item.setName(name);
        item.setDescription(description);
        item.setPrice(price);
        item.setPictureFileName(pictureFileName);
        item.setAvailableStock(stock);
        return item;
    }
}
