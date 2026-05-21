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

@WebMvcTest(FilesRestController.class)
@AutoConfigureMockMvc(addFilters = false)
class FilesRestControllerTest {

    @Autowired
    private MockMvc mockMvc;

    @MockitoBean
    private ICatalogService catalogService;

    @Test
    void getFilesReturns200WithJsonArrayOfBrandDTOs() throws Exception {
        CatalogBrand brand1 = createBrand(1, "Azure");
        CatalogBrand brand2 = createBrand(2, ".NET");
        CatalogBrand brand3 = createBrand(3, "Visual Studio");
        when(catalogService.getCatalogBrands()).thenReturn(List.of(brand1, brand2, brand3));

        mockMvc.perform(get("/api/files"))
                .andExpect(status().isOk())
                .andExpect(content().contentType(MediaType.APPLICATION_JSON))
                .andExpect(jsonPath("$", hasSize(3)))
                .andExpect(jsonPath("$[0].id", is(1)))
                .andExpect(jsonPath("$[0].brand", is("Azure")))
                .andExpect(jsonPath("$[1].id", is(2)))
                .andExpect(jsonPath("$[1].brand", is(".NET")))
                .andExpect(jsonPath("$[2].id", is(3)))
                .andExpect(jsonPath("$[2].brand", is("Visual Studio")));
    }

    @Test
    void getFilesReturnsEmptyArrayWhenNoBrands() throws Exception {
        when(catalogService.getCatalogBrands()).thenReturn(List.of());

        mockMvc.perform(get("/api/files"))
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
