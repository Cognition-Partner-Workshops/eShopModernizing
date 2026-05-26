package com.eshop.catalog.controller.api;

import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.eshop.catalog.config.CatalogMetrics;
import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.service.CatalogService;
import com.eshop.catalog.util.JsonSerializationUtil;
import java.util.List;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.WebMvcTest;
import org.springframework.http.MediaType;
import org.springframework.security.test.context.support.WithMockUser;
import org.springframework.test.context.bean.override.mockito.MockitoBean;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.MvcResult;

@WebMvcTest(FilesRestController.class)
@WithMockUser
class FilesRestControllerTest {

  @Autowired private MockMvc mockMvc;

  @MockitoBean private CatalogService catalogService;
  @MockitoBean private CatalogMetrics catalogMetrics;
  @MockitoBean private JsonSerializationUtil jsonSerializationUtil;

  @Test
  void get_returnsJsonResponse() throws Exception {
    CatalogBrand brand = new CatalogBrand();
    brand.setId(1);
    brand.setBrand("Azure");
    List<CatalogBrand> brands = List.of(brand);
    byte[] jsonBytes = "[{\"id\":1,\"brand\":\"Azure\"}]".getBytes();

    when(catalogService.getCatalogBrands()).thenReturn(brands);
    when(jsonSerializationUtil.serializeToBytes(brands)).thenReturn(jsonBytes);

    mockMvc
        .perform(get("/api/files"))
        .andExpect(status().isOk())
        .andExpect(content().contentTypeCompatibleWith(MediaType.APPLICATION_JSON))
        .andExpect(content().bytes(jsonBytes));
  }

  @Test
  void get_emptyBrands_returnsEmptyJsonArray() throws Exception {
    List<CatalogBrand> brands = List.of();
    byte[] jsonBytes = "[]".getBytes();

    when(catalogService.getCatalogBrands()).thenReturn(brands);
    when(jsonSerializationUtil.serializeToBytes(brands)).thenReturn(jsonBytes);

    MvcResult result =
        mockMvc
            .perform(get("/api/files"))
            .andExpect(status().isOk())
            .andExpect(content().contentTypeCompatibleWith(MediaType.APPLICATION_JSON))
            .andReturn();

    org.assertj.core.api.Assertions.assertThat(result.getResponse().getContentAsString())
        .isEqualTo("[]");
  }
}
