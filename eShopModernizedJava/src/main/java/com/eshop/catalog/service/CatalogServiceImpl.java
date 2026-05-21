package com.eshop.catalog.service;

import com.eshop.catalog.dto.PaginatedItemsDto;
import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogType;
import com.eshop.catalog.repository.CatalogBrandRepository;
import com.eshop.catalog.repository.CatalogItemRepository;
import com.eshop.catalog.repository.CatalogTypeRepository;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Sort;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;

@Service
@ConditionalOnProperty(name = "app.use-mock-data", havingValue = "false", matchIfMissing = true)
public class CatalogServiceImpl implements CatalogService {

    private final CatalogItemRepository catalogItemRepository;
    private final CatalogBrandRepository catalogBrandRepository;
    private final CatalogTypeRepository catalogTypeRepository;

    public CatalogServiceImpl(CatalogItemRepository catalogItemRepository,
                              CatalogBrandRepository catalogBrandRepository,
                              CatalogTypeRepository catalogTypeRepository) {
        this.catalogItemRepository = catalogItemRepository;
        this.catalogBrandRepository = catalogBrandRepository;
        this.catalogTypeRepository = catalogTypeRepository;
    }

    @Override
    public PaginatedItemsDto<CatalogItem> getCatalogItemsPaginated(int pageSize, int pageIndex) {
        long totalItems = catalogItemRepository.count();
        Page<CatalogItem> page = catalogItemRepository.findAll(
                PageRequest.of(pageIndex, pageSize, Sort.by("id")));
        return new PaginatedItemsDto<>(pageIndex, pageSize, totalItems, page.getContent());
    }

    @Override
    public CatalogItem findCatalogItem(int id) {
        return catalogItemRepository.findById(id).orElse(null);
    }

    @Override
    public List<CatalogType> getCatalogTypes() {
        return catalogTypeRepository.findAll();
    }

    @Override
    public List<CatalogBrand> getCatalogBrands() {
        return catalogBrandRepository.findAll();
    }

    @Override
    @Transactional
    public void createCatalogItem(CatalogItem catalogItem) {
        catalogItemRepository.save(catalogItem);
    }

    @Override
    @Transactional
    public void updateCatalogItem(CatalogItem catalogItem) {
        catalogItemRepository.save(catalogItem);
    }

    @Override
    @Transactional
    public void removeCatalogItem(CatalogItem catalogItem) {
        catalogItemRepository.delete(catalogItem);
    }
}
