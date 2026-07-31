package com.eshop.catalog.controller.api;

import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.eshop.catalog.config.CatalogMetrics;
import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.service.CatalogService;
import java.util.List;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.WebMvcTest;
import org.springframework.http.MediaType;
import org.springframework.security.test.context.support.WithMockUser;
import org.springframework.test.context.bean.override.mockito.MockitoBean;
import org.springframework.test.web.servlet.MockMvc;

@WebMvcTest(FilesRestController.class)
@WithMockUser
class FilesRestControllerTest {

  @Autowired private MockMvc mockMvc;

  @MockitoBean private CatalogService catalogService;
  @MockitoBean private CatalogMetrics catalogMetrics;

  @Test
  void get_returnsJsonResponse() throws Exception {
    CatalogBrand brand = new CatalogBrand();
    brand.setId(1);
    brand.setBrand("Azure");
    when(catalogService.getCatalogBrands()).thenReturn(List.of(brand));

    mockMvc
        .perform(get("/api/files"))
        .andExpect(status().isOk())
        .andExpect(content().contentTypeCompatibleWith(MediaType.APPLICATION_JSON))
        .andExpect(jsonPath("$[0].id").value(1))
        .andExpect(jsonPath("$[0].brand").value("Azure"));
  }

  @Test
  void get_emptyBrands_returnsEmptyJsonArray() throws Exception {
    when(catalogService.getCatalogBrands()).thenReturn(List.of());

    mockMvc
        .perform(get("/api/files"))
        .andExpect(status().isOk())
        .andExpect(content().contentTypeCompatibleWith(MediaType.APPLICATION_JSON))
        .andExpect(content().json("[]"));
  }
}
