package com.eshop.catalog.config;

import com.eshop.catalog.controller.CatalogController;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.service.CatalogService;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.WebMvcTest;
import org.springframework.context.annotation.Import;
import org.springframework.http.MediaType;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.context.bean.override.mockito.MockitoBean;
import org.springframework.test.web.servlet.MockMvc;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@WebMvcTest(CatalogController.class)
@ActiveProfiles("test")
@Import(SecurityConfig.class)
class SecurityConfigTest {

    @Autowired
    private MockMvc mockMvc;

    @MockitoBean
    private CatalogService catalogService;

    @Test
    void apiGetEndpointsAreAccessibleWithoutAuthentication() throws Exception {
        mockMvc.perform(get("/api/catalog/items"))
            .andExpect(status().isOk());
    }

    @Test
    void apiPostEndpointsWorkWithoutCsrf() throws Exception {
        CatalogItem item = new CatalogItem();
        item.setId(1);
        item.setName("test");
        when(catalogService.createCatalogItem(any(CatalogItem.class))).thenReturn(item);

        mockMvc.perform(post("/api/catalog/items")
                .contentType(MediaType.APPLICATION_JSON)
                .content("{\"name\":\"test\",\"price\":10.0}"))
            .andExpect(status().isCreated());
    }
}
