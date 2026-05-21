package com.eshop.catalog.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.SequenceGenerator;
import jakarta.persistence.Table;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;

@Entity
@Table(name = "CatalogType")
public class CatalogType {

    @Id
    @GeneratedValue(strategy = GenerationType.SEQUENCE, generator = "catalog_type_hilo")
    @SequenceGenerator(name = "catalog_type_hilo", sequenceName = "catalog_type_hilo", allocationSize = 10)
    private Integer id;

    @NotNull
    @Size(max = 100)
    @Column(name = "Type", nullable = false, length = 100)
    private String type;

    public CatalogType() {
    }

    public Integer getId() {
        return id;
    }

    public void setId(Integer id) {
        this.id = id;
    }

    public String getType() {
        return type;
    }

    public void setType(String type) {
        this.type = type;
    }
}
