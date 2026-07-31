package com.eshop.catalog.controller.api;

import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.eshop.catalog.config.CatalogMetrics;
import com.eshop.catalog.service.CatalogService;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.WebMvcTest;
import org.springframework.http.MediaType;
import org.springframework.security.test.context.support.WithMockUser;
import org.springframework.test.context.bean.override.mockito.MockitoBean;
import org.springframework.test.web.servlet.MockMvc;

@WebMvcTest(CatalogApiController.class)
@WithMockUser
class CatalogApiControllerTest {

  @Autowired private MockMvc mockMvc;

  @MockitoBean private CatalogService catalogService;
  @MockitoBean private CatalogMetrics catalogMetrics;

  @Test
  void index_returnsHelloWorldMessage() throws Exception {
    mockMvc
        .perform(get("/api"))
        .andExpect(status().isOk())
        .andExpect(content().contentTypeCompatibleWith(MediaType.APPLICATION_JSON))
        .andExpect(jsonPath("$.message").value("Hello World!"));
  }

  @Test
  void index_returnsJsonContentType() throws Exception {
    mockMvc
        .perform(get("/api"))
        .andExpect(status().isOk())
        .andExpect(content().contentTypeCompatibleWith(MediaType.APPLICATION_JSON));
  }
}
