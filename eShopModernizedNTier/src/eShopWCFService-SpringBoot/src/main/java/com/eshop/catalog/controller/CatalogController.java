package com.eshop.catalog.controller;

import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogItemsStock;
import com.eshop.catalog.model.CatalogType;
import com.eshop.catalog.model.DiscountItem;
import com.eshop.catalog.service.CatalogService;

import org.springframework.format.annotation.DateTimeFormat;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import java.net.URI;
import java.time.LocalDate;
import java.util.List;

@RestController
@RequestMapping("/api/catalog")
public class CatalogController {

    private final CatalogService catalogService;

    public CatalogController(CatalogService catalogService) {
        this.catalogService = catalogService;
    }

    @GetMapping("/items/{id}")
    public ResponseEntity<CatalogItem> findCatalogItem(@PathVariable int id) {
        return catalogService.findCatalogItem(id)
                .map(ResponseEntity::ok)
                .orElse(ResponseEntity.notFound().build());
    }

    @GetMapping("/items")
    public List<CatalogItem> getCatalogItems(
            @RequestParam(defaultValue = "0") int brandId,
            @RequestParam(defaultValue = "0") int typeId) {
        return catalogService.getCatalogItems(brandId, typeId);
    }

    @PostMapping("/items")
    public ResponseEntity<CatalogItem> createCatalogItem(@RequestBody CatalogItem catalogItem) {
        CatalogItem created = catalogService.createCatalogItem(catalogItem);
        return ResponseEntity
                .created(URI.create("/api/catalog/items/" + created.getId()))
                .body(created);
    }

    @PutMapping("/items")
    public ResponseEntity<CatalogItem> updateCatalogItem(@RequestBody CatalogItem catalogItem) {
        CatalogItem updated = catalogService.updateCatalogItem(catalogItem);
        return ResponseEntity.ok(updated);
    }

    @DeleteMapping("/items/{id}")
    public ResponseEntity<Void> removeCatalogItem(@PathVariable int id) {
        catalogService.removeCatalogItem(id);
        return ResponseEntity.noContent().build();
    }

    @GetMapping("/brands")
    public List<CatalogBrand> getCatalogBrands() {
        return catalogService.getCatalogBrands();
    }

    @GetMapping("/types")
    public List<CatalogType> getCatalogTypes() {
        return catalogService.getCatalogTypes();
    }

    @GetMapping("/stock")
    public int getAvailableStock(
            @RequestParam @DateTimeFormat(iso = DateTimeFormat.ISO.DATE) LocalDate date,
            @RequestParam int catalogItemId) {
        return catalogService.getAvailableStock(date, catalogItemId);
    }

    @PostMapping("/stock")
    public ResponseEntity<Void> createAvailableStock(@RequestBody CatalogItemsStock catalogItemsStock) {
        catalogService.createAvailableStock(catalogItemsStock);
        return ResponseEntity.created(URI.create("/api/catalog/stock")).build();
    }

    @GetMapping("/discount")
    public ResponseEntity<DiscountItem> getDiscount(
            @RequestParam @DateTimeFormat(iso = DateTimeFormat.ISO.DATE) LocalDate date) {
        return catalogService.getDiscount(date)
                .map(ResponseEntity::ok)
                .orElse(ResponseEntity.notFound().build());
    }
}
