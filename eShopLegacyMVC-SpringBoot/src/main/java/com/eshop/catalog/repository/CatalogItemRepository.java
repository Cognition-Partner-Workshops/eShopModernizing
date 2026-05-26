package com.eshop.catalog.repository;

import com.eshop.catalog.model.CatalogItem;
import java.util.Optional;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

@Repository
public interface CatalogItemRepository extends JpaRepository<CatalogItem, Integer> {

    @Query(value = "SELECT ci FROM CatalogItem ci JOIN FETCH ci.catalogType JOIN FETCH ci.catalogBrand",
            countQuery = "SELECT count(ci) FROM CatalogItem ci")
    Page<CatalogItem> findAllWithBrandAndType(Pageable pageable);

    @Query("SELECT ci FROM CatalogItem ci"
            + " JOIN FETCH ci.catalogType"
            + " JOIN FETCH ci.catalogBrand"
            + " WHERE ci.id = :id")
    Optional<CatalogItem> findByIdWithBrandAndType(@Param("id") int id);

    Page<CatalogItem> findByCatalogBrandIdAndCatalogTypeId(int catalogBrandId, int catalogTypeId, Pageable pageable);

    Page<CatalogItem> findByCatalogBrandId(int catalogBrandId, Pageable pageable);

    Page<CatalogItem> findByCatalogTypeId(int catalogTypeId, Pageable pageable);
}
