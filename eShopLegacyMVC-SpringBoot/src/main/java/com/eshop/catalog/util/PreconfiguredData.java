package com.eshop.catalog.util;

import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.List;

import com.eshop.catalog.domain.entity.CatalogBrand;
import com.eshop.catalog.domain.entity.CatalogItem;
import com.eshop.catalog.domain.entity.CatalogType;

public final class PreconfiguredData {

    private PreconfiguredData() {
    }

    public static List<CatalogType> getCatalogTypes() {
        var types = new ArrayList<CatalogType>();

        types.add(createType(1, "Mug"));
        types.add(createType(2, "T-Shirt"));
        types.add(createType(3, "Sheet"));
        types.add(createType(4, "USB Memory Stick"));

        return types;
    }

    public static List<CatalogBrand> getCatalogBrands() {
        var brands = new ArrayList<CatalogBrand>();

        brands.add(createBrand(1, "Azure"));
        brands.add(createBrand(2, ".NET"));
        brands.add(createBrand(3, "Visual Studio"));
        brands.add(createBrand(4, "SQL Server"));
        brands.add(createBrand(5, "Other"));

        return brands;
    }

    public static List<CatalogItem> getCatalogItems() {
        var items = new ArrayList<CatalogItem>();

        items.add(createItem(1, 2, 2, ".NET Bot Black Hoodie", ".NET Bot Black Hoodie", new BigDecimal("19.50"), "1.png", 100));
        items.add(createItem(2, 1, 2, ".NET Black & White Mug", ".NET Black & White Mug", new BigDecimal("8.50"), "2.png", 100));
        items.add(createItem(3, 2, 5, "Prism White T-Shirt", "Prism White T-Shirt", new BigDecimal("12.00"), "3.png", 100));
        items.add(createItem(4, 2, 2, ".NET Foundation T-shirt", ".NET Foundation T-shirt", new BigDecimal("12.00"), "4.png", 100));
        items.add(createItem(5, 3, 5, "Roslyn Red Sheet", "Roslyn Red Sheet", new BigDecimal("8.50"), "5.png", 100));
        items.add(createItem(6, 2, 2, ".NET Blue Hoodie", ".NET Blue Hoodie", new BigDecimal("12.00"), "6.png", 100));
        items.add(createItem(7, 2, 5, "Roslyn Red T-Shirt", "Roslyn Red T-Shirt", new BigDecimal("12.00"), "7.png", 100));
        items.add(createItem(8, 2, 5, "Kudu Purple Hoodie", "Kudu Purple Hoodie", new BigDecimal("8.50"), "8.png", 100));
        items.add(createItem(9, 1, 5, "Cup<T> White Mug", "Cup<T> White Mug", new BigDecimal("12.00"), "9.png", 100));
        items.add(createItem(10, 3, 2, ".NET Foundation Sheet", ".NET Foundation Sheet", new BigDecimal("12.00"), "10.png", 100));
        items.add(createItem(11, 3, 2, "Cup<T> Sheet", "Cup<T> Sheet", new BigDecimal("8.50"), "11.png", 100));
        items.add(createItem(12, 2, 5, "Prism White TShirt", "Prism White TShirt", new BigDecimal("12.00"), "12.png", 100));

        return items;
    }

    private static CatalogType createType(int id, String type) {
        var catalogType = new CatalogType(type);
        catalogType.setId(id);
        return catalogType;
    }

    private static CatalogBrand createBrand(int id, String brand) {
        var catalogBrand = new CatalogBrand(brand);
        catalogBrand.setId(id);
        return catalogBrand;
    }

    private static CatalogItem createItem(int id, int typeId, int brandId,
                                          String name, String description,
                                          BigDecimal price, String pictureFileName,
                                          int availableStock) {
        var item = new CatalogItem();
        item.setId(id);
        item.setCatalogTypeId(typeId);
        item.setCatalogBrandId(brandId);
        item.setName(name);
        item.setDescription(description);
        item.setPrice(price);
        item.setPictureFileName(pictureFileName);
        item.setAvailableStock(availableStock);
        return item;
    }
}
