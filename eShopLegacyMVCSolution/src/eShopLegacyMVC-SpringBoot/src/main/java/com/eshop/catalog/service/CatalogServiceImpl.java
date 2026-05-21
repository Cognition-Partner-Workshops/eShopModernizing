package com.eshop.catalog.service;

import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogItemHiLoGenerator;
import com.eshop.catalog.model.CatalogType;
import com.eshop.catalog.repository.CatalogBrandRepository;
import com.eshop.catalog.repository.CatalogItemRepository;
import com.eshop.catalog.repository.CatalogTypeRepository;
import com.eshop.catalog.viewmodel.PaginatedItemsViewModel;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageRequest;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;

@Transactional
public class CatalogServiceImpl implements ICatalogService {

    private final CatalogItemRepository itemRepository;
    private final CatalogBrandRepository brandRepository;
    private final CatalogTypeRepository typeRepository;
    private final CatalogItemHiLoGenerator hiLoGenerator;

    public CatalogServiceImpl(CatalogItemRepository itemRepository,
                              CatalogBrandRepository brandRepository,
                              CatalogTypeRepository typeRepository,
                              CatalogItemHiLoGenerator hiLoGenerator) {
        this.itemRepository = itemRepository;
        this.brandRepository = brandRepository;
        this.typeRepository = typeRepository;
        this.hiLoGenerator = hiLoGenerator;
    }

    @Override
    @Transactional(readOnly = true)
    public CatalogItem findCatalogItem(int id) {
        return itemRepository.findByIdWithBrandAndType(id).orElse(null);
    }

    @Override
    @Transactional(readOnly = true)
    public List<CatalogBrand> getCatalogBrands() {
        return brandRepository.findAll();
    }

    @Override
    @Transactional(readOnly = true)
    public PaginatedItemsViewModel<CatalogItem> getCatalogItemsPaginated(int pageSize, int pageIndex) {
        Page<CatalogItem> page = itemRepository.findAllWithBrandAndType(PageRequest.of(pageIndex, pageSize));
        return new PaginatedItemsViewModel<>(
                pageIndex,
                pageSize,
                page.getTotalElements(),
                page.getContent()
        );
    }

    @Override
    @Transactional(readOnly = true)
    public List<CatalogType> getCatalogTypes() {
        return typeRepository.findAll();
    }

    @Override
    public CatalogItem createCatalogItem(CatalogItem item) {
        item.setId(hiLoGenerator.getNextSequenceValue());
        return itemRepository.save(item);
    }

    @Override
    public CatalogItem updateCatalogItem(CatalogItem item) {
        return itemRepository.save(item);
    }

    @Override
    public void removeCatalogItem(CatalogItem item) {
        itemRepository.delete(item);
    }
}
