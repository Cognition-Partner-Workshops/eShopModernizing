package com.eshop.catalog.controller.api;

import com.eshop.catalog.dto.BrandDto;

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
class FilesRestIntegrationTest {

    @Autowired
    private TestRestTemplate restTemplate;

    @Test
    void getFiles_returnsOkWithBrands() {
        ResponseEntity<List<BrandDto>> response = restTemplate.exchange(
                "/api/files",
                HttpMethod.GET,
                null,
                new ParameterizedTypeReference<>() {});

        assertEquals(HttpStatus.OK, response.getStatusCode());
        assertNotNull(response.getBody());
        assertFalse(response.getBody().isEmpty());
        assertEquals(5, response.getBody().size());
    }

    @Test
    void getFiles_containsExpectedBrandDto() {
        ResponseEntity<List<BrandDto>> response = restTemplate.exchange(
                "/api/files",
                HttpMethod.GET,
                null,
                new ParameterizedTypeReference<>() {});

        List<BrandDto> brands = response.getBody();
        assertNotNull(brands);
        assertTrue(brands.stream().anyMatch(b -> "Azure".equals(b.brand()) && b.id() == 1));
    }

    @Test
    void getFiles_brandDtoHasCorrectFields() {
        ResponseEntity<List<BrandDto>> response = restTemplate.exchange(
                "/api/files",
                HttpMethod.GET,
                null,
                new ParameterizedTypeReference<>() {});

        List<BrandDto> brands = response.getBody();
        assertNotNull(brands);

        BrandDto first = brands.stream()
                .filter(b -> b.id() == 1)
                .findFirst()
                .orElse(null);
        assertNotNull(first);
        assertEquals("Azure", first.brand());
    }
}
