package com.eshop.catalog.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import com.eshop.catalog.domain.entity.CatalogBrand;

@Repository
public interface CatalogBrandRepository extends JpaRepository<CatalogBrand, Integer> {
}
