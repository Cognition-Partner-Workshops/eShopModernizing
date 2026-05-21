package com.eshop.webforms.service;

import com.eshop.webforms.dto.PaginatedItemsViewModel;
import com.eshop.webforms.model.CatalogBrand;
import com.eshop.webforms.model.CatalogItem;
import com.eshop.webforms.model.CatalogType;
import com.eshop.webforms.repository.CatalogBrandRepository;
import com.eshop.webforms.repository.CatalogItemRepository;
import com.eshop.webforms.repository.CatalogTypeRepository;
import org.springframework.context.annotation.Profile;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Sort;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.server.ResponseStatusException;

import java.util.List;

@Service
@Profile("!mock")
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
    @Transactional(readOnly = true)
    public PaginatedItemsViewModel<CatalogItem> getCatalogItemsPaginated(int pageSize, int pageIndex) {
        Page<CatalogItem> page = catalogItemRepository.findAllWithBrandAndType(
                PageRequest.of(pageIndex, pageSize, Sort.by("id")));

        return new PaginatedItemsViewModel<>(
                pageIndex,
                pageSize,
                page.getTotalElements(),
                page.getContent());
    }

    @Override
    @Transactional(readOnly = true)
    public CatalogItem findCatalogItem(int id) {
        return catalogItemRepository.findByIdWithBrandAndType(id)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND,
                        "Catalog item with id " + id + " not found"));
    }

    @Override
    @Transactional(readOnly = true)
    public List<CatalogType> getCatalogTypes() {
        return catalogTypeRepository.findAll();
    }

    @Override
    @Transactional(readOnly = true)
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
