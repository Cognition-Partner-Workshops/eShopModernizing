package com.eshop.catalog.controller;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyInt;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;
import static org.springframework.security.test.web.servlet.request.SecurityMockMvcRequestPostProcessors.csrf;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.model;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.redirectedUrlPattern;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.view;

import com.eshop.catalog.config.CatalogMetrics;
import com.eshop.catalog.dto.PaginatedItemsDto;
import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogType;
import com.eshop.catalog.service.CatalogService;
import java.math.BigDecimal;
import java.util.List;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.WebMvcTest;
import org.springframework.test.context.bean.override.mockito.MockitoBean;
import org.springframework.security.test.context.support.WithMockUser;
import org.springframework.test.web.servlet.MockMvc;

@WebMvcTest(CatalogController.class)
@WithMockUser
class CatalogControllerTest {

  @Autowired private MockMvc mockMvc;

  @MockitoBean private CatalogService catalogService;
  @MockitoBean private CatalogMetrics catalogMetrics;

  private CatalogItem sampleItem;
  private CatalogBrand sampleBrand;
  private CatalogType sampleType;

  @BeforeEach
  void setUp() {
    sampleBrand = new CatalogBrand();
    sampleBrand.setId(1);
    sampleBrand.setBrand("TestBrand");

    sampleType = new CatalogType();
    sampleType.setId(1);
    sampleType.setType("TestType");

    sampleItem = new CatalogItem();
    sampleItem.setId(1);
    sampleItem.setName("Test Item");
    sampleItem.setDescription("Test Description");
    sampleItem.setPrice(new BigDecimal("19.99"));
    sampleItem.setCatalogBrandId(1);
    sampleItem.setCatalogTypeId(1);
    sampleItem.setCatalogBrand(sampleBrand);
    sampleItem.setCatalogType(sampleType);
    sampleItem.setAvailableStock(100);
  }

  @Test
  void index_returnsCatalogIndexView() throws Exception {
    PaginatedItemsDto<CatalogItem> paginatedItems =
        new PaginatedItemsDto<>(0, 10, 1, List.of(sampleItem));
    when(catalogService.getCatalogItemsPaginated(10, 0)).thenReturn(paginatedItems);

    mockMvc
        .perform(get("/"))
        .andExpect(status().isOk())
        .andExpect(view().name("catalog/index"))
        .andExpect(model().attributeExists("paginatedItems"))
        .andExpect(model().attribute("title", "Index"));
  }

  @Test
  void index_withCustomPagination() throws Exception {
    PaginatedItemsDto<CatalogItem> paginatedItems =
        new PaginatedItemsDto<>(1, 5, 10, List.of(sampleItem));
    when(catalogService.getCatalogItemsPaginated(5, 1)).thenReturn(paginatedItems);

    mockMvc
        .perform(get("/").param("pageSize", "5").param("pageIndex", "1"))
        .andExpect(status().isOk())
        .andExpect(view().name("catalog/index"));
  }

  @Test
  void details_existingItem_returnsDetailsView() throws Exception {
    when(catalogService.findCatalogItem(1)).thenReturn(sampleItem);

    mockMvc
        .perform(get("/catalog/details/1"))
        .andExpect(status().isOk())
        .andExpect(view().name("catalog/details"))
        .andExpect(model().attributeExists("catalogItem"))
        .andExpect(model().attribute("title", "Details"));

    verify(catalogMetrics).incrementItemsViewed();
  }

  @Test
  void details_nonExistingItem_returnsErrorView() throws Exception {
    when(catalogService.findCatalogItem(999)).thenReturn(null);

    mockMvc
        .perform(get("/catalog/details/999"))
        .andExpect(status().isOk())
        .andExpect(view().name("error"));
  }

  @Test
  void createForm_returnsCreateView() throws Exception {
    when(catalogService.getCatalogTypes()).thenReturn(List.of(sampleType));
    when(catalogService.getCatalogBrands()).thenReturn(List.of(sampleBrand));

    mockMvc
        .perform(get("/catalog/create"))
        .andExpect(status().isOk())
        .andExpect(view().name("catalog/create"))
        .andExpect(model().attributeExists("catalogItem"))
        .andExpect(model().attributeExists("types"))
        .andExpect(model().attributeExists("brands"))
        .andExpect(model().attribute("title", "Create"));
  }

  @Test
  void create_validItem_redirectsToIndex() throws Exception {
    mockMvc
        .perform(
            post("/catalog/create")
                .with(csrf())
                .param("name", "New Item")
                .param("price", "29.99")
                .param("catalogBrandId", "1")
                .param("catalogTypeId", "1")
                .param("availableStock", "50")
                .param("restockThreshold", "5")
                .param("maxStockThreshold", "100"))
        .andExpect(status().is3xxRedirection())
        .andExpect(redirectedUrlPattern("/**"));

    verify(catalogService).createCatalogItem(any(CatalogItem.class));
    verify(catalogMetrics).incrementItemsCreated();
  }

