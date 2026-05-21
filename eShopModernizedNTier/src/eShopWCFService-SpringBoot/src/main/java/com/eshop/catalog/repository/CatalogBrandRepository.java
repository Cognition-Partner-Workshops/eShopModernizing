package com.eshop.catalog.repository;

import org.springframework.data.jpa.repository.JpaRepository;

import com.eshop.catalog.model.CatalogBrand;

public interface CatalogBrandRepository extends JpaRepository<CatalogBrand, Integer> {
}
