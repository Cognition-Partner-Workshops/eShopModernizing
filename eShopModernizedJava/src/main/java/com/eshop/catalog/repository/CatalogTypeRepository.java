package com.eshop.catalog.repository;

import com.eshop.catalog.model.CatalogType;
import org.springframework.data.jpa.repository.JpaRepository;

public interface CatalogTypeRepository extends JpaRepository<CatalogType, Integer> {
}
