package com.eshop.catalog.service;

import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogItemsStock;
import com.eshop.catalog.model.CatalogType;
import com.eshop.catalog.model.DiscountItem;

import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Service;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

@Service
@Profile("mock")
public class CatalogServiceMock implements CatalogService {

    private final List<CatalogItem> catalogItems;
    private final List<CatalogBrand> catalogBrands;
    private final List<CatalogType> catalogTypes;
    private final List<CatalogItemsStock> catalogItemsStock;
    private final List<DiscountItem> discountItems;

    public CatalogServiceMock() {
        this.catalogItems = new ArrayList<>(getPreconfiguredCatalogItems());
        this.catalogBrands = new ArrayList<>(getPreconfiguredCatalogBrands());
        this.catalogTypes = new ArrayList<>(getPreconfiguredCatalogTypes());
        this.catalogItemsStock = new ArrayList<>(getPreconfiguredCatalogItemsStock());
        this.discountItems = new ArrayList<>(getPreconfiguredDiscountItems());
    }

    @Override
    public Optional<CatalogItem> findCatalogItem(int id) {
        return catalogItems.stream()
                .filter(item -> item.getId() == id)
                .findFirst();
    }

    @Override
    public List<CatalogBrand> getCatalogBrands() {
        return List.copyOf(catalogBrands);
    }

    @Override
    public List<CatalogItem> getCatalogItems(int brandIdFilter, int typeIdFilter) {
        boolean brandFilterIsNull = brandIdFilter == 0;
        boolean typeFilterIsNull = typeIdFilter == 0;

        return catalogItems.stream()
                .filter(item -> brandFilterIsNull || item.getCatalogBrandId() == brandIdFilter)
                .filter(item -> typeFilterIsNull || item.getCatalogTypeId() == typeIdFilter)
                .toList();
    }

    @Override
    public List<CatalogType> getCatalogTypes() {
        return List.copyOf(catalogTypes);
    }

    @Override
    public int getAvailableStock(LocalDate date, int catalogItemId) {
        return catalogItemsStock.stream()
                .filter(s -> s.getCatalogItemId() == catalogItemId && s.getDate().equals(date))
                .findFirst()
                .map(CatalogItemsStock::getAvailableStock)
                .orElse(0);
    }

    @Override
    public void createAvailableStock(CatalogItemsStock stock) {
        Optional<CatalogItemsStock> existing = catalogItemsStock.stream()
                .filter(s -> s.getCatalogItemId() == stock.getCatalogItemId()
                        && s.getDate().equals(stock.getDate()))
                .findFirst();

        if (existing.isPresent()) {
            existing.get().setAvailableStock(stock.getAvailableStock());
        } else {
            int maxId = catalogItemsStock.stream()
                    .mapToInt(CatalogItemsStock::getStockId)
                    .max()
                    .orElse(0);
            stock.setStockId(maxId + 1);
            catalogItemsStock.add(stock);
        }
    }

    @Override
    public CatalogItem createCatalogItem(CatalogItem catalogItem) {
        int maxId = catalogItems.stream()
                .mapToInt(CatalogItem::getId)
                .max()
                .orElse(0);
        catalogItem.setId(maxId + 1);
        catalogItems.add(catalogItem);
        return catalogItem;
    }

    @Override
    public CatalogItem updateCatalogItem(CatalogItem modifiedItem) {
        for (int i = 0; i < catalogItems.size(); i++) {
            if (catalogItems.get(i).getId().equals(modifiedItem.getId())) {
                catalogItems.set(i, modifiedItem);
                return modifiedItem;
            }
        }
        return modifiedItem;
    }

    @Override
    public void removeCatalogItem(int id) {
        catalogItems.removeIf(item -> item.getId() == id);
    }

    @Override
    public Optional<DiscountItem> getDiscount(LocalDate day) {
        return discountItems.stream()
                .filter(d -> !d.getStart().isAfter(day) && !d.getEnd().isBefore(day))
                .findFirst();
    }

    // --- Preconfigured data mirroring PreconfiguredData.cs ---

