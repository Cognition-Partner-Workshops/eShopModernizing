package com.eshop.catalog.controller.api;

import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.service.CatalogService;
import java.util.List;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/brands")
public class BrandsRestController {

  private final CatalogService catalogService;

  public BrandsRestController(CatalogService catalogService) {
    this.catalogService = catalogService;
  }

  @GetMapping
  public List<CatalogBrand> getAll() {
    return catalogService.getCatalogBrands();
  }

  @GetMapping("/{id}")
  public ResponseEntity<CatalogBrand> getById(@PathVariable int id) {
    List<CatalogBrand> brands = catalogService.getCatalogBrands();
    return brands.stream()
        .filter(b -> b.getId() == id)
        .findFirst()
        .map(ResponseEntity::ok)
        .orElse(ResponseEntity.notFound().build());
  }

  @DeleteMapping("/{id}")
  public ResponseEntity<Void> delete(@PathVariable int id) {
    List<CatalogBrand> brands = catalogService.getCatalogBrands();
    boolean exists = brands.stream().anyMatch(b -> b.getId() == id);
    if (!exists) {
      return ResponseEntity.notFound().build();
    }
    // demo only - don't actually delete
    return ResponseEntity.ok().build();
  }
}
