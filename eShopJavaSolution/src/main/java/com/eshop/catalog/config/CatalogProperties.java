package com.eshop.catalog.config;

import org.springframework.boot.context.properties.ConfigurationProperties;

@ConfigurationProperties(prefix = "app")
public record CatalogProperties(boolean useMockData, boolean useCustomizationData) {}
