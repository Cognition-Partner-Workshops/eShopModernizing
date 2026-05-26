package com.eshop.catalog.service.impl;

import com.eshop.catalog.config.PreconfiguredData;
import com.eshop.catalog.dto.PaginatedItemsDto;
import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogType;
import com.eshop.catalog.service.CatalogService;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.stereotype.Service;

@Service
@ConditionalOnProperty(name = "app.catalog.use-mock-data", havingValue = "true", matchIfMissing = true)
public class CatalogServiceMock implements CatalogService {

    private final List<CatalogItem> catalogItems;

    public CatalogServiceMock() {
        this.catalogItems = new ArrayList<>(PreconfiguredData.getPreconfiguredCatalogItems());
    }

    @Override
    public PaginatedItemsDto<CatalogItem> getCatalogItemsPaginated(int pageSize, int pageIndex) {
        List<CatalogItem> composed = composeCatalogItems(catalogItems);

        List<CatalogItem> itemsOnPage = composed.stream()
                .sorted(Comparator.comparingInt(CatalogItem::getId))
                .skip((long) pageSize * pageIndex)
                .limit(pageSize)
                .toList();

        return new PaginatedItemsDto<>(pageIndex, pageSize, composed.size(), itemsOnPage);
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

    private List<CatalogItem> composeCatalogItems(List<CatalogItem> items) {
        List<CatalogBrand> brands = PreconfiguredData.getPreconfiguredCatalogBrands();
        List<CatalogType> types = PreconfiguredData.getPreconfiguredCatalogTypes();

        for (CatalogItem item : items) {
            brands.stream()
                    .filter(b -> b.getId() == item.getCatalogBrandId())
                    .findFirst()
                    .ifPresent(item::setCatalogBrand);
            types.stream()
                    .filter(t -> t.getId() == item.getCatalogTypeId())
                    .findFirst()
                    .ifPresent(item::setCatalogType);
        }

        return items;
    }
}
