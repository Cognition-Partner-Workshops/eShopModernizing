package com.eshop.catalog.controller.api;

import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.delete;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.eshop.catalog.config.SecurityConfig;
import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.service.CatalogService;
import java.util.Collections;
import java.util.List;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.WebMvcTest;
import org.springframework.context.annotation.Import;
import org.springframework.http.MediaType;
import org.springframework.security.test.context.support.WithMockUser;
import org.springframework.test.context.bean.override.mockito.MockitoBean;
import org.springframework.test.web.servlet.MockMvc;

@WebMvcTest(BrandsRestController.class)
@Import(SecurityConfig.class)
@WithMockUser
class BrandsRestControllerTest {

  @Autowired private MockMvc mockMvc;

  @MockitoBean private CatalogService catalogService;

  private CatalogBrand brand1;
  private CatalogBrand brand2;

  @BeforeEach
  void setUp() {
    brand1 = new CatalogBrand();
    brand1.setId(1);
    brand1.setBrand("Azure");

    brand2 = new CatalogBrand();
    brand2.setId(2);
    brand2.setBrand(".NET");
  }

  @Test
  void getAll_returnsBrandListAsJson() throws Exception {
    when(catalogService.getCatalogBrands()).thenReturn(List.of(brand1, brand2));

    mockMvc
        .perform(get("/api/brands"))
        .andExpect(status().isOk())
        .andExpect(content().contentTypeCompatibleWith(MediaType.APPLICATION_JSON))
        .andExpect(jsonPath("$").isArray())
        .andExpect(jsonPath("$.length()").value(2))
        .andExpect(jsonPath("$[0].id").value(1))
        .andExpect(jsonPath("$[0].brand").value("Azure"))
        .andExpect(jsonPath("$[1].id").value(2))
        .andExpect(jsonPath("$[1].brand").value(".NET"));
  }

  @Test
  void getAll_emptyList_returnsEmptyArray() throws Exception {
    when(catalogService.getCatalogBrands()).thenReturn(Collections.emptyList());

    mockMvc
        .perform(get("/api/brands"))
        .andExpect(status().isOk())
        .andExpect(content().contentTypeCompatibleWith(MediaType.APPLICATION_JSON))
        .andExpect(jsonPath("$").isArray())
        .andExpect(jsonPath("$.length()").value(0));
  }

  @Test
  void getById_existingBrand_returnsBrand() throws Exception {
    when(catalogService.getCatalogBrands()).thenReturn(List.of(brand1, brand2));

    mockMvc
        .perform(get("/api/brands/1"))
        .andExpect(status().isOk())
        .andExpect(content().contentTypeCompatibleWith(MediaType.APPLICATION_JSON))
        .andExpect(jsonPath("$.id").value(1))
        .andExpect(jsonPath("$.brand").value("Azure"));
  }

  @Test
  void getById_nonExistingBrand_returns404() throws Exception {
    when(catalogService.getCatalogBrands()).thenReturn(List.of(brand1, brand2));

    mockMvc.perform(get("/api/brands/999")).andExpect(status().isNotFound());
  }

  @Test
  void delete_existingBrand_returns200() throws Exception {
    when(catalogService.getCatalogBrands()).thenReturn(List.of(brand1, brand2));

    mockMvc.perform(delete("/api/brands/1")).andExpect(status().isOk());
  }

  @Test
  void delete_nonExistingBrand_returns404() throws Exception {
    when(catalogService.getCatalogBrands()).thenReturn(List.of(brand1, brand2));

    mockMvc.perform(delete("/api/brands/999")).andExpect(status().isNotFound());
  }
}
