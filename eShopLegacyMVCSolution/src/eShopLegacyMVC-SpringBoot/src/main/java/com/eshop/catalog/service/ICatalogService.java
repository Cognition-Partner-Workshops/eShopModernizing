package com.eshop.catalog.service;

import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogType;
import com.eshop.catalog.viewmodel.PaginatedItemsViewModel;

import java.util.List;

public interface ICatalogService {

    CatalogItem findCatalogItem(int id);

    List<CatalogBrand> getCatalogBrands();

    PaginatedItemsViewModel<CatalogItem> getCatalogItemsPaginated(int pageSize, int pageIndex);

    List<CatalogType> getCatalogTypes();

    CatalogItem createCatalogItem(CatalogItem item);

    CatalogItem updateCatalogItem(CatalogItem item);

    void removeCatalogItem(CatalogItem item);
}
