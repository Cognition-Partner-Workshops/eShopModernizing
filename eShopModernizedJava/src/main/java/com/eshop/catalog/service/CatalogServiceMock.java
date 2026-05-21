package com.eshop.catalog.service;

import com.eshop.catalog.dto.PaginatedItemsDto;
import com.eshop.catalog.infrastructure.PreconfiguredData;
import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogType;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.function.Function;
import java.util.stream.Collectors;

@Service
@ConditionalOnProperty(name = "app.use-mock-data", havingValue = "true")
public class CatalogServiceMock implements CatalogService {

    private final List<CatalogItem> catalogItems;

    public CatalogServiceMock() {
        this.catalogItems = new ArrayList<>(PreconfiguredData.getPreconfiguredCatalogItems());
    }

    @Override
    public PaginatedItemsDto<CatalogItem> getCatalogItemsPaginated(int pageSize, int pageIndex) {
        composeCatalogItems(catalogItems);
        List<CatalogItem> itemsOnPage = catalogItems.stream()
                .sorted((a, b) -> Integer.compare(a.getId(), b.getId()))
                .skip((long) pageSize * pageIndex)
                .limit(pageSize)
                .collect(Collectors.toList());
        return new PaginatedItemsDto<>(pageIndex, pageSize, catalogItems.size(), itemsOnPage);
    }

    @Override
    public CatalogItem findCatalogItem(int id) {
        return catalogItems.stream()
                .filter(item -> item.getId() == id)
                .findFirst()
                .orElse(null);
    }

    @Override
    public List<CatalogType> getCatalogTypes() {
        return PreconfiguredData.getPreconfiguredCatalogTypes();
    }

    @Override
    public List<CatalogBrand> getCatalogBrands() {
        return PreconfiguredData.getPreconfiguredCatalogBrands();
    }

    @Override
    public void createCatalogItem(CatalogItem catalogItem) {
        int maxId = catalogItems.stream()
                .mapToInt(CatalogItem::getId)
                .max()
                .orElse(0);
        catalogItem.setId(maxId + 1);
        catalogItems.add(catalogItem);
    }

    @Override
    public void updateCatalogItem(CatalogItem modifiedItem) {
        for (int i = 0; i < catalogItems.size(); i++) {
            if (catalogItems.get(i).getId() == modifiedItem.getId()) {
                catalogItems.set(i, modifiedItem);
                return;
            }
        }
    }

    @Override
    public void removeCatalogItem(CatalogItem catalogItem) {
        catalogItems.removeIf(item -> item.getId() == catalogItem.getId());
    }

    private void composeCatalogItems(List<CatalogItem> items) {
        Map<Integer, CatalogBrand> brandsById = PreconfiguredData.getPreconfiguredCatalogBrands()
                .stream().collect(Collectors.toMap(CatalogBrand::getId, Function.identity()));
        Map<Integer, CatalogType> typesById = PreconfiguredData.getPreconfiguredCatalogTypes()
                .stream().collect(Collectors.toMap(CatalogType::getId, Function.identity()));
        for (CatalogItem item : items) {
            item.setCatalogBrand(brandsById.get(item.getCatalogBrandId()));
            item.setCatalogType(typesById.get(item.getCatalogTypeId()));
        }
    }
}
