package com.eshop.catalog.repository;

import java.time.LocalDate;
import java.util.Optional;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import com.eshop.catalog.model.DiscountItem;

public interface DiscountItemRepository extends JpaRepository<DiscountItem, Integer> {

    @Query("SELECT d FROM DiscountItem d WHERE d.start <= :date AND d.end >= :date")
    Optional<DiscountItem> findActiveDiscount(@Param("date") LocalDate date);
}
