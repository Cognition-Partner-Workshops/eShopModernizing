package com.eshop.catalog.service;

import java.util.List;
import java.util.Optional;

import org.springframework.context.annotation.Profile;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageRequest;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

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

    public CatalogServiceImpl(CatalogItemRepository catalogItemRepository,
                              CatalogBrandRepository catalogBrandRepository,
                              CatalogTypeRepository catalogTypeRepository) {
        this.catalogItemRepository = catalogItemRepository;
        this.catalogBrandRepository = catalogBrandRepository;
        this.catalogTypeRepository = catalogTypeRepository;
    }

    @Override
    public PaginatedItemsDto<CatalogItem> getPaginatedItems(int pageIndex, int pageSize) {
        Page<CatalogItem> page = catalogItemRepository.findAll(PageRequest.of(pageIndex, pageSize));
        return new PaginatedItemsDto<>(
                page.getNumber(),
                page.getSize(),
                page.getTotalElements(),
                page.getContent()
        );
    }

    @Override
    public Optional<CatalogItem> findById(int id) {
        return catalogItemRepository.findByIdWithBrandAndType(id);
    }

    @Override
    @Transactional
    public CatalogItem createItem(CatalogItem item) {
        return catalogItemRepository.save(item);
    }

    @Override
    @Transactional
    public CatalogItem updateItem(CatalogItem item) {
        return catalogItemRepository.save(item);
    }

    @Override
    @Transactional
    public void deleteItem(int id) {
        catalogItemRepository.deleteById(id);
    }

    @Override
    public List<CatalogBrand> getAllBrands() {
        return catalogBrandRepository.findAll();
    }

    @Override
    public List<CatalogType> getAllTypes() {
        return catalogTypeRepository.findAll();
    }
}
