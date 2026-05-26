package com.eshop.catalog.controller;

import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.web.client.TestRestTemplate;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.util.LinkedMultiValueMap;
import org.springframework.util.MultiValueMap;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertTrue;

@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ActiveProfiles("mock")
class CatalogControllerIntegrationTest {

    @Autowired
    private TestRestTemplate restTemplate;

    @Test
    void indexPage_returnsOkWithHtml() {
        ResponseEntity<String> response = restTemplate.getForEntity("/", String.class);

        assertEquals(HttpStatus.OK, response.getStatusCode());
        assertNotNull(response.getBody());
        assertTrue(response.getBody().contains("html"));
    }

    @Test
    void indexPage_containsCatalogItems() {
        ResponseEntity<String> response = restTemplate.getForEntity("/", String.class);

        assertEquals(HttpStatus.OK, response.getStatusCode());
        assertNotNull(response.getBody());
        assertTrue(response.getBody().contains(".NET Bot Black Hoodie"));
    }

    @Test
    void indexPage_supportsPagination() {
        ResponseEntity<String> response =
                restTemplate.getForEntity("/?pageSize=5&pageIndex=0", String.class);

        assertEquals(HttpStatus.OK, response.getStatusCode());
        assertNotNull(response.getBody());
    }

    @Test
    void detailsPage_existingItem_returnsOk() {
        ResponseEntity<String> response =
                restTemplate.getForEntity("/catalog/details/1", String.class);

        assertEquals(HttpStatus.OK, response.getStatusCode());
        assertNotNull(response.getBody());
        assertTrue(response.getBody().contains(".NET Bot Black Hoodie"));
    }

    @Test
    void detailsPage_nonExistentItem_returns404() {
        ResponseEntity<String> response =
                restTemplate.getForEntity("/catalog/details/999", String.class);

        assertEquals(HttpStatus.NOT_FOUND, response.getStatusCode());
    }

    @Test
    void createForm_returnsOk() {
        ResponseEntity<String> response =
                restTemplate.getForEntity("/catalog/create", String.class);

        assertEquals(HttpStatus.OK, response.getStatusCode());
        assertNotNull(response.getBody());
    }

    @Test
    void createItem_validData_redirectsToIndex() {
        MultiValueMap<String, String> form = new LinkedMultiValueMap<>();
        form.add("name", "Test Item");
        form.add("description", "A test catalog item");
        form.add("price", "9.99");
        form.add("pictureFileName", "test.png");
        form.add("catalogTypeId", "1");
        form.add("catalogBrandId", "1");
        form.add("availableStock", "50");
        form.add("restockThreshold", "10");
        form.add("maxStockThreshold", "100");

        HttpHeaders headers = new HttpHeaders();
        headers.setContentType(MediaType.APPLICATION_FORM_URLENCODED);
        HttpEntity<MultiValueMap<String, String>> request = new HttpEntity<>(form, headers);

        ResponseEntity<String> response =
                restTemplate.postForEntity("/catalog/create", request, String.class);

        assertTrue(
                response.getStatusCode() == HttpStatus.OK
                        || response.getStatusCode() == HttpStatus.FOUND
                        || response.getStatusCode() == HttpStatus.SEE_OTHER,
                "Expected redirect or OK but got " + response.getStatusCode());
    }

    @Test
    void editForm_existingItem_returnsOk() {
        ResponseEntity<String> response =
                restTemplate.getForEntity("/catalog/edit/1", String.class);

        assertEquals(HttpStatus.OK, response.getStatusCode());
        assertNotNull(response.getBody());
        assertTrue(response.getBody().contains(".NET Bot Black Hoodie"));
    }

    @Test
    void editForm_nonExistentItem_returns404() {
        ResponseEntity<String> response =
                restTemplate.getForEntity("/catalog/edit/999", String.class);

        assertEquals(HttpStatus.NOT_FOUND, response.getStatusCode());
    }

    @Test
    void editItem_validData_redirectsToIndex() {
        MultiValueMap<String, String> form = new LinkedMultiValueMap<>();
        form.add("name", "Updated Sheet");
        form.add("description", "Updated description");
        form.add("price", "25.00");
        form.add("pictureFileName", "5.png");
        form.add("catalogTypeId", "3");
        form.add("catalogBrandId", "5");
        form.add("availableStock", "100");
        form.add("restockThreshold", "10");
        form.add("maxStockThreshold", "200");

        HttpHeaders headers = new HttpHeaders();
        headers.setContentType(MediaType.APPLICATION_FORM_URLENCODED);
        HttpEntity<MultiValueMap<String, String>> request = new HttpEntity<>(form, headers);

        ResponseEntity<String> response =
                restTemplate.postForEntity("/catalog/edit/5", request, String.class);

        assertTrue(
                response.getStatusCode() == HttpStatus.OK
                        || response.getStatusCode() == HttpStatus.FOUND
                        || response.getStatusCode() == HttpStatus.SEE_OTHER,
                "Expected redirect or OK but got " + response.getStatusCode());
    }

    @Test
    void deleteForm_existingItem_returnsOk() {
        ResponseEntity<String> response =
                restTemplate.getForEntity("/catalog/delete/2", String.class);

        assertEquals(HttpStatus.OK, response.getStatusCode());
        assertNotNull(response.getBody());
    }

    @Test
    void deleteForm_nonExistentItem_returns404() {
        ResponseEntity<String> response =
                restTemplate.getForEntity("/catalog/delete/999", String.class);

        assertEquals(HttpStatus.NOT_FOUND, response.getStatusCode());
    }

    @Test
    void deleteConfirmed_existingItem_redirectsToIndex() {
        MultiValueMap<String, String> form = new LinkedMultiValueMap<>();
        HttpHeaders headers = new HttpHeaders();
        headers.setContentType(MediaType.APPLICATION_FORM_URLENCODED);
        HttpEntity<MultiValueMap<String, String>> request = new HttpEntity<>(form, headers);

        ResponseEntity<String> response =
                restTemplate.postForEntity("/catalog/delete/12", request, String.class);

        assertTrue(
                response.getStatusCode() == HttpStatus.OK
                        || response.getStatusCode() == HttpStatus.FOUND
                        || response.getStatusCode() == HttpStatus.SEE_OTHER,
                "Expected redirect or OK but got " + response.getStatusCode());
    }

    @Test
    void deleteConfirmed_nonExistentItem_returns404() {
        MultiValueMap<String, String> form = new LinkedMultiValueMap<>();
        HttpHeaders headers = new HttpHeaders();
        headers.setContentType(MediaType.APPLICATION_FORM_URLENCODED);
        HttpEntity<MultiValueMap<String, String>> request = new HttpEntity<>(form, headers);

        ResponseEntity<String> response =
                restTemplate.postForEntity("/catalog/delete/999", request, String.class);

        assertEquals(HttpStatus.NOT_FOUND, response.getStatusCode());
    }
}
