package com.eshop.catalog.controller.api;

import java.util.List;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import com.eshop.catalog.dto.BrandDto;
import com.eshop.catalog.service.CatalogService;

@RestController
@RequestMapping("/api/files")
public class FilesRestController {

    private static final Logger log = LoggerFactory.getLogger(FilesRestController.class);

    private final CatalogService catalogService;

    public FilesRestController(CatalogService catalogService) {
        this.catalogService = catalogService;
    }

    @GetMapping
    public ResponseEntity<List<BrandDto>> getBrands() {
        log.info("Fetching all catalog brands");
        List<BrandDto> brands = catalogService.getCatalogBrands().stream()
                .map(b -> new BrandDto(b.getId(), b.getBrand()))
                .toList();
        log.debug("Returning {} brands", brands.size());
        return ResponseEntity.ok(brands);
    }
}
