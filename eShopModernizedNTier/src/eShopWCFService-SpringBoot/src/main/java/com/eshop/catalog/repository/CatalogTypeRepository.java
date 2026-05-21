package com.eshop.catalog.repository;

import org.springframework.data.jpa.repository.JpaRepository;

import com.eshop.catalog.model.CatalogType;

public interface CatalogTypeRepository extends JpaRepository<CatalogType, Integer> {
}
