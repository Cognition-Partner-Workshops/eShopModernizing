package com.eshop.catalog.model;

import java.time.LocalDate;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.Table;

@Entity
@Table(name = "catalog_items_stock")
public class CatalogItemsStock {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "stock_id")
    private Integer stockId;

    private LocalDate date;

    @Column(name = "catalog_item_id")
    private int catalogItemId;

    @Column(name = "available_stock")
    private int availableStock;

    public CatalogItemsStock() {
    }

    public CatalogItemsStock(Integer stockId, LocalDate date, int catalogItemId, int availableStock) {
        this.stockId = stockId;
        this.date = date;
        this.catalogItemId = catalogItemId;
        this.availableStock = availableStock;
    }

    public Integer getStockId() {
        return stockId;
    }

    public void setStockId(Integer stockId) {
        this.stockId = stockId;
    }

    public LocalDate getDate() {
        return date;
    }

    public void setDate(LocalDate date) {
        this.date = date;
    }

    public int getCatalogItemId() {
        return catalogItemId;
    }

    public void setCatalogItemId(int catalogItemId) {
        this.catalogItemId = catalogItemId;
    }

    public int getAvailableStock() {
        return availableStock;
    }

    public void setAvailableStock(int availableStock) {
        this.availableStock = availableStock;
    }
}
