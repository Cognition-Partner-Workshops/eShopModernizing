package com.eshop.catalog.config;

import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogType;
import java.math.BigDecimal;
import java.util.List;

public final class PreconfiguredData {

    private PreconfiguredData() {}

    public static List<CatalogItem> getPreconfiguredCatalogItems() {
        return List.of(
                createItem(1, 2, 2, 100, ".NET Bot Black Hoodie", new BigDecimal("19.50"), "1.png"),
                createItem(2, 1, 2, 100, ".NET Black & White Mug", new BigDecimal("8.50"), "2.png"),
                createItem(3, 2, 5, 100, "Prism White T-Shirt", new BigDecimal("12.00"), "3.png"),
                createItem(4, 2, 2, 100, ".NET Foundation T-shirt", new BigDecimal("12.00"), "4.png"),
                createItem(5, 3, 5, 100, "Roslyn Red Sheet", new BigDecimal("8.50"), "5.png"),
                createItem(6, 2, 2, 100, ".NET Blue Hoodie", new BigDecimal("12.00"), "6.png"),
                createItem(7, 2, 5, 100, "Roslyn Red T-Shirt", new BigDecimal("12.00"), "7.png"),
                createItem(8, 2, 5, 100, "Kudu Purple Hoodie", new BigDecimal("8.50"), "8.png"),
                createItem(9, 1, 5, 100, "Cup<T> White Mug", new BigDecimal("12.00"), "9.png"),
                createItem(10, 3, 2, 100, ".NET Foundation Sheet", new BigDecimal("12.00"), "10.png"),
                createItem(11, 3, 2, 100, "Cup<T> Sheet", new BigDecimal("8.50"), "11.png"),
                createItem(12, 2, 5, 100, "Prism White TShirt", new BigDecimal("12.00"), "12.png"));
    }

    public static List<CatalogBrand> getPreconfiguredCatalogBrands() {
        return List.of(
                createBrand(1, "Azure"),
                createBrand(2, ".NET"),
                createBrand(3, "Visual Studio"),
                createBrand(4, "SQL Server"),
                createBrand(5, "Other"));
    }

    public static List<CatalogType> getPreconfiguredCatalogTypes() {
        return List.of(
                createType(1, "Mug"),
                createType(2, "T-Shirt"),
                createType(3, "Sheet"),
                createType(4, "USB Memory Stick"));
    }

    private static CatalogItem createItem(
            int id, int typeId, int brandId, int stock, String name, BigDecimal price, String picture) {
        CatalogItem item = new CatalogItem();
        item.setId(id);
        item.setCatalogTypeId(typeId);
        item.setCatalogBrandId(brandId);
        item.setAvailableStock(stock);
        item.setDescription(name);
        item.setName(name);
        item.setPrice(price);
        item.setPictureFileName(picture);
        return item;
    }

    private static CatalogBrand createBrand(int id, String brand) {
        CatalogBrand cb = new CatalogBrand();
        cb.setId(id);
        cb.setBrand(brand);
        return cb;
    }

    private static CatalogType createType(int id, String type) {
        CatalogType ct = new CatalogType();
        ct.setId(id);
        ct.setType(type);
        return ct;
    }
}
