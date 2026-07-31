package com.eshop.modernized.config;

import org.springframework.boot.context.properties.ConfigurationProperties;

@ConfigurationProperties(prefix = "app")
public record AppProperties(
        boolean useMockData,
        boolean useCustomizationData
) {
}
