package com.eshop.catalog.controller.api;

import java.util.Map;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api")
public class CatalogApiController {

    private static final Logger log = LoggerFactory.getLogger(CatalogApiController.class);

    @GetMapping
    public Map<String, String> index() {
        log.info("GET /api called");
        return Map.of("message", "Hello World!");
    }
}
