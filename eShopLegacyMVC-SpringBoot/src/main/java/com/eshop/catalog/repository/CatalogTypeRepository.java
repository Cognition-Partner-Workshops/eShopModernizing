package com.eshop.catalog.repository;

import com.eshop.catalog.model.CatalogType;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface CatalogTypeRepository extends JpaRepository<CatalogType, Integer> {
}
