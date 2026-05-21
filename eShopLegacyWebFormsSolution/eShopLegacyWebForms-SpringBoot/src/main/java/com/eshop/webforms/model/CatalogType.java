package com.eshop.webforms.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

@Entity
@Table(name = "CatalogType")
public class CatalogType {

    @Id
    private int id;

    @NotBlank
    @Size(max = 100)
    @Column(name = "Type", nullable = false, length = 100)
    private String type;

    public CatalogType() {
    }

    public int getId() {
        return id;
    }

    public void setId(int id) {
        this.id = id;
    }

    public String getType() {
        return type;
    }

    public void setType(String type) {
        this.type = type;
    }
}
