package com.eshop.catalog.controller.api;

import static org.assertj.core.api.Assertions.assertThat;

import com.eshop.catalog.model.CatalogBrand;
import java.util.Map;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.web.client.TestRestTemplate;
import org.springframework.http.HttpMethod;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.test.context.ActiveProfiles;

@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ActiveProfiles("test")
class RestApiIntegrationTest {

  @Autowired private TestRestTemplate restTemplate;

  @Test
  void getApiIndex_returns200WithHelloMessage() {
    ResponseEntity<Map> response = restTemplate.getForEntity("/api", Map.class);

    assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
    assertThat(response.getHeaders().getContentType()).isNotNull();
    assertThat(response.getHeaders().getContentType().isCompatibleWith(MediaType.APPLICATION_JSON))
        .isTrue();
    assertThat(response.getBody()).containsEntry("message", "Hello World!");
  }

  @Test
  void getBrands_returns200WithBrandArray() {
    ResponseEntity<CatalogBrand[]> response =
        restTemplate.getForEntity("/api/brands", CatalogBrand[].class);

    assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
    assertThat(response.getHeaders().getContentType()).isNotNull();
    assertThat(response.getHeaders().getContentType().isCompatibleWith(MediaType.APPLICATION_JSON))
        .isTrue();
    assertThat(response.getBody()).isNotNull();
    assertThat(response.getBody().length).isGreaterThan(0);
  }

  @Test
  void getBrandById_existingBrand_returns200() {
    ResponseEntity<CatalogBrand> response =
        restTemplate.getForEntity("/api/brands/1", CatalogBrand.class);

    assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
    assertThat(response.getHeaders().getContentType()).isNotNull();
    assertThat(response.getHeaders().getContentType().isCompatibleWith(MediaType.APPLICATION_JSON))
        .isTrue();
    assertThat(response.getBody()).isNotNull();
    assertThat(response.getBody().getId()).isEqualTo(1);
  }

  @Test
  void getBrandById_nonExistingBrand_returns404() {
    ResponseEntity<String> response = restTemplate.getForEntity("/api/brands/99999", String.class);

    assertThat(response.getStatusCode()).isEqualTo(HttpStatus.NOT_FOUND);
  }

  @Test
  void deleteBrand_existingBrand_returns200() {
    ResponseEntity<Void> response =
        restTemplate.exchange("/api/brands/1", HttpMethod.DELETE, null, Void.class);

    assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
  }

  @Test
  void deleteBrand_nonExistingBrand_returns404() {
    ResponseEntity<Void> response =
        restTemplate.exchange("/api/brands/99999", HttpMethod.DELETE, null, Void.class);

    assertThat(response.getStatusCode()).isEqualTo(HttpStatus.NOT_FOUND);
  }

  @Test
  void getFiles_returns200WithJsonContent() {
    ResponseEntity<String> response = restTemplate.getForEntity("/api/files", String.class);

    assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
    assertThat(response.getHeaders().getContentType()).isNotNull();
    assertThat(response.getHeaders().getContentType().isCompatibleWith(MediaType.APPLICATION_JSON))
        .isTrue();
    assertThat(response.getBody()).isNotNull();
    assertThat(response.getBody()).startsWith("[");
  }

  @Test
  void catalogHomepage_returns200() {
    ResponseEntity<String> response = restTemplate.getForEntity("/", String.class);

    assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
    assertThat(response.getBody()).isNotNull();
    assertThat(response.getBody()).contains("html");
  }

  @Test
  void catalogDetails_existingItem_returns200() {
    ResponseEntity<String> response = restTemplate.getForEntity("/catalog/details/1", String.class);

    assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
  }

  @Test
  void catalogCreateForm_returns200() {
    ResponseEntity<String> response = restTemplate.getForEntity("/catalog/create", String.class);

    assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
  }
}
