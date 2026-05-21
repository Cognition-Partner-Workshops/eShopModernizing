package com.eshop.catalog.dto;

import java.util.Objects;

public class BrandDTO {

    private int id;
    private String brand;

    public BrandDTO() {
    }

    public BrandDTO(int id, String brand) {
        this.id = id;
        this.brand = brand;
    }

    public int getId() {
        return id;
    }

    public void setId(int id) {
        this.id = id;
    }

    public String getBrand() {
        return brand;
    }

    public void setBrand(String brand) {
        this.brand = brand;
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;
        BrandDTO brandDTO = (BrandDTO) o;
        return id == brandDTO.id && Objects.equals(brand, brandDTO.brand);
    }

    @Override
    public int hashCode() {
        return Objects.hash(id, brand);
    }

    @Override
    public String toString() {
        return "BrandDTO{id=" + id + ", brand='" + brand + "'}";
    }
}
