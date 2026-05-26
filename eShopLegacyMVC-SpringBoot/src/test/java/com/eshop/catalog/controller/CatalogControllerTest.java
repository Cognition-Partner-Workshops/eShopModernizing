package com.eshop.catalog.controller;

import static org.hamcrest.Matchers.instanceOf;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;
import static org.springframework.security.test.web.servlet.request.SecurityMockMvcRequestPostProcessors.csrf;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.model;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.redirectedUrl;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.view;

import java.math.BigDecimal;
import java.util.List;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.WebMvcTest;
import org.springframework.test.context.bean.override.mockito.MockitoBean;
import org.springframework.security.test.context.support.WithMockUser;
import org.springframework.test.web.servlet.MockMvc;

import com.eshop.catalog.domain.entity.CatalogBrand;
import com.eshop.catalog.domain.entity.CatalogItem;
import com.eshop.catalog.domain.entity.CatalogType;
import com.eshop.catalog.dto.PaginatedItemsDto;
import com.eshop.catalog.service.CatalogService;

@WebMvcTest(CatalogController.class)
@WithMockUser
class CatalogControllerTest {

    @Autowired
    private MockMvc mockMvc;

    @MockitoBean
    private CatalogService catalogService;

    private CatalogItem sampleItem;
    private List<CatalogBrand> brands;
    private List<CatalogType> types;

    @BeforeEach
    void setUp() {
        CatalogBrand brand = new CatalogBrand("TestBrand");
        brand.setId(1);
        CatalogType type = new CatalogType("TestType");
        type.setId(1);

        sampleItem = new CatalogItem();
        sampleItem.setId(1);
        sampleItem.setName("Test Item");
        sampleItem.setPrice(new BigDecimal("19.99"));
        sampleItem.setCatalogBrand(brand);
        sampleItem.setCatalogType(type);

        brands = List.of(brand);
        types = List.of(type);

        when(catalogService.getCatalogBrands()).thenReturn(brands);
        when(catalogService.getCatalogTypes()).thenReturn(types);
    }

    // --- Index ---

    @Test
    void index_returnsIndexViewWithPaginatedItems() throws Exception {
        PaginatedItemsDto<CatalogItem> page =
                new PaginatedItemsDto<>(0, 10, 1, List.of(sampleItem));
        when(catalogService.getCatalogItemsPaginated(10, 0)).thenReturn(page);

        mockMvc.perform(get("/"))
                .andExpect(status().isOk())
                .andExpect(view().name("catalog/index"))
                .andExpect(model().attributeExists("paginatedItems"));
    }

    @Test
    void index_withCustomPagination_passesParameters() throws Exception {
        PaginatedItemsDto<CatalogItem> page =
                new PaginatedItemsDto<>(2, 5, 20, List.of(sampleItem));
        when(catalogService.getCatalogItemsPaginated(5, 2)).thenReturn(page);

        mockMvc.perform(get("/").param("pageSize", "5").param("pageIndex", "2"))
                .andExpect(status().isOk())
                .andExpect(view().name("catalog/index"))
                .andExpect(model().attributeExists("paginatedItems"));

        verify(catalogService).getCatalogItemsPaginated(5, 2);
    }

    // --- Details ---

    @Test
    void details_existingItem_returnsDetailsView() throws Exception {
        when(catalogService.findCatalogItem(1)).thenReturn(sampleItem);

        mockMvc.perform(get("/catalog/details/1"))
                .andExpect(status().isOk())
                .andExpect(view().name("catalog/details"))
                .andExpect(model().attributeExists("catalogItem"));
    }

    @Test
    void details_nonExistingItem_returns404() throws Exception {
        when(catalogService.findCatalogItem(999)).thenReturn(null);

        mockMvc.perform(get("/catalog/details/999"))
                .andExpect(status().isNotFound());
    }

    // --- Create ---

    @Test
    void createForm_returnsCreateViewWithEmptyItemAndDropdowns() throws Exception {
        mockMvc.perform(get("/catalog/create"))
                .andExpect(status().isOk())
                .andExpect(view().name("catalog/create"))
                .andExpect(model().attribute("catalogItem", instanceOf(CatalogItem.class)))
                .andExpect(model().attributeExists("brands"))
                .andExpect(model().attributeExists("types"));
    }

