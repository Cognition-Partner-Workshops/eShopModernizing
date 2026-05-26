package com.eshop.catalog.service.impl;

import com.eshop.catalog.config.PreconfiguredData;
import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogType;
import com.eshop.catalog.service.CatalogService;
import java.util.Comparator;
import java.util.List;
import java.util.concurrent.CopyOnWriteArrayList;
import java.util.concurrent.atomic.AtomicInteger;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageImpl;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Sort;
import org.springframework.stereotype.Service;

@Service
@ConditionalOnProperty(name = "app.use-mock-data", havingValue = "true")
public class CatalogServiceMock implements CatalogService {

  private final List<CatalogItem> catalogItems;
  private final AtomicInteger idCounter;

  private final List<CatalogBrand> brands;
  private final List<CatalogType> types;

  public CatalogServiceMock() {
    this.brands = PreconfiguredData.getPreconfiguredCatalogBrands();
    this.types = PreconfiguredData.getPreconfiguredCatalogTypes();
    this.catalogItems =
        new CopyOnWriteArrayList<>(PreconfiguredData.getPreconfiguredCatalogItems());
    composeCatalogItems(catalogItems);
    this.idCounter =
        new AtomicInteger(catalogItems.stream().mapToInt(CatalogItem::getId).max().orElse(0));
  }

  @Override
  public Page<CatalogItem> getCatalogItemsPaginated(int pageSize, int pageIndex) {
    List<CatalogItem> itemsOnPage =
        catalogItems.stream()
            .sorted(Comparator.comparingInt(CatalogItem::getId))
            .skip((long) pageSize * pageIndex)
            .limit(pageSize)
            .toList();

    PageRequest pageRequest = PageRequest.of(pageIndex, pageSize, Sort.by("id"));
    return new PageImpl<>(itemsOnPage, pageRequest, catalogItems.size());
  }

  @Override
  public CatalogItem findCatalogItem(int id) {
    return catalogItems.stream().filter(item -> item.getId() == id).findFirst().orElse(null);
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
  public synchronized void createCatalogItem(CatalogItem catalogItem) {
    catalogItem.setId(idCounter.incrementAndGet());
    composeCatalogItem(catalogItem);
    catalogItems.add(catalogItem);
  }

  @Override
  public synchronized void updateCatalogItem(CatalogItem modifiedItem) {
    catalogItems.removeIf(item -> item.getId().equals(modifiedItem.getId()));
    composeCatalogItem(modifiedItem);
    catalogItems.add(modifiedItem);
  }

  @Override
  public synchronized void removeCatalogItem(CatalogItem catalogItem) {
    catalogItems.removeIf(item -> item.getId().equals(catalogItem.getId()));
  }

  private void composeCatalogItems(List<CatalogItem> items) {
    for (CatalogItem item : items) {
      composeCatalogItem(item);
    }
  }

  private void composeCatalogItem(CatalogItem item) {
    brands.stream()
        .filter(b -> b.getId().equals(item.getCatalogBrandId()))
        .findFirst()
        .ifPresent(item::setCatalogBrand);
    types.stream()
        .filter(t -> t.getId().equals(item.getCatalogTypeId()))
        .findFirst()
        .ifPresent(item::setCatalogType);
  }
}
