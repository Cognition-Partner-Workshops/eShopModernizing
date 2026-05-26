package com.eshop.catalog.service;

import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogType;
import java.util.List;
import org.springframework.data.domain.Page;

public interface CatalogService {

  CatalogItem findCatalogItem(int id);

  List<CatalogBrand> getCatalogBrands();

  Page<CatalogItem> getCatalogItemsPaginated(int pageSize, int pageIndex);

  List<CatalogType> getCatalogTypes();

  void createCatalogItem(CatalogItem catalogItem);

  void updateCatalogItem(CatalogItem catalogItem);

  void removeCatalogItem(CatalogItem catalogItem);
}
