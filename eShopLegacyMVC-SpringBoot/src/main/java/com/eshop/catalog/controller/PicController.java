package com.eshop.catalog.controller;

import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.service.CatalogService;
import java.io.IOException;
import java.io.InputStream;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.core.io.ClassPathResource;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;

@Controller
public class PicController {

  public static final String GET_PIC_ROUTE_NAME = "GetPicRouteTemplate";

  private static final Logger log = LoggerFactory.getLogger(PicController.class);

  private final CatalogService catalogService;

  public PicController(CatalogService catalogService) {
    this.catalogService = catalogService;
  }

  @GetMapping(value = "/items/{catalogItemId}/pic", name = GET_PIC_ROUTE_NAME)
  public ResponseEntity<byte[]> getImage(@PathVariable int catalogItemId) {
    log.info("Now loading... /items/{}/pic", catalogItemId);

    if (catalogItemId <= 0) {
      return ResponseEntity.badRequest().build();
    }

    CatalogItem item = catalogService.findCatalogItem(catalogItemId);
    if (item == null) {
      return ResponseEntity.notFound().build();
    }

    String pictureFileName = item.getPictureFileName();
    if (pictureFileName == null
        || pictureFileName.contains("..")
        || pictureFileName.contains("/")
        || pictureFileName.contains("\\")) {
      log.warn("Invalid picture file name for catalog item {}: {}", catalogItemId, pictureFileName);
      return ResponseEntity.badRequest().build();
    }

    ClassPathResource resource = new ClassPathResource("static/pics/" + pictureFileName);

    try (InputStream inputStream = resource.getInputStream()) {
      byte[] imageBytes = inputStream.readAllBytes();
      MediaType mediaType = getMediaType(pictureFileName);
      return ResponseEntity.ok().contentType(mediaType).body(imageBytes);
    } catch (IOException e) {
      log.warn("Image file not found: {}", pictureFileName);
      return ResponseEntity.status(HttpStatus.NOT_FOUND).build();
    }
  }

  private MediaType getMediaType(String fileName) {
    String extension = "";
    int dotIndex = fileName.lastIndexOf('.');
    if (dotIndex > 0) {
      extension = fileName.substring(dotIndex).toLowerCase();
    }

    return switch (extension) {
      case ".png" -> MediaType.IMAGE_PNG;
      case ".gif" -> MediaType.IMAGE_GIF;
      case ".jpg", ".jpeg" -> MediaType.IMAGE_JPEG;
      case ".bmp" -> MediaType.parseMediaType("image/bmp");
      case ".tiff" -> MediaType.parseMediaType("image/tiff");
      case ".wmf" -> MediaType.parseMediaType("image/wmf");
      case ".jp2" -> MediaType.parseMediaType("image/jp2");
      case ".svg" -> MediaType.parseMediaType("image/svg+xml");
      default -> MediaType.APPLICATION_OCTET_STREAM;
    };
  }
}
