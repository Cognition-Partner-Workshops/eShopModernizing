package com.eshop.catalog.controller;

import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.service.ICatalogService;

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;

@RestController
@RequestMapping("/api/brands")
public class BrandsRestController {

    private final ICatalogService service;

    public BrandsRestController(ICatalogService service) {
        this.service = service;
    }

    @GetMapping
    public List<CatalogBrand> getAll() {
        return service.getCatalogBrands();
    }

    @GetMapping("/{id}")
    public ResponseEntity<CatalogBrand> getById(@PathVariable int id) {
        List<CatalogBrand> brands = service.getCatalogBrands();
        return brands.stream()
                .filter(b -> b.getId() == id)
                .findFirst()
                .map(ResponseEntity::ok)
                .orElse(ResponseEntity.notFound().build());
    }

    @DeleteMapping("/{id}")
    public ResponseEntity<Void> delete(@PathVariable int id) {
        List<CatalogBrand> brands = service.getCatalogBrands();
        boolean found = brands.stream().anyMatch(b -> b.getId() == id);
        if (found) {
            return ResponseEntity.ok().build();
        }
        return ResponseEntity.notFound().build();
    }
}
