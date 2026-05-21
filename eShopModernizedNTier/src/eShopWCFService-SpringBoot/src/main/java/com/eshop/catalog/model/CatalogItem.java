package com.eshop.catalog.model;

import java.math.BigDecimal;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.FetchType;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.Table;
import jakarta.validation.constraints.NotNull;

@Entity
@Table(name = "catalog_items")
public class CatalogItem {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Integer id;

    private String description;

    @NotNull
    private String name;

    @Column(precision = 19, scale = 4)
    private BigDecimal price;

    @Column(name = "picture_filename")
    private String pictureFilename;

    @Column(name = "catalog_brand_id", insertable = false, updatable = false)
    private int catalogBrandId;

    @Column(name = "catalog_type_id", insertable = false, updatable = false)
    private int catalogTypeId;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "catalog_brand_id")
    private CatalogBrand catalogBrand;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "catalog_type_id")
    private CatalogType catalogType;

    public CatalogItem() {
    }

    public CatalogItem(Integer id, String description, String name, BigDecimal price,
                       String pictureFilename, CatalogBrand catalogBrand, CatalogType catalogType) {
        this.id = id;
        this.description = description;
        this.name = name;
        this.price = price;
        this.pictureFilename = pictureFilename;
        this.catalogBrand = catalogBrand;
        this.catalogType = catalogType;
    }

    public Integer getId() {
        return id;
    }

    public void setId(Integer id) {
        this.id = id;
    }

    public String getDescription() {
        return description;
    }

    public void setDescription(String description) {
        this.description = description;
    }

    public String getName() {
        return name;
    }

    public void setName(String name) {
        this.name = name;
    }

    public BigDecimal getPrice() {
        return price;
    }

    public void setPrice(BigDecimal price) {
        this.price = price;
    }

    public String getPictureFilename() {
        return pictureFilename;
    }

    public void setPictureFilename(String pictureFilename) {
        this.pictureFilename = pictureFilename;
    }

    public int getCatalogBrandId() {
        return catalogBrandId;
    }

    public void setCatalogBrandId(int catalogBrandId) {
        this.catalogBrandId = catalogBrandId;
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

    public CatalogType getCatalogType() {
        return catalogType;
    }

    public void setCatalogType(CatalogType catalogType) {
        this.catalogType = catalogType;
    }
}
