package com.eshop.catalog.controller;

import com.eshop.catalog.service.CatalogService;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.core.io.ClassPathResource;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;

import java.io.IOException;

@Controller
public class PicController {

    private static final Logger log = LoggerFactory.getLogger(PicController.class);

    private final CatalogService catalogService;

    public PicController(CatalogService catalogService) {
        this.catalogService = catalogService;
    }

    @GetMapping("/items/{catalogItemId}/pic")
    public ResponseEntity<byte[]> getImage(@PathVariable int catalogItemId) {
        log.info("Now loading... /items/{}/pic", catalogItemId);

        if (catalogItemId <= 0) {
            return ResponseEntity.badRequest().build();
        }

        var item = catalogService.findCatalogItem(catalogItemId);
        if (item == null) {
            return ResponseEntity.notFound().build();
        }

        try {
            String fileName = item.getPictureFileName();
            if (fileName.contains("..") || fileName.contains("/") || fileName.contains("\\")) {
                return ResponseEntity.badRequest().build();
            }
            ClassPathResource resource = new ClassPathResource("static/pics/" + fileName);
            if (!resource.exists()) {
                return ResponseEntity.notFound().build();
            }

            byte[] imageBytes = resource.getContentAsByteArray();
            String extension = getFileExtension(item.getPictureFileName());
            String mimeType = getImageMimeType(extension);

            HttpHeaders headers = new HttpHeaders();
            headers.setContentType(MediaType.parseMediaType(mimeType));
            return new ResponseEntity<>(imageBytes, headers, HttpStatus.OK);
        } catch (IOException e) {
            log.error("Error reading image file for item {}", catalogItemId, e);
            return ResponseEntity.internalServerError().build();
        }
    }

    private String getFileExtension(String filename) {
        int dotIndex = filename.lastIndexOf('.');
        if (dotIndex < 0) {
            return "";
        }
        return filename.substring(dotIndex).toLowerCase();
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
