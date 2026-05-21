package com.eshop.catalog.service;

import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogItemsStock;
import com.eshop.catalog.model.CatalogType;
import com.eshop.catalog.model.DiscountItem;
import com.eshop.catalog.repository.CatalogBrandRepository;
import com.eshop.catalog.repository.CatalogItemRepository;
import com.eshop.catalog.repository.CatalogItemsStockRepository;
import com.eshop.catalog.repository.CatalogTypeRepository;
import com.eshop.catalog.repository.DiscountItemRepository;

import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDate;
import java.util.List;
import java.util.Optional;

@Service
@Profile("!mock")
public class CatalogServiceImpl implements CatalogService {

    private final CatalogItemRepository catalogItemRepository;
    private final CatalogBrandRepository catalogBrandRepository;
    private final CatalogTypeRepository catalogTypeRepository;
    private final CatalogItemsStockRepository catalogItemsStockRepository;
    private final DiscountItemRepository discountItemRepository;

    public CatalogServiceImpl(CatalogItemRepository catalogItemRepository,
                              CatalogBrandRepository catalogBrandRepository,
                              CatalogTypeRepository catalogTypeRepository,
                              CatalogItemsStockRepository catalogItemsStockRepository,
                              DiscountItemRepository discountItemRepository) {
        this.catalogItemRepository = catalogItemRepository;
        this.catalogBrandRepository = catalogBrandRepository;
        this.catalogTypeRepository = catalogTypeRepository;
        this.catalogItemsStockRepository = catalogItemsStockRepository;
        this.discountItemRepository = discountItemRepository;
    }

    @Override
    @Transactional(readOnly = true)
    public Optional<CatalogItem> findCatalogItem(int id) {
        return catalogItemRepository.findByIdWithBrandAndType(id);
    }

    @Override
    @Transactional(readOnly = true)
    public List<CatalogBrand> getCatalogBrands() {
        return catalogBrandRepository.findAll();
    }

    @Override
    @Transactional(readOnly = true)
    public List<CatalogItem> getCatalogItems(int brandIdFilter, int typeIdFilter) {
        boolean brandFilterIsNull = brandIdFilter == 0;
        boolean typeFilterIsNull = typeIdFilter == 0;

        List<CatalogItem> items = catalogItemRepository.findAllWithBrandAndType();

        return items.stream()
                .filter(item -> brandFilterIsNull || item.getCatalogBrandId() == brandIdFilter)
                .filter(item -> typeFilterIsNull || item.getCatalogTypeId() == typeIdFilter)
                .toList();
    }

    @Override
    @Transactional(readOnly = true)
    public List<CatalogType> getCatalogTypes() {
        return catalogTypeRepository.findAll();
    }

    @Override
    @Transactional(readOnly = true)
    public int getAvailableStock(LocalDate date, int catalogItemId) {
        return catalogItemsStockRepository.findByCatalogItemIdAndDate(catalogItemId, date)
                .map(CatalogItemsStock::getAvailableStock)
                .orElse(0);
    }

    @Override
    @Transactional
    public void createAvailableStock(CatalogItemsStock catalogItemsStock) {
        Optional<CatalogItemsStock> existing = catalogItemsStockRepository
                .findByCatalogItemIdAndDate(catalogItemsStock.getCatalogItemId(), catalogItemsStock.getDate());

        if (existing.isPresent()) {
            CatalogItemsStock stock = existing.get();
            stock.setAvailableStock(catalogItemsStock.getAvailableStock());
            catalogItemsStockRepository.save(stock);
        } else {
            catalogItemsStockRepository.save(catalogItemsStock);
        }
    }

    @Override
    @Transactional
    public CatalogItem createCatalogItem(CatalogItem catalogItem) {
        resolveRelationships(catalogItem);
        return catalogItemRepository.save(catalogItem);
    }

    @Override
    @Transactional
    public CatalogItem updateCatalogItem(CatalogItem catalogItem) {
        resolveRelationships(catalogItem);
        return catalogItemRepository.save(catalogItem);
    }

    private void resolveRelationships(CatalogItem catalogItem) {
        if (catalogItem.getCatalogBrand() == null && catalogItem.getCatalogBrandId() != 0) {
            catalogBrandRepository.findById(catalogItem.getCatalogBrandId())
                    .ifPresent(catalogItem::setCatalogBrand);
        }
        if (catalogItem.getCatalogType() == null && catalogItem.getCatalogTypeId() != 0) {
            catalogTypeRepository.findById(catalogItem.getCatalogTypeId())
                    .ifPresent(catalogItem::setCatalogType);
        }
    }

    @Override
    @Transactional
    public void removeCatalogItem(int id) {
        catalogItemRepository.deleteById(id);
    }

    @Override
    @Transactional(readOnly = true)
    public Optional<DiscountItem> getDiscount(LocalDate day) {
        return discountItemRepository.findActiveDiscounts(day).stream().findFirst();
    }
}
