package com.eshop.catalog.controller;

import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.service.ICatalogService;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.autoconfigure.web.servlet.WebMvcTest;
import org.springframework.http.MediaType;
import org.springframework.test.context.bean.override.mockito.MockitoBean;
import org.springframework.test.web.servlet.MockMvc;

import java.util.List;

import static org.hamcrest.Matchers.hasSize;
import static org.hamcrest.Matchers.is;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@WebMvcTest(BrandsRestController.class)
@AutoConfigureMockMvc(addFilters = false)
class BrandsRestControllerTest {

    @Autowired
    private MockMvc mockMvc;

    @MockitoBean
    private ICatalogService catalogService;

    @Test
    void getAllBrandsReturns200WithJsonArray() throws Exception {
        CatalogBrand brand1 = createBrand(1, "Azure");
        CatalogBrand brand2 = createBrand(2, ".NET");
        when(catalogService.getCatalogBrands()).thenReturn(List.of(brand1, brand2));

        mockMvc.perform(get("/api/brands"))
                .andExpect(status().isOk())
                .andExpect(content().contentType(MediaType.APPLICATION_JSON))
                .andExpect(jsonPath("$", hasSize(2)))
                .andExpect(jsonPath("$[0].id", is(1)))
                .andExpect(jsonPath("$[0].brand", is("Azure")))
                .andExpect(jsonPath("$[1].id", is(2)))
                .andExpect(jsonPath("$[1].brand", is(".NET")));
    }

    @Test
    void getBrandByIdReturns200WithBrand() throws Exception {
        CatalogBrand brand1 = createBrand(1, "Azure");
        CatalogBrand brand2 = createBrand(2, ".NET");
        when(catalogService.getCatalogBrands()).thenReturn(List.of(brand1, brand2));

        mockMvc.perform(get("/api/brands/1"))
                .andExpect(status().isOk())
                .andExpect(content().contentType(MediaType.APPLICATION_JSON))
                .andExpect(jsonPath("$.id", is(1)))
                .andExpect(jsonPath("$.brand", is("Azure")));
    }

    @Test
    void getBrandByIdReturns404ForNonExistentBrand() throws Exception {
        when(catalogService.getCatalogBrands()).thenReturn(List.of(createBrand(1, "Azure")));

        mockMvc.perform(get("/api/brands/999"))
                .andExpect(status().isNotFound());
    }

    @Test
    void getAllBrandsReturnsEmptyArrayWhenNoBrands() throws Exception {
        when(catalogService.getCatalogBrands()).thenReturn(List.of());

        mockMvc.perform(get("/api/brands"))
                .andExpect(status().isOk())
                .andExpect(content().contentType(MediaType.APPLICATION_JSON))
                .andExpect(jsonPath("$", hasSize(0)));
    }

    private CatalogBrand createBrand(int id, String brand) {
        CatalogBrand b = new CatalogBrand();
        b.setId(id);
        b.setBrand(brand);
        return b;
    }
}
