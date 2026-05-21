package com.eshop.catalog.repository;

import java.util.List;
import java.util.Optional;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import com.eshop.catalog.model.CatalogItem;

public interface CatalogItemRepository extends JpaRepository<CatalogItem, Integer> {

    @Query("SELECT ci FROM CatalogItem ci JOIN FETCH ci.catalogBrand JOIN FETCH ci.catalogType WHERE ci.id = :id")
    Optional<CatalogItem> findByIdWithBrandAndType(@Param("id") int id);

    @Query("SELECT ci FROM CatalogItem ci JOIN FETCH ci.catalogBrand JOIN FETCH ci.catalogType")
    List<CatalogItem> findAllWithBrandAndType();
}
