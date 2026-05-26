package com.eshop.catalog.service;

import java.util.List;
import java.util.Optional;

import com.eshop.catalog.domain.entity.CatalogBrand;
import com.eshop.catalog.domain.entity.CatalogItem;
import com.eshop.catalog.domain.entity.CatalogType;
import com.eshop.catalog.dto.PaginatedItemsDto;

public interface CatalogService {

    PaginatedItemsDto<CatalogItem> getPaginatedItems(int pageIndex, int pageSize);

    Optional<CatalogItem> findById(int id);

    CatalogItem createItem(CatalogItem item);

    CatalogItem updateItem(CatalogItem item);

    void deleteItem(int id);

    List<CatalogBrand> getAllBrands();

    List<CatalogType> getAllTypes();
}
