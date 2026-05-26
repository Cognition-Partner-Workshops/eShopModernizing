package com.eshop.catalog.repository;

import java.util.List;
import java.util.Optional;

import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.EntityGraph;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import com.eshop.catalog.domain.entity.CatalogItem;

@Repository
public interface CatalogItemRepository extends JpaRepository<CatalogItem, Integer> {

    @Query("SELECT ci FROM CatalogItem ci JOIN FETCH ci.catalogBrand JOIN FETCH ci.catalogType")
    List<CatalogItem> findAllWithBrandAndType();

    @EntityGraph(attributePaths = {"catalogBrand", "catalogType"})
    Page<CatalogItem> findAll(Pageable pageable);

    @Query("SELECT ci FROM CatalogItem ci JOIN FETCH ci.catalogBrand JOIN FETCH ci.catalogType WHERE ci.id = :id")
    Optional<CatalogItem> findByIdWithBrandAndType(@Param("id") int id);
}
