package com.eshop.catalog.controller.api;

import com.eshop.catalog.domain.entity.CatalogBrand;

import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.web.client.TestRestTemplate;
import org.springframework.core.ParameterizedTypeReference;
import org.springframework.http.HttpMethod;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.test.context.ActiveProfiles;

import java.util.List;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertTrue;

@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ActiveProfiles("mock")
class BrandsRestIntegrationTest {

    @Autowired
    private TestRestTemplate restTemplate;

    @Test
    void getAllBrands_returnsNonEmptyList() {
        ResponseEntity<List<CatalogBrand>> response = restTemplate.exchange(
                "/api/brands",
                HttpMethod.GET,
                null,
                new ParameterizedTypeReference<>() {});

        assertEquals(HttpStatus.OK, response.getStatusCode());
        assertNotNull(response.getBody());
        assertFalse(response.getBody().isEmpty());
        assertEquals(5, response.getBody().size());
    }

    @Test
    void getAllBrands_containsExpectedBrand() {
        ResponseEntity<List<CatalogBrand>> response = restTemplate.exchange(
                "/api/brands",
                HttpMethod.GET,
                null,
                new ParameterizedTypeReference<>() {});

        List<CatalogBrand> brands = response.getBody();
        assertNotNull(brands);
        assertTrue(brands.stream().anyMatch(b -> "Azure".equals(b.getBrand())));
    }

    @Test
    void getBrandById_existingId_returnsBrand() {
        ResponseEntity<CatalogBrand> response =
                restTemplate.getForEntity("/api/brands/1", CatalogBrand.class);

        assertEquals(HttpStatus.OK, response.getStatusCode());
        assertNotNull(response.getBody());
        assertEquals(1, response.getBody().getId());
        assertEquals("Azure", response.getBody().getBrand());
    }

    @Test
    void getBrandById_nonExistentId_returns404() {
        ResponseEntity<CatalogBrand> response =
                restTemplate.getForEntity("/api/brands/999", CatalogBrand.class);

        assertEquals(HttpStatus.NOT_FOUND, response.getStatusCode());
    }
}
