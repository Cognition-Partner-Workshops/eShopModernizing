package com.eshop.webforms.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.FetchType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.Table;
import jakarta.persistence.Transient;
import jakarta.validation.constraints.DecimalMin;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;
import java.math.BigDecimal;

@Entity
@Table(name = "Catalog")
public class CatalogItem {

    @Id
    private int id;

    @NotBlank
    @Size(max = 50)
    @Column(name = "Name", nullable = false, length = 50)
    private String name;

    @Column(name = "Description", length = 2048)
    private String description;

    @DecimalMin("0")
    @Column(name = "Price", nullable = false, precision = 18, scale = 2)
    private BigDecimal price;

    @NotBlank
    @Column(name = "PictureFileName", nullable = false)
    private String pictureFileName = "dummy.png";

    @Transient
    private String pictureUri;

    @Column(name = "CatalogTypeId", nullable = false)
    private int catalogTypeId;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "CatalogTypeId", insertable = false, updatable = false)
    private CatalogType catalogType;

    @Column(name = "CatalogBrandId", nullable = false)
    private int catalogBrandId;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "CatalogBrandId", insertable = false, updatable = false)
    private CatalogBrand catalogBrand;

    @Min(0)
    @Max(10000000)
    @Column(name = "AvailableStock", nullable = false)
    private int availableStock;

    @Min(0)
    @Max(10000000)
    @Column(name = "RestockThreshold", nullable = false)
    private int restockThreshold;

    @Min(0)
    @Max(10000000)
    @Column(name = "MaxStockThreshold", nullable = false)
    private int maxStockThreshold;

    @Column(name = "OnReorder", nullable = false)
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

    public int getCatalogTypeId() {
        return catalogTypeId;
    }

    public void setCatalogTypeId(int catalogTypeId) {
        this.catalogTypeId = catalogTypeId;
    }

    public CatalogType getCatalogType() {
        return catalogType;
    }

    public void setCatalogType(CatalogType catalogType) {
        this.catalogType = catalogType;
    }

    public int getCatalogBrandId() {
        return catalogBrandId;
    }

    public void setCatalogBrandId(int catalogBrandId) {
        this.catalogBrandId = catalogBrandId;
    }

    public CatalogBrand getCatalogBrand() {
        return catalogBrand;
    }

    public void setCatalogBrand(CatalogBrand catalogBrand) {
        this.catalogBrand = catalogBrand;
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
