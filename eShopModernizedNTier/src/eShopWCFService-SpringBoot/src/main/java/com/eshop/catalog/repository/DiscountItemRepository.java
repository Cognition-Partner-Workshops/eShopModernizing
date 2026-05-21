package com.eshop.catalog.repository;

import java.time.LocalDate;
import java.util.List;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import com.eshop.catalog.model.DiscountItem;

public interface DiscountItemRepository extends JpaRepository<DiscountItem, Integer> {

    @Query("SELECT d FROM DiscountItem d WHERE d.start <= :date AND d.end >= :date ORDER BY d.start ASC")
    List<DiscountItem> findActiveDiscounts(@Param("date") LocalDate date);
}