    private static List<CatalogItem> getPreconfiguredCatalogItems() {
        return List.of(
                catalogItem(1, 2, 2, ".NET Bot Black Hoodie", new BigDecimal("19.50"), "2.png"),
                catalogItem(2, 1, 2, ".NET Black & White Mug", new BigDecimal("8.50"), "11.png"),
                catalogItem(3, 2, 5, "Prism White T-Shirt", new BigDecimal("12.00"), "7.png"),
                catalogItem(4, 2, 2, ".NET Foundation T-shirt", new BigDecimal("12.00"), "5.png"),
                catalogItem(5, 3, 5, "Roslyn Red Sheet", new BigDecimal("8.50"), "9.png"),
                catalogItem(6, 2, 2, ".NET Blue Hoodie", new BigDecimal("12.00"), "1.png"),
                catalogItem(7, 2, 5, "Roslyn Red T-Shirt", new BigDecimal("12.00"), "6.png"),
                catalogItem(8, 2, 5, "Kudu Purple Hoodie", new BigDecimal("8.50"), "3.png"),
                catalogItem(9, 1, 5, "Cup<T> White Mug", new BigDecimal("12.00"), "12.png"),
                catalogItem(10, 3, 2, ".NET Foundation Sheet", new BigDecimal("12.00"), "8.png"),
                catalogItem(11, 3, 2, "Cup<T> Sheet", new BigDecimal("8.50"), "10.png"),
                catalogItem(12, 2, 5, "Cup<T> TShirt", new BigDecimal("12.00"), "4.png")
        );
    }

    private static CatalogItem catalogItem(int id, int typeId, int brandId, String name,
                                           BigDecimal price, String pictureFilename) {
        CatalogItem item = new CatalogItem();
        item.setId(id);
        item.setCatalogTypeId(typeId);
        item.setCatalogBrandId(brandId);
        item.setName(name);
        item.setDescription(name);
        item.setPrice(price);
        item.setPictureFilename(pictureFilename);
        return item;
    }

    private static List<CatalogBrand> getPreconfiguredCatalogBrands() {
        return List.of(
                catalogBrand(1, "Azure"),
                catalogBrand(2, ".NET"),
                catalogBrand(3, "Visual Studio"),
                catalogBrand(4, "SQL Server"),
                catalogBrand(5, "Other")
        );
    }

    private static CatalogBrand catalogBrand(int id, String brand) {
        CatalogBrand b = new CatalogBrand();
        b.setId(id);
        b.setBrand(brand);
        return b;
    }

    private static List<CatalogType> getPreconfiguredCatalogTypes() {
        return List.of(
                catalogType(1, "Mug"),
                catalogType(2, "T-Shirt"),
                catalogType(3, "Sheet"),
                catalogType(4, "USB Memory Stick")
        );
    }

    private static CatalogType catalogType(int id, String type) {
        CatalogType t = new CatalogType();
        t.setId(id);
        t.setType(type);
        return t;
    }

    private static List<CatalogItemsStock> getPreconfiguredCatalogItemsStock() {
        return List.of(
                stock(1, 1, LocalDate.of(2017, 9, 20), 100),
                stock(2, 1, LocalDate.of(2017, 9, 21), 120),
                stock(3, 1, LocalDate.of(2017, 9, 22), 80),
                stock(4, 2, LocalDate.of(2017, 9, 20), 45),
                stock(5, 4, LocalDate.of(2017, 9, 25), 65),
                stock(6, 5, LocalDate.of(2017, 9, 28), 22)
        );
    }

    private static CatalogItemsStock stock(int stockId, int catalogItemId, LocalDate date, int availableStock) {
        CatalogItemsStock s = new CatalogItemsStock();
        s.setStockId(stockId);
        s.setCatalogItemId(catalogItemId);
        s.setDate(date);
        s.setAvailableStock(availableStock);
        return s;
    }

    private static List<DiscountItem> getPreconfiguredDiscountItems() {
        return List.of(
                discount(LocalDate.of(2017, 9, 18), LocalDate.of(2017, 9, 21), 0.3f),
                discount(LocalDate.of(2017, 9, 22), LocalDate.of(2017, 9, 26), 0.25f),
                discount(LocalDate.of(2017, 9, 27), LocalDate.of(2017, 9, 30), 0.1f),
                discount(LocalDate.of(2017, 10, 5), LocalDate.of(2017, 10, 20), 0.5f),
                discount(LocalDate.of(2017, 11, 13), LocalDate.of(2017, 11, 25), 0.3f),
                discount(LocalDate.of(2017, 12, 20), LocalDate.of(2017, 12, 25), 0.25f)
        );
    }

    private static DiscountItem discount(LocalDate start, LocalDate end, float size) {
        DiscountItem d = new DiscountItem();
        d.setStart(start);
        d.setEnd(end);
        d.setSize(size);
        return d;
    }
}
