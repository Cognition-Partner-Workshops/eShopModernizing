package com.eshop.catalog.service.impl;

import com.eshop.catalog.dto.PaginatedItemsDto;
import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogType;
import com.eshop.catalog.repository.CatalogBrandRepository;
import com.eshop.catalog.repository.CatalogItemRepository;
import com.eshop.catalog.repository.CatalogTypeRepository;
import com.eshop.catalog.service.CatalogService;
import com.eshop.catalog.util.CatalogItemHiLoGenerator;
import java.util.List;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Sort;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
@ConditionalOnProperty(name = "app.use-mock-data", havingValue = "false", matchIfMissing = false)
public class CatalogServiceImpl implements CatalogService {

  private final CatalogItemRepository catalogItemRepository;
  private final CatalogBrandRepository catalogBrandRepository;
  private final CatalogTypeRepository catalogTypeRepository;
  private final CatalogItemHiLoGenerator indexGenerator;

  public CatalogServiceImpl(
      CatalogItemRepository catalogItemRepository,
      CatalogBrandRepository catalogBrandRepository,
      CatalogTypeRepository catalogTypeRepository,
      CatalogItemHiLoGenerator indexGenerator) {
    this.catalogItemRepository = catalogItemRepository;
    this.catalogBrandRepository = catalogBrandRepository;
    this.catalogTypeRepository = catalogTypeRepository;
    this.indexGenerator = indexGenerator;
  }

  @Override
  public PaginatedItemsDto<CatalogItem> getCatalogItemsPaginated(int pageSize, int pageIndex) {
    PageRequest pageRequest = PageRequest.of(pageIndex, pageSize, Sort.by("id"));
    Page<CatalogItem> page = catalogItemRepository.findAllWithBrandAndType(pageRequest);

    return new PaginatedItemsDto<>(
        pageIndex, pageSize, page.getTotalElements(), page.getContent());
  }

  @Override
  public CatalogItem findCatalogItem(int id) {
    return catalogItemRepository.findByIdWithBrandAndType(id).orElse(null);
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
    catalogItem.setId(indexGenerator.getNextSequenceValue());
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
