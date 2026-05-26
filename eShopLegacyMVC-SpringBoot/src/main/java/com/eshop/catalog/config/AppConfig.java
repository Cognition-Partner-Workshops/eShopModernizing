package com.eshop.catalog.config;

import com.eshop.catalog.repository.CatalogBrandRepository;
import com.eshop.catalog.repository.CatalogItemRepository;
import com.eshop.catalog.repository.CatalogTypeRepository;
import com.eshop.catalog.service.CatalogService;
import com.eshop.catalog.service.impl.CatalogServiceImpl;
import com.eshop.catalog.service.impl.CatalogServiceMock;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;

/**
 * Central dependency-injection configuration — replaces the Autofac {@code ApplicationModule} from
 * the legacy .NET application. Conditionally registers either the mock or real {@link
 * CatalogService} implementation based on the {@code app.catalog.use-mock-data} property.
 */
@Configuration
public class AppConfig {

  private static final Logger log = LoggerFactory.getLogger(AppConfig.class);

  @Bean
  @ConditionalOnProperty(name = "app.catalog.use-mock-data", havingValue = "true", matchIfMissing = true)
  public CatalogService catalogServiceMock() {
    log.info("Registering CatalogServiceMock (app.catalog.use-mock-data=true)");
    return new CatalogServiceMock();
  }

  @Bean
  @ConditionalOnProperty(name = "app.catalog.use-mock-data", havingValue = "false")
  public CatalogService catalogServiceImpl(
      CatalogItemRepository catalogItemRepository,
      CatalogBrandRepository catalogBrandRepository,
      CatalogTypeRepository catalogTypeRepository,
      CatalogItemHiLoGenerator indexGenerator) {
    log.info("Registering CatalogServiceImpl (app.catalog.use-mock-data=false)");
    return new CatalogServiceImpl(
        catalogItemRepository, catalogBrandRepository, catalogTypeRepository, indexGenerator);
  }

  @Bean
  public CatalogItemHiLoGenerator catalogItemHiLoGenerator(JdbcTemplate jdbcTemplate) {
    return new CatalogItemHiLoGenerator(jdbcTemplate);
  }
}
