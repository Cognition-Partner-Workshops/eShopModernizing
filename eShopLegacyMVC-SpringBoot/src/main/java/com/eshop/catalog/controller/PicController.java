package com.eshop.catalog.controller;

import java.io.IOException;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.core.io.ClassPathResource;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;

import com.eshop.catalog.domain.entity.CatalogItem;
import com.eshop.catalog.service.CatalogService;

@Controller
public class PicController {

    private static final Logger log = LoggerFactory.getLogger(PicController.class);

    private final CatalogService catalogService;

    public PicController(CatalogService catalogService) {
        this.catalogService = catalogService;
    }

    @GetMapping("/items/{catalogItemId}/pic")
    public ResponseEntity<byte[]> getItemPicture(@PathVariable int catalogItemId) {
        if (catalogItemId <= 0) {
            log.warn("Invalid catalogItemId: {}", catalogItemId);
            return ResponseEntity.badRequest().build();
        }

        CatalogItem item = catalogService.findCatalogItem(catalogItemId);
        if (item == null) {
            log.warn("Catalog item not found for id: {}", catalogItemId);
            return ResponseEntity.notFound().build();
        }

        String fileName = item.getPictureFileName();
        String extension = getFileExtension(fileName);
        String mimeType = getImageMimeType(extension);

        ClassPathResource resource = new ClassPathResource("static/pics/" + fileName);
        try {
            byte[] imageBytes = resource.getContentAsByteArray();
            HttpHeaders headers = new HttpHeaders();
            headers.set(HttpHeaders.CONTENT_TYPE, mimeType);
            return new ResponseEntity<>(imageBytes, headers, HttpStatus.OK);
        } catch (IOException e) {
            log.error("Failed to read image file: {}", fileName, e);
            return ResponseEntity.notFound().build();
        }
    }

    private String getFileExtension(String fileName) {
        if (fileName == null || !fileName.contains(".")) {
            return "";
        }
        return fileName.substring(fileName.lastIndexOf('.')).toLowerCase();
    }

    private String getImageMimeType(String extension) {
        return switch (extension) {
            case ".png" -> "image/png";
            case ".gif" -> "image/gif";
            case ".jpg", ".jpeg" -> "image/jpeg";
            case ".bmp" -> "image/bmp";
            case ".tiff" -> "image/tiff";
            case ".wmf" -> "image/wmf";
            case ".jp2" -> "image/jp2";
            case ".svg" -> "image/svg+xml";
            default -> "application/octet-stream";
        };
    }
}
