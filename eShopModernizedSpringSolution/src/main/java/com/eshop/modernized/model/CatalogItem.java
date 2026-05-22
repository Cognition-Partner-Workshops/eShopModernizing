package com.eshop.modernized.model;

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
import jakarta.validation.constraints.Digits;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;

import java.math.BigDecimal;
import java.util.Objects;

@Entity
@Table(name = "Catalog")
public class CatalogItem {

    public static final String DEFAULT_PICTURE_NAME = "dummy.png";

    @Id
    private Integer id;

    @NotBlank
    @Size(max = 50)
    @Column(nullable = false, length = 50)
    private String name;

    private String description;

    @NotNull
    @DecimalMin("0")
    @DecimalMax("1000000")
    @Digits(integer = 16, fraction = 2)
    @Column(precision = 18, scale = 2)
    private BigDecimal price;

    private String pictureFileName;

    @Transient
    private String pictureUri;

    @Column(name = "catalog_type_id", insertable = false, updatable = false)
    private Integer catalogTypeId;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "catalog_type_id")
    private CatalogType catalogType;

    @Column(name = "catalog_brand_id", insertable = false, updatable = false)
    private Integer catalogBrandId;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "catalog_brand_id")
    private CatalogBrand catalogBrand;

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
        this.pictureFileName = DEFAULT_PICTURE_NAME;
    }

    public Integer getId() {
        return id;
    }

    public void setId(Integer id) {
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

    public Integer getCatalogTypeId() {
        return catalogTypeId;
    }

    public CatalogType getCatalogType() {
        return catalogType;
    }

    public void setCatalogType(CatalogType catalogType) {
        this.catalogType = catalogType;
        this.catalogTypeId = (catalogType != null) ? catalogType.getId() : null;
    }

    public Integer getCatalogBrandId() {
        return catalogBrandId;
    }

    public CatalogBrand getCatalogBrand() {
        return catalogBrand;
    }

    public void setCatalogBrand(CatalogBrand catalogBrand) {
        this.catalogBrand = catalogBrand;
        this.catalogBrandId = (catalogBrand != null) ? catalogBrand.getId() : null;
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

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (!(o instanceof CatalogItem other)) return false;
        return Objects.equals(id, other.id);
    }

    @Override
    public int hashCode() {
        return Objects.hash(id);
    }

    @Override
    public String toString() {
        return "CatalogItem{id=" + id + ", name='" + name + "'}";
    }
}
