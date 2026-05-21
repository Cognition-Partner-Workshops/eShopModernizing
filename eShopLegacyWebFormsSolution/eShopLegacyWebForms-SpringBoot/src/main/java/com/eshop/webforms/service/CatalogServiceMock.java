package com.eshop.webforms.service;

import com.eshop.webforms.config.PreconfiguredData;
import com.eshop.webforms.dto.PaginatedItemsViewModel;
import com.eshop.webforms.model.CatalogBrand;
import com.eshop.webforms.model.CatalogItem;
import com.eshop.webforms.model.CatalogType;
import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Service;

import org.springframework.http.HttpStatus;
import org.springframework.web.server.ResponseStatusException;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;

@Service
@Profile("mock")
public class CatalogServiceMock implements CatalogService {

    private final List<CatalogItem> catalogItems;

    public CatalogServiceMock() {
        this.catalogItems = new ArrayList<>(PreconfiguredData.getCatalogItems());
    }

    @Override
    public PaginatedItemsViewModel<CatalogItem> getCatalogItemsPaginated(int pageSize, int pageIndex) {
        List<CatalogItem> composed = composeCatalogItems(catalogItems);

        List<CatalogItem> itemsOnPage = composed.stream()
                .sorted(Comparator.comparingInt(CatalogItem::getId))
                .skip((long) pageSize * pageIndex)
                .limit(pageSize)
                .toList();

        return new PaginatedItemsViewModel<>(pageIndex, pageSize, composed.size(), itemsOnPage);
    }

    @Override
    public CatalogItem findCatalogItem(int id) {
        return catalogItems.stream()
                .filter(item -> item.getId() == id)
                .findFirst()
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND,
                        "Catalog item with id " + id + " not found"));
    }

    @Override
    public List<CatalogType> getCatalogTypes() {
        return PreconfiguredData.getCatalogTypes();
    }

    @Override
    public List<CatalogBrand> getCatalogBrands() {
        return PreconfiguredData.getCatalogBrands();
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
        List<CatalogType> catalogTypes = PreconfiguredData.getCatalogTypes();
        List<CatalogBrand> catalogBrands = PreconfiguredData.getCatalogBrands();

        for (CatalogItem item : items) {
            catalogBrands.stream()
                    .filter(b -> b.getId() == item.getCatalogBrandId())
                    .findFirst()
                    .ifPresent(item::setCatalogBrand);
            catalogTypes.stream()
                    .filter(t -> t.getId() == item.getCatalogTypeId())
                    .findFirst()
                    .ifPresent(item::setCatalogType);
        }

        return items;
    }
}
