package com.eshop.catalog.repository;

import com.eshop.catalog.model.CatalogItem;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.EntityGraph;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface CatalogItemRepository extends JpaRepository<CatalogItem, Integer> {

    @EntityGraph(attributePaths = {"catalogBrand", "catalogType"})
    Page<CatalogItem> findAll(Pageable pageable);

    @EntityGraph(attributePaths = {"catalogBrand", "catalogType"})
    Optional<CatalogItem> findById(Integer id);
}
