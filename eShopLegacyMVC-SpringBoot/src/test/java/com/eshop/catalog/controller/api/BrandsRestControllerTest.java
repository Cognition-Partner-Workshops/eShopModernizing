package com.eshop.catalog.controller.api;

import static org.mockito.Mockito.when;
import static org.springframework.security.test.web.servlet.request.SecurityMockMvcRequestPostProcessors.csrf;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.delete;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import java.util.List;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.WebMvcTest;
import org.springframework.test.context.bean.override.mockito.MockitoBean;
import org.springframework.security.test.context.support.WithMockUser;
import org.springframework.test.web.servlet.MockMvc;

import com.eshop.catalog.domain.entity.CatalogBrand;
import com.eshop.catalog.service.CatalogService;

@WebMvcTest(BrandsRestController.class)
@WithMockUser
class BrandsRestControllerTest {

    @Autowired
    private MockMvc mockMvc;

    @MockitoBean
    private CatalogService catalogService;

    private List<CatalogBrand> brands;

    @BeforeEach
    void setUp() {
        CatalogBrand brand1 = new CatalogBrand("Nike");
        brand1.setId(1);
        CatalogBrand brand2 = new CatalogBrand("Adidas");
        brand2.setId(2);
        brands = List.of(brand1, brand2);

        when(catalogService.getCatalogBrands()).thenReturn(brands);
    }

    @Test
    void getAllBrands_returnsBrandList() throws Exception {
        mockMvc.perform(get("/api/brands"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(2))
                .andExpect(jsonPath("$[0].id").value(1))
                .andExpect(jsonPath("$[0].brand").value("Nike"))
                .andExpect(jsonPath("$[1].id").value(2))
                .andExpect(jsonPath("$[1].brand").value("Adidas"));
    }

    @Test
    void getBrandById_existingBrand_returnsBrand() throws Exception {
        mockMvc.perform(get("/api/brands/1"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.id").value(1))
                .andExpect(jsonPath("$.brand").value("Nike"));
    }

    @Test
    void getBrandById_nonExistingBrand_returns404() throws Exception {
        mockMvc.perform(get("/api/brands/999"))
                .andExpect(status().isNotFound());
    }

    @Test
    void deleteBrand_existingBrand_returns200() throws Exception {
        mockMvc.perform(delete("/api/brands/1").with(csrf()))
                .andExpect(status().isOk());
    }

    @Test
    void deleteBrand_nonExistingBrand_returns404() throws Exception {
        mockMvc.perform(delete("/api/brands/999").with(csrf()))
                .andExpect(status().isNotFound());
    }

}
