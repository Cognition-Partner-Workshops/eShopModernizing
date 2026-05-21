package com.eshop.catalog.controller.api;

import com.eshop.catalog.dto.BrandDto;
import com.eshop.catalog.service.CatalogService;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;

@RestController
@RequestMapping("/api/files")
public class FilesRestController {

    private final CatalogService catalogService;

    public FilesRestController(CatalogService catalogService) {
        this.catalogService = catalogService;
    }

    @GetMapping
    public List<BrandDto> get() {
        return catalogService.getCatalogBrands().stream()
                .map(b -> new BrandDto(b.getId(), b.getBrand()))
                .toList();
    }
}
