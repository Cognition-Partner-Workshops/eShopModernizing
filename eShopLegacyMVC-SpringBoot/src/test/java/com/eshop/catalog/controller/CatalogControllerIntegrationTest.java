package com.eshop.catalog.controller;

import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.web.client.TestRestTemplate;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.util.LinkedMultiValueMap;
import org.springframework.util.MultiValueMap;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.springframework.security.test.web.servlet.request.SecurityMockMvcRequestPostProcessors.csrf;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@AutoConfigureMockMvc
@ActiveProfiles("mock")
class CatalogControllerIntegrationTest {

    @Autowired
    private TestRestTemplate restTemplate;

    @Autowired
    private MockMvc mockMvc;

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
    void createItem_validData_returnsSuccessOrRedirect() throws Exception {
        mockMvc.perform(post("/catalog/create")
                        .with(csrf())
                        .contentType(MediaType.APPLICATION_FORM_URLENCODED)
                        .param("name", "Test Item")
                        .param("description", "A test catalog item")
                        .param("price", "9.99")
                        .param("pictureFileName", "test.png")
                        .param("catalogTypeId", "1")
                        .param("catalogBrandId", "1")
                        .param("availableStock", "50")
                        .param("restockThreshold", "10")
                        .param("maxStockThreshold", "100"))
                .andExpect(result -> {
                    int status = result.getResponse().getStatus();
                    assertTrue(status == 200 || status == 302 || status == 303,
                            "Expected 200, 302, or 303 but got " + status);
                });
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
    void editItem_validData_returnsSuccessOrRedirect() throws Exception {
        mockMvc.perform(post("/catalog/edit/5")
                        .with(csrf())
                        .contentType(MediaType.APPLICATION_FORM_URLENCODED)
                        .param("name", "Updated Sheet")
                        .param("description", "Updated description")
                        .param("price", "25.00")
                        .param("pictureFileName", "5.png")
                        .param("catalogTypeId", "3")
                        .param("catalogBrandId", "5")
                        .param("availableStock", "100")
                        .param("restockThreshold", "10")
                        .param("maxStockThreshold", "200"))
                .andExpect(result -> {
                    int status = result.getResponse().getStatus();
                    assertTrue(status == 200 || status == 302 || status == 303,
                            "Expected 200, 302, or 303 but got " + status);
                });
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
    void deleteConfirmed_existingItem_redirectsToIndex() throws Exception {
        mockMvc.perform(post("/catalog/delete/12")
                        .with(csrf())
                        .contentType(MediaType.APPLICATION_FORM_URLENCODED))
                .andExpect(status().is3xxRedirection());
    }

    @Test
    void deleteConfirmed_nonExistentItem_returns404() throws Exception {
        mockMvc.perform(post("/catalog/delete/999")
                        .with(csrf())
                        .contentType(MediaType.APPLICATION_FORM_URLENCODED))
                .andExpect(status().isNotFound());
    }
}
