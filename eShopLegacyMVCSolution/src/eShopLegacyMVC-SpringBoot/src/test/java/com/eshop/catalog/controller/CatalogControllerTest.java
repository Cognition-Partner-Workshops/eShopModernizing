package com.eshop.catalog.controller;

import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogType;
import com.eshop.catalog.service.ICatalogService;
import com.eshop.catalog.viewmodel.PaginatedItemsViewModel;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;

import java.math.BigDecimal;
import java.util.List;

import static org.mockito.ArgumentMatchers.anyInt;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.model;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.view;

@ExtendWith(MockitoExtension.class)
class CatalogControllerTest {

    private MockMvc mockMvc;

    @Mock
    private ICatalogService catalogService;

    @BeforeEach
    void setUp() {
        CatalogController controller = new CatalogController(catalogService);
        mockMvc = MockMvcBuilders.standaloneSetup(controller).build();
    }

    @Test
    void getIndexReturns200WithIndexView() throws Exception {
        CatalogItem item = createItem(1, "Test Item");
        PaginatedItemsViewModel<CatalogItem> paginated =
                new PaginatedItemsViewModel<>(0, 10, 1, List.of(item));
        when(catalogService.getCatalogItemsPaginated(anyInt(), anyInt())).thenReturn(paginated);

        mockMvc.perform(get("/catalog"))
                .andExpect(status().isOk())
                .andExpect(view().name("catalog/index"))
                .andExpect(model().attributeExists("model"));
    }

    @Test
    void getDetailsReturns200WithDetailView() throws Exception {
        CatalogItem item = createItem(1, "Detail Item");
        when(catalogService.findCatalogItem(1)).thenReturn(item);

        mockMvc.perform(get("/catalog/details/1"))
                .andExpect(status().isOk())
                .andExpect(view().name("catalog/details"))
                .andExpect(model().attributeExists("catalogItem"));
    }

    @Test
    void getDetailsReturns404ForNonExistentItem() throws Exception {
        when(catalogService.findCatalogItem(999)).thenReturn(null);

        mockMvc.perform(get("/catalog/details/999"))
                .andExpect(status().isNotFound());
    }

    @Test
    void getCreateReturns200WithForm() throws Exception {
        when(catalogService.getCatalogBrands()).thenReturn(List.of(createBrand(1, "Azure")));
        when(catalogService.getCatalogTypes()).thenReturn(List.of(createType(1, "Mug")));

        mockMvc.perform(get("/catalog/create"))
                .andExpect(status().isOk())
                .andExpect(view().name("catalog/create"))
                .andExpect(model().attributeExists("brands"))
                .andExpect(model().attributeExists("types"))
                .andExpect(model().attributeExists("catalogItem"));
    }

    @Test
    void getEditReturns200WithEditForm() throws Exception {
        CatalogItem item = createItem(1, "Edit Item");
        when(catalogService.findCatalogItem(1)).thenReturn(item);
        when(catalogService.getCatalogBrands()).thenReturn(List.of(createBrand(1, "Azure")));
        when(catalogService.getCatalogTypes()).thenReturn(List.of(createType(1, "Mug")));

        mockMvc.perform(get("/catalog/edit/1"))
                .andExpect(status().isOk())
                .andExpect(view().name("catalog/edit"))
                .andExpect(model().attributeExists("catalogItem"))
                .andExpect(model().attributeExists("brands"))
                .andExpect(model().attributeExists("types"));
    }

    @Test
    void getEditReturns404ForNonExistentItem() throws Exception {
        when(catalogService.findCatalogItem(999)).thenReturn(null);

        mockMvc.perform(get("/catalog/edit/999"))
                .andExpect(status().isNotFound());
    }

    @Test
    void getDeleteReturns200WithDeleteConfirmation() throws Exception {
        CatalogItem item = createItem(1, "Delete Item");
        when(catalogService.findCatalogItem(1)).thenReturn(item);

        mockMvc.perform(get("/catalog/delete/1"))
                .andExpect(status().isOk())
                .andExpect(view().name("catalog/delete"))
                .andExpect(model().attributeExists("catalogItem"));
    }

    @Test
    void getDeleteReturns404ForNonExistentItem() throws Exception {
        when(catalogService.findCatalogItem(999)).thenReturn(null);

        mockMvc.perform(get("/catalog/delete/999"))
                .andExpect(status().isNotFound());
    }

    private CatalogItem createItem(int id, String name) {
        CatalogItem item = new CatalogItem();
        item.setId(id);
        item.setName(name);
        item.setPrice(new BigDecimal("10.00"));
        item.setPictureFileName("test.png");
        return item;
    }

    private CatalogBrand createBrand(int id, String brand) {
        CatalogBrand b = new CatalogBrand();
        b.setId(id);
        b.setBrand(brand);
        return b;
    }

    private CatalogType createType(int id, String type) {
        CatalogType t = new CatalogType();
        t.setId(id);
        t.setType(type);
        return t;
    }
}
