package com.eshop.catalog.service;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicInteger;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Service;

import com.eshop.catalog.domain.entity.CatalogBrand;
import com.eshop.catalog.domain.entity.CatalogItem;
import com.eshop.catalog.domain.entity.CatalogType;
import com.eshop.catalog.dto.PaginatedItemsDto;
import com.eshop.catalog.util.PreconfiguredData;

@Service
@Profile("mock")
public class CatalogServiceMock implements CatalogService {

    private static final Logger logger = LoggerFactory.getLogger(CatalogServiceMock.class);

    private final Map<Integer, CatalogItem> catalogItems = new ConcurrentHashMap<>();
    private final List<CatalogType> catalogTypes;
    private final List<CatalogBrand> catalogBrands;
    private final AtomicInteger idSequence;

    public CatalogServiceMock() {
        this.catalogTypes = PreconfiguredData.getCatalogTypes();
        this.catalogBrands = PreconfiguredData.getCatalogBrands();

        for (CatalogItem item : PreconfiguredData.getCatalogItems()) {
            composeCatalogItem(item);
            catalogItems.put(item.getId(), item);
        }

        int maxId = catalogItems.keySet().stream()
                .mapToInt(Integer::intValue)
                .max()
                .orElse(0);
        this.idSequence = new AtomicInteger(maxId);

        logger.info("CatalogServiceMock initialized with {} items", catalogItems.size());
    }

    @Override
    public PaginatedItemsDto<CatalogItem> getCatalogItemsPaginated(int pageSize, int pageIndex) {
        List<CatalogItem> allItems = catalogItems.values().stream()
                .sorted(Comparator.comparingInt(CatalogItem::getId))
                .toList();

        long totalItems = allItems.size();
        List<CatalogItem> pageItems = allItems.stream()
                .skip((long) pageSize * pageIndex)
                .limit(pageSize)
                .toList();

        return new PaginatedItemsDto<>(pageIndex, pageSize, totalItems, pageItems);
    }

    @Override
    public CatalogItem findCatalogItem(int id) {
        return catalogItems.get(id);
    }

    @Override
    public List<CatalogType> getCatalogTypes() {
        return List.copyOf(catalogTypes);
    }

    @Override
    public List<CatalogBrand> getCatalogBrands() {
        return List.copyOf(catalogBrands);
    }

    @Override
    public void createCatalogItem(CatalogItem catalogItem) {
        int newId = idSequence.incrementAndGet();
        catalogItem.setId(newId);
        composeCatalogItem(catalogItem);
        catalogItems.put(newId, catalogItem);
        logger.info("Created catalog item with id {}", newId);
    }

    @Override
    public void updateCatalogItem(CatalogItem catalogItem) {
        if (catalogItems.containsKey(catalogItem.getId())) {
            composeCatalogItem(catalogItem);
            catalogItems.put(catalogItem.getId(), catalogItem);
            logger.info("Updated catalog item with id {}", catalogItem.getId());
        }
    }

    @Override
    public void removeCatalogItem(CatalogItem catalogItem) {
        catalogItems.remove(catalogItem.getId());
        logger.info("Removed catalog item with id {}", catalogItem.getId());
    }

    private void composeCatalogItem(CatalogItem item) {
        catalogBrands.stream()
                .filter(b -> b.getId() == item.getCatalogBrandId())
                .findFirst()
                .ifPresent(item::setCatalogBrand);

        catalogTypes.stream()
                .filter(t -> t.getId() == item.getCatalogTypeId())
                .findFirst()
                .ifPresent(item::setCatalogType);
    }
}
