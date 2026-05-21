package com.eshop.catalog.controller;

import com.eshop.catalog.service.ICatalogService;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.core.io.Resource;
import org.springframework.core.io.ResourceLoader;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;

import java.io.IOException;

@Controller
public class PicController {

    private static final Logger log = LoggerFactory.getLogger(PicController.class);

    private final ICatalogService service;
    private final ResourceLoader resourceLoader;

    @Value("${app.pics-path:classpath:static/Pics}")
    private String picsPath;

    public PicController(ICatalogService service, ResourceLoader resourceLoader) {
        this.service = service;
        this.resourceLoader = resourceLoader;
    }

    @GetMapping("/items/{catalogItemId}/pic")
    public ResponseEntity<byte[]> index(@PathVariable int catalogItemId) throws IOException {
        log.info("Now loading... /items/{}/pic", catalogItemId);

        if (catalogItemId <= 0) {
            return ResponseEntity.badRequest().build();
        }

        var item = service.findCatalogItem(catalogItemId);
        if (item == null) {
            return ResponseEntity.notFound().build();
        }

        String fileName = item.getPictureFileName();
        String resourcePath = picsPath.endsWith("/")
                ? picsPath + fileName
                : picsPath + "/" + fileName;

        Resource resource = resourceLoader.getResource(resourcePath);
        if (!resource.exists()) {
            return ResponseEntity.notFound().build();
        }

        byte[] buffer = resource.getInputStream().readAllBytes();
        String extension = getFileExtension(fileName);
        String mimeType = getMimeTypeFromExtension(extension);

        return ResponseEntity.ok()
                .contentType(MediaType.parseMediaType(mimeType))
                .body(buffer);
    }

    private String getFileExtension(String fileName) {
        int lastDot = fileName.lastIndexOf('.');
        if (lastDot < 0) {
            return "";
        }
        return fileName.substring(lastDot).toLowerCase();
    }

    private String getMimeTypeFromExtension(String extension) {
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
