package com.eshop.catalog.controller.api;

import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import java.util.List;

import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.WebMvcTest;
import org.springframework.test.context.bean.override.mockito.MockitoBean;
import org.springframework.security.test.context.support.WithMockUser;
import org.springframework.test.web.servlet.MockMvc;

import com.eshop.catalog.domain.entity.CatalogBrand;
import com.eshop.catalog.service.CatalogService;

@WebMvcTest(FilesRestController.class)
@WithMockUser
class FilesRestControllerTest {

    @Autowired
    private MockMvc mockMvc;

    @MockitoBean
    private CatalogService catalogService;

    @Test
    void getBrands_returnsBrandDtoList() throws Exception {
        CatalogBrand brand1 = new CatalogBrand("Nike");
        brand1.setId(1);
        CatalogBrand brand2 = new CatalogBrand("Adidas");
        brand2.setId(2);
        when(catalogService.getCatalogBrands()).thenReturn(List.of(brand1, brand2));

        mockMvc.perform(get("/api/files"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(2))
                .andExpect(jsonPath("$[0].id").value(1))
                .andExpect(jsonPath("$[0].brand").value("Nike"))
                .andExpect(jsonPath("$[1].id").value(2))
                .andExpect(jsonPath("$[1].brand").value("Adidas"));
    }

    @Test
    void getBrands_emptyList_returnsEmptyArray() throws Exception {
        when(catalogService.getCatalogBrands()).thenReturn(List.of());

        mockMvc.perform(get("/api/files"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(0));
    }
}
