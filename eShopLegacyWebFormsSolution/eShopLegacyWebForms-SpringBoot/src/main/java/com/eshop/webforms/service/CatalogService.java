package com.eshop.webforms.service;

import com.eshop.webforms.dto.PaginatedItemsViewModel;
import com.eshop.webforms.model.CatalogBrand;
import com.eshop.webforms.model.CatalogItem;
import com.eshop.webforms.model.CatalogType;

import java.util.List;

public interface CatalogService {

    PaginatedItemsViewModel<CatalogItem> getCatalogItemsPaginated(int pageSize, int pageIndex);

    CatalogItem findCatalogItem(int id);

    List<CatalogType> getCatalogTypes();

    List<CatalogBrand> getCatalogBrands();

    void createCatalogItem(CatalogItem catalogItem);

    void updateCatalogItem(CatalogItem catalogItem);

    void removeCatalogItem(CatalogItem catalogItem);
}
