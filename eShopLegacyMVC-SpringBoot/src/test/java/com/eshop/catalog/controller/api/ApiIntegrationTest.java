package com.eshop.catalog.controller.api;

import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.delete;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.http.MediaType;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.web.servlet.MockMvc;

@SpringBootTest
@AutoConfigureMockMvc
@ActiveProfiles("test")
class ApiIntegrationTest {

  @Autowired private MockMvc mockMvc;

  @Test
  void getApiIndex_returnsHelloMessage() throws Exception {
    mockMvc
        .perform(get("/api"))
        .andExpect(status().isOk())
        .andExpect(content().contentTypeCompatibleWith(MediaType.APPLICATION_JSON))
        .andExpect(jsonPath("$.message").value("Hello World!"));
  }

  @Test
  void getBrands_returnsBrandList() throws Exception {
    mockMvc
        .perform(get("/api/brands"))
        .andExpect(status().isOk())
        .andExpect(content().contentTypeCompatibleWith(MediaType.APPLICATION_JSON))
        .andExpect(jsonPath("$").isArray())
        .andExpect(jsonPath("$.length()").value(org.hamcrest.Matchers.greaterThan(0)));
  }

  @Test
  void getBrandById_existingBrand_returnsBrand() throws Exception {
    mockMvc
        .perform(get("/api/brands/1"))
        .andExpect(status().isOk())
        .andExpect(content().contentTypeCompatibleWith(MediaType.APPLICATION_JSON))
        .andExpect(jsonPath("$.id").value(1));
  }

  @Test
  void getBrandById_nonExistingBrand_returns404() throws Exception {
    mockMvc.perform(get("/api/brands/99999")).andExpect(status().isNotFound());
  }

  @Test
  void deleteBrand_existingBrand_returns200() throws Exception {
    mockMvc.perform(delete("/api/brands/1")).andExpect(status().isOk());
  }

  @Test
  void deleteBrand_nonExistingBrand_returns404() throws Exception {
    mockMvc.perform(delete("/api/brands/99999")).andExpect(status().isNotFound());
  }

  @Test
  void getFiles_returnsJsonBrandDtoList() throws Exception {
    mockMvc
        .perform(get("/api/files"))
        .andExpect(status().isOk())
        .andExpect(content().contentTypeCompatibleWith(MediaType.APPLICATION_JSON))
        .andExpect(jsonPath("$").isArray())
        .andExpect(jsonPath("$.length()").value(org.hamcrest.Matchers.greaterThan(0)))
        .andExpect(jsonPath("$[0].id").isNumber())
        .andExpect(jsonPath("$[0].brand").isString());
  }
}
