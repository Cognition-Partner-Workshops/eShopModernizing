package com.eshop.catalog;

import static org.assertj.core.api.Assertions.assertThat;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.eshop.catalog.controller.CatalogController;
import com.eshop.catalog.service.CatalogService;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.context.ApplicationContext;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.web.servlet.MockMvc;

@SpringBootTest
@AutoConfigureMockMvc
@ActiveProfiles("test")
class SmokeTest {

  @Autowired private ApplicationContext applicationContext;
  @Autowired private MockMvc mockMvc;

  @Test
  void contextLoads() {
    assertThat(applicationContext).isNotNull();
  }

  @Test
  void catalogControllerBeanExists() {
    assertThat(applicationContext.getBean(CatalogController.class)).isNotNull();
  }

  @Test
  void catalogServiceBeanExists() {
    assertThat(applicationContext.getBean(CatalogService.class)).isNotNull();
  }

  @Test
  void healthEndpoint_returnsUp() throws Exception {
    mockMvc
        .perform(get("/actuator/health"))
        .andExpect(status().isOk())
        .andExpect(jsonPath("$.status").value("UP"));
  }

  @Test
  void bootstrapCss_isAccessible() throws Exception {
    mockMvc.perform(get("/css/bootstrap.min.css")).andExpect(status().isOk());
  }

  @Test
  void rootRedirectsToCatalog() throws Exception {
    mockMvc.perform(get("/")).andExpect(status().is3xxRedirection());
  }

  @Test
  void catalogIndex_returns200() throws Exception {
    mockMvc.perform(get("/catalog")).andExpect(status().isOk());
  }

  @Test
  void favicon_isAccessible() throws Exception {
    mockMvc.perform(get("/favicon.ico")).andExpect(status().isOk());
  }
}
