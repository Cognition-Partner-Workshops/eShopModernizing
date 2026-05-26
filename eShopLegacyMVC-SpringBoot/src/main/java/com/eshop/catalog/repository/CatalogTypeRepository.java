package com.eshop.catalog.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import com.eshop.catalog.domain.entity.CatalogType;

@Repository
public interface CatalogTypeRepository extends JpaRepository<CatalogType, Integer> {
}
