package com.eshop.catalog.config;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.boot.context.properties.EnableConfigurationProperties;
import org.springframework.context.annotation.Configuration;

@Configuration
@EnableConfigurationProperties(AppProperties.class)
public class CatalogConfig {

  private static final Logger log = LoggerFactory.getLogger(CatalogConfig.class);

  private final AppProperties appProperties;

  public CatalogConfig(AppProperties appProperties) {
    this.appProperties = appProperties;
    log.info(
        "Catalog configuration loaded — useMockData={}, useCustomizationData={}",
        appProperties.isUseMockData(),
        appProperties.isUseCustomizationData());
  }
}
