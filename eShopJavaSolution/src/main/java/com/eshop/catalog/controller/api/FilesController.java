package com.eshop.catalog.controller.api;

import com.eshop.catalog.util.JsonSerializationUtil;
import java.io.IOException;
import java.io.InputStream;
import org.springframework.core.io.InputStreamResource;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/**
 * REST endpoint that serialises catalog brands to JSON, replacing the legacy FilesController that
 * used BinaryFormatter.
 */
@RestController
@RequestMapping("api/files")
public class FilesController {

  @GetMapping
  public ResponseEntity<InputStreamResource> getBrands() throws IOException {
    // Placeholder: the actual brand list will be wired in when ICatalogService is migrated.
    // For now, return an empty JSON array to prove the serialization path works.
    var data = new Object[] {};
    InputStream stream = JsonSerializationUtil.serializeToStream(data);
    return ResponseEntity.ok()
        .contentType(MediaType.APPLICATION_JSON)
        .body(new InputStreamResource(stream));
  }
}
