package com.eshop.catalog.service;

import java.util.List;

import com.eshop.catalog.domain.entity.CatalogBrand;
import com.eshop.catalog.domain.entity.CatalogItem;
import com.eshop.catalog.domain.entity.CatalogType;
import com.eshop.catalog.dto.PaginatedItemsDto;

public interface CatalogService {

    CatalogItem findCatalogItem(int id);

    List<CatalogBrand> getCatalogBrands();

    PaginatedItemsDto<CatalogItem> getCatalogItemsPaginated(int pageSize, int pageIndex);

    List<CatalogType> getCatalogTypes();

    void createCatalogItem(CatalogItem catalogItem);

    void updateCatalogItem(CatalogItem catalogItem);

    void removeCatalogItem(CatalogItem catalogItem);
}
