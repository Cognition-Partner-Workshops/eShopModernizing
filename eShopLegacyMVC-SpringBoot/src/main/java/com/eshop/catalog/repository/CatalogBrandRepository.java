package com.eshop.catalog.repository;

import com.eshop.catalog.model.CatalogBrand;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface CatalogBrandRepository extends JpaRepository<CatalogBrand, Integer> {
}
