package com.eshop.catalog.service;

import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogType;
import com.eshop.catalog.model.infrastructure.PreconfiguredData;
import com.eshop.catalog.viewmodel.PaginatedItemsViewModel;

import java.util.ArrayList;
import java.util.List;

public class CatalogServiceMock implements ICatalogService {

    private final List<CatalogBrand> brands = new ArrayList<>(PreconfiguredData.getCatalogBrands());
    private final List<CatalogType> types = new ArrayList<>(PreconfiguredData.getCatalogTypes());
    private final List<CatalogItem> items = new ArrayList<>(PreconfiguredData.getCatalogItems());

    @Override
    public CatalogItem findCatalogItem(int id) {
        return items.stream()
                .filter(i -> i.getId() == id)
                .findFirst()
                .orElse(null);
    }

    @Override
    public List<CatalogBrand> getCatalogBrands() {
        return brands;
    }

    @Override
    public PaginatedItemsViewModel<CatalogItem> getCatalogItemsPaginated(int pageSize, int pageIndex) {
        int start = pageIndex * pageSize;
        int end = Math.min(start + pageSize, items.size());
        List<CatalogItem> pageData = (start < items.size()) ? items.subList(start, end) : List.of();
        return new PaginatedItemsViewModel<>(pageIndex, pageSize, items.size(), pageData);
    }

    @Override
    public List<CatalogType> getCatalogTypes() {
        return types;
    }

    @Override
    public CatalogItem createCatalogItem(CatalogItem item) {
        int nextId = items.stream().mapToInt(CatalogItem::getId).max().orElse(0) + 1;
        item.setId(nextId);
        items.add(item);
        return item;
    }

    @Override
    public CatalogItem updateCatalogItem(CatalogItem item) {
        items.removeIf(i -> i.getId() == item.getId());
        items.add(item);
        return item;
    }

    @Override
    public void removeCatalogItem(CatalogItem item) {
        items.removeIf(i -> i.getId() == item.getId());
    }
}
