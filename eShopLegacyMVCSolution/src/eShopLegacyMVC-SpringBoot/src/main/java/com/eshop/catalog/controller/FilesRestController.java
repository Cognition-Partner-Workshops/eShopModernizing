package com.eshop.catalog.controller;

import com.eshop.catalog.dto.BrandDTO;
import com.eshop.catalog.service.ICatalogService;

import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;
import java.util.stream.Collectors;

@RestController
@RequestMapping("/api/files")
public class FilesRestController {

    private final ICatalogService service;

    public FilesRestController(ICatalogService service) {
        this.service = service;
    }

    @GetMapping
    public List<BrandDTO> get() {
        return service.getCatalogBrands().stream()
                .map(b -> new BrandDTO(b.getId(), b.getBrand()))
                .collect(Collectors.toList());
    }
}
