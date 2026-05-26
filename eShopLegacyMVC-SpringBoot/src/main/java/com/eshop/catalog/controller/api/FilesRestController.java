package com.eshop.catalog.controller.api;

import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.service.CatalogService;
import com.eshop.catalog.util.JsonSerializationUtil;
import java.util.List;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/files")
public class FilesRestController {

  private final CatalogService catalogService;
  private final JsonSerializationUtil jsonSerializationUtil;

  public FilesRestController(
      CatalogService catalogService, JsonSerializationUtil jsonSerializationUtil) {
    this.catalogService = catalogService;
    this.jsonSerializationUtil = jsonSerializationUtil;
  }

  @GetMapping
  public ResponseEntity<byte[]> get() {
    List<CatalogBrand> brands = catalogService.getCatalogBrands();
    byte[] json = jsonSerializationUtil.serializeToBytes(brands);
    return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).body(json);
  }
}
