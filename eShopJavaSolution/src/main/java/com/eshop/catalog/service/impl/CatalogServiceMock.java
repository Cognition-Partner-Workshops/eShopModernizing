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

  public CatalogServiceMock() {
    this.catalogItems =
        new CopyOnWriteArrayList<>(PreconfiguredData.getPreconfiguredCatalogItems());
    this.idCounter =
        new AtomicInteger(catalogItems.stream().mapToInt(CatalogItem::getId).max().orElse(0));
  }

  @Override
  public Page<CatalogItem> getCatalogItemsPaginated(int pageSize, int pageIndex) {
    List<CatalogItem> composed = composeCatalogItems(catalogItems);

    List<CatalogItem> itemsOnPage =
        composed.stream()
            .sorted(Comparator.comparingInt(CatalogItem::getId))
            .skip((long) pageSize * pageIndex)
            .limit(pageSize)
            .toList();

    PageRequest pageRequest = PageRequest.of(pageIndex, pageSize, Sort.by("id"));
    return new PageImpl<>(itemsOnPage, pageRequest, composed.size());
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
    catalogItems.add(catalogItem);
  }

  @Override
  public synchronized void updateCatalogItem(CatalogItem modifiedItem) {
    catalogItems.removeIf(item -> item.getId().equals(modifiedItem.getId()));
    catalogItems.add(modifiedItem);
  }

  @Override
  public synchronized void removeCatalogItem(CatalogItem catalogItem) {
    catalogItems.removeIf(item -> item.getId().equals(catalogItem.getId()));
  }

  private List<CatalogItem> composeCatalogItems(List<CatalogItem> items) {
    List<CatalogBrand> brands = PreconfiguredData.getPreconfiguredCatalogBrands();
    List<CatalogType> types = PreconfiguredData.getPreconfiguredCatalogTypes();

    for (CatalogItem item : items) {
      brands.stream()
          .filter(b -> b.getId().equals(item.getCatalogBrandId()))
          .findFirst()
          .ifPresent(item::setCatalogBrand);
      types.stream()
          .filter(t -> t.getId().equals(item.getCatalogTypeId()))
          .findFirst()
          .ifPresent(item::setCatalogType);
    }

    return items;
  }
}
