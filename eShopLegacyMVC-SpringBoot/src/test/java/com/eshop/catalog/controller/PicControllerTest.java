package com.eshop.catalog.controller;

import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.eshop.catalog.config.CatalogMetrics;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.service.CatalogService;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.WebMvcTest;
import org.springframework.http.MediaType;
import org.springframework.security.test.context.support.WithMockUser;
import org.springframework.test.context.bean.override.mockito.MockitoBean;
import org.springframework.test.web.servlet.MockMvc;

@WebMvcTest(PicController.class)
@WithMockUser
class PicControllerTest {

  @Autowired private MockMvc mockMvc;

  @MockitoBean private CatalogService catalogService;
  @MockitoBean private CatalogMetrics catalogMetrics;

  @Test
  void getImage_invalidId_returnsBadRequest() throws Exception {
    mockMvc.perform(get("/items/0/pic")).andExpect(status().isBadRequest());
  }

  @Test
  void getImage_negativeId_returnsBadRequest() throws Exception {
    mockMvc.perform(get("/items/-1/pic")).andExpect(status().isBadRequest());
  }

  @Test
  void getImage_itemNotFound_returnsNotFound() throws Exception {
    when(catalogService.findCatalogItem(999)).thenReturn(null);

    mockMvc.perform(get("/items/999/pic")).andExpect(status().isNotFound());
  }

  @Test
  void getImage_imageFileNotFound_returnsNotFound() throws Exception {
    CatalogItem item = new CatalogItem();
    item.setId(1);
    item.setPictureFileName("nonexistent.png");
    when(catalogService.findCatalogItem(1)).thenReturn(item);

    mockMvc.perform(get("/items/1/pic")).andExpect(status().isNotFound());
  }

  @Test
  void getImage_pngFile_returnsImageWithCorrectContentType() throws Exception {
    CatalogItem item = new CatalogItem();
    item.setId(1);
    item.setPictureFileName("test.png");
    when(catalogService.findCatalogItem(1)).thenReturn(item);

    mockMvc
        .perform(get("/items/1/pic"))
        .andExpect(status().isOk())
        .andExpect(content().contentType(MediaType.IMAGE_PNG));
  }

  @Test
  void getImage_nullFileName_returnsBadRequest() throws Exception {
    CatalogItem item = new CatalogItem();
    item.setId(1);
    item.setPictureFileName(null);
    when(catalogService.findCatalogItem(1)).thenReturn(item);

    mockMvc.perform(get("/items/1/pic")).andExpect(status().isBadRequest());
  }

  @Test
  void getImage_pathTraversal_returnsBadRequest() throws Exception {
    CatalogItem item = new CatalogItem();
    item.setId(1);
    item.setPictureFileName("../../etc/passwd");
    when(catalogService.findCatalogItem(1)).thenReturn(item);

    mockMvc.perform(get("/items/1/pic")).andExpect(status().isBadRequest());
  }

  @Test
  void getImage_forwardSlash_returnsBadRequest() throws Exception {
    CatalogItem item = new CatalogItem();
    item.setId(1);
    item.setPictureFileName("subdir/image.png");
    when(catalogService.findCatalogItem(1)).thenReturn(item);

    mockMvc.perform(get("/items/1/pic")).andExpect(status().isBadRequest());
  }

  @Test
  void getImage_backslash_returnsBadRequest() throws Exception {
    CatalogItem item = new CatalogItem();
    item.setId(1);
    item.setPictureFileName("subdir\\image.png");
    when(catalogService.findCatalogItem(1)).thenReturn(item);

    mockMvc.perform(get("/items/1/pic")).andExpect(status().isBadRequest());
  }
}
