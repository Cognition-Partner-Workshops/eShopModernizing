package com.eshop.catalog.repository;

import com.eshop.catalog.model.CatalogItem;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.EntityGraph;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.stereotype.Repository;

import java.util.Optional;

@Repository
public interface CatalogItemRepository extends JpaRepository<CatalogItem, Integer> {

    @Query("SELECT c FROM CatalogItem c JOIN FETCH c.catalogBrand JOIN FETCH c.catalogType ORDER BY c.id")
    Page<CatalogItem> findAllWithBrandAndType(Pageable pageable);

    @Query("SELECT c FROM CatalogItem c JOIN FETCH c.catalogBrand JOIN FETCH c.catalogType WHERE c.id = :id")
    Optional<CatalogItem> findByIdWithBrandAndType(int id);
}