  @Test
  void create_invalidItem_returnsCreateView() throws Exception {
    when(catalogService.getCatalogTypes()).thenReturn(List.of(sampleType));
    when(catalogService.getCatalogBrands()).thenReturn(List.of(sampleBrand));

    mockMvc
        .perform(post("/catalog/create").with(csrf()))
        .andExpect(status().isOk())
        .andExpect(view().name("catalog/create"));

    verify(catalogService, never()).createCatalogItem(any(CatalogItem.class));
  }

  @Test
  void editForm_existingItem_returnsEditView() throws Exception {
    when(catalogService.findCatalogItem(1)).thenReturn(sampleItem);
    when(catalogService.getCatalogTypes()).thenReturn(List.of(sampleType));
    when(catalogService.getCatalogBrands()).thenReturn(List.of(sampleBrand));

    mockMvc
        .perform(get("/catalog/edit/1"))
        .andExpect(status().isOk())
        .andExpect(view().name("catalog/edit"))
        .andExpect(model().attributeExists("catalogItem"))
        .andExpect(model().attributeExists("types"))
        .andExpect(model().attributeExists("brands"))
        .andExpect(model().attribute("title", "Edit"));
  }

  @Test
  void editForm_nonExistingItem_returnsErrorView() throws Exception {
    when(catalogService.findCatalogItem(999)).thenReturn(null);

    mockMvc
        .perform(get("/catalog/edit/999"))
        .andExpect(status().isOk())
        .andExpect(view().name("error"));
  }

  @Test
  void edit_validItem_redirectsToIndex() throws Exception {
    mockMvc
        .perform(
            post("/catalog/edit/1")
                .with(csrf())
                .param("id", "1")
                .param("name", "Updated Item")
                .param("price", "39.99")
                .param("catalogBrandId", "1")
                .param("catalogTypeId", "1")
                .param("availableStock", "50")
                .param("restockThreshold", "5")
                .param("maxStockThreshold", "100"))
        .andExpect(status().is3xxRedirection())
        .andExpect(redirectedUrlPattern("/**"));

    verify(catalogService).updateCatalogItem(any(CatalogItem.class));
    verify(catalogMetrics).incrementItemsUpdated();
  }

  @Test
  void edit_invalidItem_returnsEditView() throws Exception {
    when(catalogService.getCatalogTypes()).thenReturn(List.of(sampleType));
    when(catalogService.getCatalogBrands()).thenReturn(List.of(sampleBrand));

    mockMvc
        .perform(post("/catalog/edit/1").with(csrf()))
        .andExpect(status().isOk())
        .andExpect(view().name("catalog/edit"));

    verify(catalogService, never()).updateCatalogItem(any(CatalogItem.class));
  }

  @Test
  void deleteForm_existingItem_returnsDeleteView() throws Exception {
    when(catalogService.findCatalogItem(1)).thenReturn(sampleItem);

    mockMvc
        .perform(get("/catalog/delete/1"))
        .andExpect(status().isOk())
        .andExpect(view().name("catalog/delete"))
        .andExpect(model().attributeExists("catalogItem"))
        .andExpect(model().attribute("title", "Delete"));
  }

  @Test
  void deleteForm_nonExistingItem_returnsErrorView() throws Exception {
    when(catalogService.findCatalogItem(999)).thenReturn(null);

    mockMvc
        .perform(get("/catalog/delete/999"))
        .andExpect(status().isOk())
        .andExpect(view().name("error"));
  }

  @Test
  void deleteConfirmed_existingItem_redirectsToIndex() throws Exception {
    when(catalogService.findCatalogItem(1)).thenReturn(sampleItem);

    mockMvc
        .perform(post("/catalog/delete/1").with(csrf()))
        .andExpect(status().is3xxRedirection())
        .andExpect(redirectedUrlPattern("/**"));

    verify(catalogService).removeCatalogItem(sampleItem);
    verify(catalogMetrics).incrementItemsDeleted();
  }

  @Test
  void deleteConfirmed_nonExistingItem_returnsErrorView() throws Exception {
    when(catalogService.findCatalogItem(999)).thenReturn(null);

    mockMvc
        .perform(post("/catalog/delete/999").with(csrf()))
        .andExpect(status().isOk())
        .andExpect(view().name("error"));

    verify(catalogService, never()).removeCatalogItem(any(CatalogItem.class));
  }
}