    @Test
    void create_validItem_redirectsToIndex() throws Exception {
        mockMvc.perform(post("/catalog/create")
                        .with(csrf())
                        .param("name", "New Item")
                        .param("price", "29.99")
                        .param("catalogBrand", "1")
                        .param("catalogType", "1")
                        .param("pictureFileName", "test.png"))
                .andExpect(status().is3xxRedirection())
                .andExpect(redirectedUrl("/"));

        verify(catalogService).createCatalogItem(any(CatalogItem.class));
    }

    @Test
    void create_invalidItem_returnsCreateViewWithErrors() throws Exception {
        mockMvc.perform(post("/catalog/create")
                        .with(csrf())
                        .param("name", "")
                        .param("price", ""))
                .andExpect(status().isOk())
                .andExpect(view().name("catalog/create"))
                .andExpect(model().attributeHasErrors("catalogItem"))
                .andExpect(model().attributeExists("brands"))
                .andExpect(model().attributeExists("types"));

        verify(catalogService, never()).createCatalogItem(any());
    }

    // --- Edit ---

    @Test
    void editForm_existingItem_returnsEditViewWithItemAndDropdowns() throws Exception {
        when(catalogService.findCatalogItem(1)).thenReturn(sampleItem);

        mockMvc.perform(get("/catalog/edit/1"))
                .andExpect(status().isOk())
                .andExpect(view().name("catalog/edit"))
                .andExpect(model().attributeExists("catalogItem"))
                .andExpect(model().attributeExists("brands"))
                .andExpect(model().attributeExists("types"));
    }

    @Test
    void editForm_nonExistingItem_returns404() throws Exception {
        when(catalogService.findCatalogItem(999)).thenReturn(null);

        mockMvc.perform(get("/catalog/edit/999"))
                .andExpect(status().isNotFound());
    }

    @Test
    void edit_validItem_redirectsToIndex() throws Exception {
        mockMvc.perform(post("/catalog/edit/1")
                        .with(csrf())
                        .param("name", "Updated Item")
                        .param("price", "39.99")
                        .param("catalogBrand", "1")
                        .param("catalogType", "1")
                        .param("pictureFileName", "test.png"))
                .andExpect(status().is3xxRedirection())
                .andExpect(redirectedUrl("/"));

        verify(catalogService).updateCatalogItem(any(CatalogItem.class));
    }

    @Test
    void edit_invalidItem_returnsEditViewWithErrors() throws Exception {
        mockMvc.perform(post("/catalog/edit/1")
                        .with(csrf())
                        .param("name", "")
                        .param("price", ""))
                .andExpect(status().isOk())
                .andExpect(view().name("catalog/edit"))
                .andExpect(model().attributeHasErrors("catalogItem"))
                .andExpect(model().attributeExists("brands"))
                .andExpect(model().attributeExists("types"));

        verify(catalogService, never()).updateCatalogItem(any());
    }

    // --- Delete ---

    @Test
    void deleteForm_existingItem_returnsDeleteView() throws Exception {
        when(catalogService.findCatalogItem(1)).thenReturn(sampleItem);

        mockMvc.perform(get("/catalog/delete/1"))
                .andExpect(status().isOk())
                .andExpect(view().name("catalog/delete"))
                .andExpect(model().attributeExists("catalogItem"));
    }

    @Test
    void deleteForm_nonExistingItem_returns404() throws Exception {
        when(catalogService.findCatalogItem(999)).thenReturn(null);

        mockMvc.perform(get("/catalog/delete/999"))
                .andExpect(status().isNotFound());
    }

    @Test
    void deleteConfirmed_existingItem_removesAndRedirects() throws Exception {
        when(catalogService.findCatalogItem(1)).thenReturn(sampleItem);

        mockMvc.perform(post("/catalog/delete/1").with(csrf()))
                .andExpect(status().is3xxRedirection())
                .andExpect(redirectedUrl("/"));

        verify(catalogService).removeCatalogItem(sampleItem);
    }

    @Test
    void deleteConfirmed_nonExistingItem_returns404() throws Exception {
        when(catalogService.findCatalogItem(999)).thenReturn(null);

        mockMvc.perform(post("/catalog/delete/999").with(csrf()))
                .andExpect(status().isNotFound());
    }

}
