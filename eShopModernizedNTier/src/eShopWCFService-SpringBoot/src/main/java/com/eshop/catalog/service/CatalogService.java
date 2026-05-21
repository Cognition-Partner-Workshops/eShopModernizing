package com.eshop.catalog.service;

import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogItemsStock;
import com.eshop.catalog.model.CatalogType;
import com.eshop.catalog.model.DiscountItem;

import java.time.LocalDate;
import java.util.List;
import java.util.Optional;

public interface CatalogService {

    Optional<CatalogItem> findCatalogItem(int id);

    List<CatalogBrand> getCatalogBrands();

    List<CatalogItem> getCatalogItems(int brandIdFilter, int typeIdFilter);

    List<CatalogType> getCatalogTypes();

    int getAvailableStock(LocalDate date, int catalogItemId);

    void createAvailableStock(CatalogItemsStock catalogItemsStock);

    CatalogItem createCatalogItem(CatalogItem catalogItem);

    CatalogItem updateCatalogItem(CatalogItem catalogItem);

    void removeCatalogItem(int id);

    Optional<DiscountItem> getDiscount(LocalDate day);
}
