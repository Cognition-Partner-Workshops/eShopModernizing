package com.eshop.catalog;

import static org.assertj.core.api.Assertions.assertThat;

import java.util.Map;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.web.client.TestRestTemplate;
import org.springframework.http.HttpMethod;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.test.context.ActiveProfiles;

@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ActiveProfiles("test")
class EndpointSmokeIT {

  @Autowired private TestRestTemplate restTemplate;

  @Test
  void healthEndpoint_returnsUpStatus() {
    ResponseEntity<Map> response = restTemplate.getForEntity("/actuator/health", Map.class);

    assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
    assertThat(response.getBody()).isNotNull();
    assertThat(response.getBody()).containsEntry("status", "UP");
  }

  @Test
  void homepage_returns2xx() {
    ResponseEntity<String> response = restTemplate.getForEntity("/", String.class);

    assertThat(response.getStatusCode().is2xxSuccessful()).isTrue();
  }

  @Test
  void apiIndex_returns2xx() {
    ResponseEntity<String> response = restTemplate.getForEntity("/api", String.class);

    assertThat(response.getStatusCode().is2xxSuccessful()).isTrue();
  }

  @Test
  void apiBrands_returns2xx() {
    ResponseEntity<String> response = restTemplate.getForEntity("/api/brands", String.class);

    assertThat(response.getStatusCode().is2xxSuccessful()).isTrue();
  }

  @Test
  void apiBrandsById_returns2xx() {
    ResponseEntity<String> response = restTemplate.getForEntity("/api/brands/1", String.class);

    assertThat(response.getStatusCode().is2xxSuccessful()).isTrue();
  }

  @Test
  void apiFiles_returns2xx() {
    ResponseEntity<String> response = restTemplate.getForEntity("/api/files", String.class);

    assertThat(response.getStatusCode().is2xxSuccessful()).isTrue();
  }

  @Test
  void catalogDetails_returns2xx() {
    ResponseEntity<String> response = restTemplate.getForEntity("/catalog/details/1", String.class);

    assertThat(response.getStatusCode().is2xxSuccessful()).isTrue();
  }

  @Test
  void catalogCreate_returns2xx() {
    ResponseEntity<String> response = restTemplate.getForEntity("/catalog/create", String.class);

    assertThat(response.getStatusCode().is2xxSuccessful()).isTrue();
  }

  @Test
  void catalogEdit_returns2xx() {
    ResponseEntity<String> response = restTemplate.getForEntity("/catalog/edit/1", String.class);

    assertThat(response.getStatusCode().is2xxSuccessful()).isTrue();
  }

  @Test
  void catalogDelete_returns2xx() {
    ResponseEntity<String> response = restTemplate.getForEntity("/catalog/delete/1", String.class);

    assertThat(response.getStatusCode().is2xxSuccessful()).isTrue();
  }

  @Test
  void itemPic_returns2xx() {
    ResponseEntity<byte[]> response = restTemplate.getForEntity("/items/1/pic", byte[].class);

    assertThat(response.getStatusCode().is2xxSuccessful()).isTrue();
  }

  @Test
  void deleteBrandEndpoint_returns2xx() {
    ResponseEntity<Void> response =
        restTemplate.exchange("/api/brands/1", HttpMethod.DELETE, null, Void.class);

    assertThat(response.getStatusCode().is2xxSuccessful()).isTrue();
  }

  @Test
  void staticCss_returns2xx() {
    ResponseEntity<String> response =
        restTemplate.getForEntity("/css/bootstrap.min.css", String.class);

    assertThat(response.getStatusCode().is2xxSuccessful()).isTrue();
  }

  @Test
  void favicon_returns2xx() {
    ResponseEntity<byte[]> response = restTemplate.getForEntity("/favicon.ico", byte[].class);

    assertThat(response.getStatusCode().is2xxSuccessful()).isTrue();
  }
}
