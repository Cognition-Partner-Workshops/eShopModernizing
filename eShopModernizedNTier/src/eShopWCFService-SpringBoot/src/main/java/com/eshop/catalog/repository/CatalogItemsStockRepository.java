package com.eshop.catalog.repository;

import java.time.LocalDate;
import java.util.List;
import java.util.Optional;

import org.springframework.data.jpa.repository.JpaRepository;

import com.eshop.catalog.model.CatalogItemsStock;

public interface CatalogItemsStockRepository extends JpaRepository<CatalogItemsStock, Integer> {

    List<CatalogItemsStock> findByCatalogItemId(int catalogItemId);

    Optional<CatalogItemsStock> findByCatalogItemIdAndDate(int catalogItemId, LocalDate date);
}
