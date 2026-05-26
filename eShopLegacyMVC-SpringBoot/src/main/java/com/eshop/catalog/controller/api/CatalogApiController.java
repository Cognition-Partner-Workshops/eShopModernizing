package com.eshop.catalog.controller.api;

import java.util.Map;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api")
public class CatalogApiController {

  @GetMapping
  public Map<String, String> index() {
    return Map.of("message", "Hello World!");
  }
}
