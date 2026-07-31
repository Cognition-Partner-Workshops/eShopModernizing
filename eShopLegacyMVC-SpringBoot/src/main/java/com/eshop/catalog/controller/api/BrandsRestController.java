package com.eshop.catalog.controller.api;

import java.util.List;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import com.eshop.catalog.domain.entity.CatalogBrand;
import com.eshop.catalog.service.CatalogService;

@RestController
@RequestMapping("/api/brands")
public class BrandsRestController {

    private static final Logger log = LoggerFactory.getLogger(BrandsRestController.class);

    private final CatalogService catalogService;

    public BrandsRestController(CatalogService catalogService) {
        this.catalogService = catalogService;
    }

    @GetMapping
    public ResponseEntity<List<CatalogBrand>> getAllBrands() {
        log.info("GET /api/brands - fetching all brands");
        List<CatalogBrand> brands = catalogService.getCatalogBrands();
        return ResponseEntity.ok(brands);
    }

    @GetMapping("/{id}")
    public ResponseEntity<CatalogBrand> getBrandById(@PathVariable int id) {
        log.info("GET /api/brands/{} - fetching brand by id", id);
        List<CatalogBrand> brands = catalogService.getCatalogBrands();
        return brands.stream()
                .filter(b -> b.getId() != null && b.getId() == id)
                .findFirst()
                .map(ResponseEntity::ok)
                .orElseGet(() -> {
                    log.warn("Brand with id {} not found", id);
                    return ResponseEntity.notFound().build();
                });
    }

    @DeleteMapping("/{id}")
    public ResponseEntity<Void> deleteBrand(@PathVariable int id) {
        log.info("DELETE /api/brands/{} - demo delete (no actual deletion)", id);
        boolean exists = catalogService.getCatalogBrands().stream()
                .anyMatch(b -> b.getId() != null && b.getId() == id);
        if (!exists) {
            log.warn("Brand with id {} not found for deletion", id);
            return ResponseEntity.notFound().build();
        }
        return ResponseEntity.ok().build();
    }
}
