package com.eshop.catalog;

import static org.assertj.core.api.Assertions.assertThat;

import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.web.client.TestRestTemplate;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.test.context.ActiveProfiles;

@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ActiveProfiles("mock")
class SmokeTest {

    @Autowired
    private TestRestTemplate restTemplate;

    @Test
    void catalogIndexReturnsOk() {
        ResponseEntity<String> response = restTemplate.getForEntity("/", String.class);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
    }

    @Test
    void apiRootReturnsOk() {
        ResponseEntity<String> response = restTemplate.getForEntity("/api", String.class);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
        assertThat(response.getBody()).contains("Hello World!");
    }

    @Test
    void apiBrandsReturnsOk() {
        ResponseEntity<String> response = restTemplate.getForEntity("/api/brands", String.class);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
        assertThat(response.getBody()).isNotEmpty();
    }

    @Test
    void apiFilesReturnsOk() {
        ResponseEntity<String> response = restTemplate.getForEntity("/api/files", String.class);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
        assertThat(response.getBody()).isNotEmpty();
    }

    @Test
    void catalogDetailsReturnsOk() {
        ResponseEntity<String> response = restTemplate.getForEntity("/catalog/details/1", String.class);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
    }

    @Test
    void catalogCreateFormReturnsOk() {
        ResponseEntity<String> response = restTemplate.getForEntity("/catalog/create", String.class);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
    }

    @Test
    void catalogEditFormReturnsOk() {
        ResponseEntity<String> response = restTemplate.getForEntity("/catalog/edit/1", String.class);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
    }

    @Test
    void catalogDeleteFormReturnsOk() {
        ResponseEntity<String> response = restTemplate.getForEntity("/catalog/delete/1", String.class);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
    }

    @Test
    void itemPicEndpointResponds() {
        ResponseEntity<byte[]> response = restTemplate.getForEntity("/items/1/pic", byte[].class);

        assertThat(response.getStatusCode().is2xxSuccessful() ||
                   response.getStatusCode().equals(HttpStatus.NOT_FOUND)).isTrue();
    }
}
