package com.eshop.catalog.repository;

import com.eshop.catalog.model.CatalogItem;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface CatalogItemRepository extends JpaRepository<CatalogItem, Integer> {

    Page<CatalogItem> findByCatalogBrandIdAndCatalogTypeId(int catalogBrandId, int catalogTypeId, Pageable pageable);

    Page<CatalogItem> findByCatalogBrandId(int catalogBrandId, Pageable pageable);

    Page<CatalogItem> findByCatalogTypeId(int catalogTypeId, Pageable pageable);
}
