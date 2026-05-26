package com.eshop.catalog.repository;

import com.eshop.catalog.model.CatalogItem;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;

public interface CatalogItemRepository extends JpaRepository<CatalogItem, Integer> {

  @Query("SELECT ci FROM CatalogItem ci JOIN FETCH ci.catalogType JOIN FETCH ci.catalogBrand")
  Page<CatalogItem> findAllWithBrandAndType(Pageable pageable);
}
