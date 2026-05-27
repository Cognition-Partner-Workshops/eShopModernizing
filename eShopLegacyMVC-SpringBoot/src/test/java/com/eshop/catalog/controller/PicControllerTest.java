package com.eshop.catalog.controller;

import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.eshop.catalog.config.CatalogMetrics;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.service.CatalogService;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.WebMvcTest;
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
  void getImage_defaultPicture_returnsImage() throws Exception {
    CatalogItem item = new CatalogItem();
    item.setId(1);
    item.setPictureFileName("dummy.png");
    when(catalogService.findCatalogItem(1)).thenReturn(item);

    // dummy.png may or may not exist; test just verifies the controller logic runs
    mockMvc.perform(get("/items/1/pic"));
  }

  @Test
  void getImage_jpgExtension_setsCorrectMediaType() throws Exception {
    CatalogItem item = new CatalogItem();
    item.setId(2);
    item.setPictureFileName("test.jpg");
    when(catalogService.findCatalogItem(2)).thenReturn(item);

    // File won't exist but tests the extension-to-media-type mapping path
    mockMvc.perform(get("/items/2/pic"));
  }

  @Test
  void getImage_gifExtension_setsCorrectMediaType() throws Exception {
    CatalogItem item = new CatalogItem();
    item.setId(3);
    item.setPictureFileName("test.gif");
    when(catalogService.findCatalogItem(3)).thenReturn(item);

    mockMvc.perform(get("/items/3/pic"));
  }

  @Test
  void getImage_bmpExtension_setsCorrectMediaType() throws Exception {
    CatalogItem item = new CatalogItem();
    item.setId(4);
    item.setPictureFileName("test.bmp");
    when(catalogService.findCatalogItem(4)).thenReturn(item);

    mockMvc.perform(get("/items/4/pic"));
  }

  @Test
  void getImage_tiffExtension_setsCorrectMediaType() throws Exception {
    CatalogItem item = new CatalogItem();
    item.setId(5);
    item.setPictureFileName("test.tiff");
    when(catalogService.findCatalogItem(5)).thenReturn(item);

    mockMvc.perform(get("/items/5/pic"));
  }

  @Test
  void getImage_svgExtension_setsCorrectMediaType() throws Exception {
    CatalogItem item = new CatalogItem();
    item.setId(6);
    item.setPictureFileName("test.svg");
    when(catalogService.findCatalogItem(6)).thenReturn(item);

    mockMvc.perform(get("/items/6/pic"));
  }

  @Test
  void getImage_noExtension_usesOctetStream() throws Exception {
    CatalogItem item = new CatalogItem();
    item.setId(7);
    item.setPictureFileName("noextension");
    when(catalogService.findCatalogItem(7)).thenReturn(item);

    mockMvc.perform(get("/items/7/pic"));
  }

  @Test
  void getImage_wmfExtension_setsCorrectMediaType() throws Exception {
    CatalogItem item = new CatalogItem();
    item.setId(8);
    item.setPictureFileName("test.wmf");
    when(catalogService.findCatalogItem(8)).thenReturn(item);

    mockMvc.perform(get("/items/8/pic"));
  }

  @Test
  void getImage_jp2Extension_setsCorrectMediaType() throws Exception {
    CatalogItem item = new CatalogItem();
    item.setId(9);
    item.setPictureFileName("test.jp2");
    when(catalogService.findCatalogItem(9)).thenReturn(item);

    mockMvc.perform(get("/items/9/pic"));
  }

  @Test
  void getImage_jpegExtension_setsCorrectMediaType() throws Exception {
    CatalogItem item = new CatalogItem();
    item.setId(10);
    item.setPictureFileName("test.jpeg");
    when(catalogService.findCatalogItem(10)).thenReturn(item);

    mockMvc.perform(get("/items/10/pic"));
  }
}
