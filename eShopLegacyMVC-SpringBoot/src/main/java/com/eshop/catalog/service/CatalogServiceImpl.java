package com.eshop.catalog.service;

import java.util.List;

import org.springframework.context.annotation.Profile;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Sort;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import com.eshop.catalog.config.CatalogItemHiLoGenerator;
import com.eshop.catalog.domain.entity.CatalogBrand;
import com.eshop.catalog.domain.entity.CatalogItem;
import com.eshop.catalog.domain.entity.CatalogType;
import com.eshop.catalog.dto.PaginatedItemsDto;
import com.eshop.catalog.repository.CatalogBrandRepository;
import com.eshop.catalog.repository.CatalogItemRepository;
import com.eshop.catalog.repository.CatalogTypeRepository;

@Service
@Profile("!mock")
@Transactional(readOnly = true)
public class CatalogServiceImpl implements CatalogService {

    private final CatalogItemRepository catalogItemRepository;
    private final CatalogBrandRepository catalogBrandRepository;
    private final CatalogTypeRepository catalogTypeRepository;
    private final CatalogItemHiLoGenerator hiLoGenerator;

    public CatalogServiceImpl(CatalogItemRepository catalogItemRepository,
                              CatalogBrandRepository catalogBrandRepository,
                              CatalogTypeRepository catalogTypeRepository,
                              CatalogItemHiLoGenerator hiLoGenerator) {
        this.catalogItemRepository = catalogItemRepository;
        this.catalogBrandRepository = catalogBrandRepository;
        this.catalogTypeRepository = catalogTypeRepository;
        this.hiLoGenerator = hiLoGenerator;
    }

    @Override
    public CatalogItem findCatalogItem(int id) {
        return catalogItemRepository.findByIdWithBrandAndType(id).orElse(null);
    }

    @Override
    public List<CatalogBrand> getCatalogBrands() {
        return catalogBrandRepository.findAll();
    }

    @Override
    public PaginatedItemsDto<CatalogItem> getCatalogItemsPaginated(int pageSize, int pageIndex) {
        Page<CatalogItem> page = catalogItemRepository.findAll(
                PageRequest.of(pageIndex, pageSize, Sort.by("id")));
        return new PaginatedItemsDto<>(
                page.getNumber(),
                page.getSize(),
                page.getTotalElements(),
                page.getContent()
        );
    }

    @Override
    public List<CatalogType> getCatalogTypes() {
        return catalogTypeRepository.findAll();
    }

    @Override
    @Transactional
    public void createCatalogItem(CatalogItem catalogItem) {
        catalogItem.setId(hiLoGenerator.getNextSequenceValue());
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
