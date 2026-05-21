package com.eshop.catalog.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.FetchType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.Table;
import jakarta.persistence.Transient;
import jakarta.validation.constraints.DecimalMax;
import jakarta.validation.constraints.DecimalMin;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;

import java.math.BigDecimal;

@Entity
@Table(name = "Catalog")
public class CatalogItem {

    public static final String DEFAULT_PICTURE_NAME = "dummy.png";

    @Id
    private int id;

    @NotNull
    @Size(max = 50)
    private String name;

    private String description;

    @NotNull
    @DecimalMin("0")
    @DecimalMax("1000000")
    @Column(precision = 18, scale = 2)
    private BigDecimal price;

    @NotNull
    private String pictureFileName = DEFAULT_PICTURE_NAME;

    @Transient
    private String pictureUri;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "CatalogTypeId")
    private CatalogType catalogType;

    @Column(name = "CatalogTypeId", insertable = false, updatable = false)
    private int catalogTypeId;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "CatalogBrandId")
    private CatalogBrand catalogBrand;

    @Column(name = "CatalogBrandId", insertable = false, updatable = false)
    private int catalogBrandId;

    @Min(0)
    @Max(10000000)
    private int availableStock;

    @Min(0)
    @Max(10000000)
    private int restockThreshold;

    @Min(0)
    @Max(10000000)
    private int maxStockThreshold;

    private boolean onReorder;

    public CatalogItem() {
    }

    public int getId() {
        return id;
    }

    public void setId(int id) {
        this.id = id;
    }

    public String getName() {
        return name;
    }

    public void setName(String name) {
        this.name = name;
    }

    public String getDescription() {
        return description;
    }

    public void setDescription(String description) {
        this.description = description;
    }

    public BigDecimal getPrice() {
        return price;
    }

    public void setPrice(BigDecimal price) {
        this.price = price;
    }

    public String getPictureFileName() {
        return pictureFileName;
    }

    public void setPictureFileName(String pictureFileName) {
        this.pictureFileName = pictureFileName;
    }

    public String getPictureUri() {
        return pictureUri;
    }

    public void setPictureUri(String pictureUri) {
        this.pictureUri = pictureUri;
    }

    public CatalogType getCatalogType() {
        return catalogType;
    }

    public void setCatalogType(CatalogType catalogType) {
        this.catalogType = catalogType;
    }

    public int getCatalogTypeId() {
        return catalogTypeId;
    }

    public void setCatalogTypeId(int catalogTypeId) {
        this.catalogTypeId = catalogTypeId;
    }

    public CatalogBrand getCatalogBrand() {
        return catalogBrand;
    }

    public void setCatalogBrand(CatalogBrand catalogBrand) {
        this.catalogBrand = catalogBrand;
    }

    public int getCatalogBrandId() {
        return catalogBrandId;
    }

    public void setCatalogBrandId(int catalogBrandId) {
        this.catalogBrandId = catalogBrandId;
    }

    public int getAvailableStock() {
        return availableStock;
    }

    public void setAvailableStock(int availableStock) {
        this.availableStock = availableStock;
    }

    public int getRestockThreshold() {
        return restockThreshold;
    }

    public void setRestockThreshold(int restockThreshold) {
        this.restockThreshold = restockThreshold;
    }

    public int getMaxStockThreshold() {
        return maxStockThreshold;
    }

    public void setMaxStockThreshold(int maxStockThreshold) {
        this.maxStockThreshold = maxStockThreshold;
    }

    public boolean isOnReorder() {
        return onReorder;
    }

    public void setOnReorder(boolean onReorder) {
        this.onReorder = onReorder;
    }
}
