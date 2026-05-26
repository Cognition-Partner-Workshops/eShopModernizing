package com.eshop.catalog.service.impl;

import com.eshop.catalog.config.CatalogItemHiLoGenerator;
import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogType;
import com.eshop.catalog.repository.CatalogBrandRepository;
import com.eshop.catalog.repository.CatalogItemRepository;
import com.eshop.catalog.repository.CatalogTypeRepository;
import com.eshop.catalog.service.CatalogService;
import java.util.List;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Sort;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class CatalogServiceImpl implements CatalogService {

  private final CatalogItemRepository catalogItemRepository;
  private final CatalogBrandRepository catalogBrandRepository;
  private final CatalogTypeRepository catalogTypeRepository;
  private final CatalogItemHiLoGenerator hiLoGenerator;

  @Autowired
  public CatalogServiceImpl(
      CatalogItemRepository catalogItemRepository,
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
    return catalogItemRepository.findById(id).orElse(null);
  }

  @Override
  public List<CatalogBrand> getCatalogBrands() {
    return catalogBrandRepository.findAll();
  }

  @Override
  public Page<CatalogItem> getCatalogItemsPaginated(int pageSize, int pageIndex) {
    PageRequest pageRequest = PageRequest.of(pageIndex, pageSize, Sort.by("id"));
    return catalogItemRepository.findAll(pageRequest);
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
