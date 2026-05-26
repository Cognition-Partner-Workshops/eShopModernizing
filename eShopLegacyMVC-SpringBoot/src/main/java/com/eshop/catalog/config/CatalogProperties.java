package com.eshop.catalog.config;

import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.stereotype.Component;

@Component
@ConfigurationProperties(prefix = "app.catalog")
public class CatalogProperties {

    private boolean useMockData = true;
    private boolean useCustomizationData = false;

    public boolean isUseMockData() {
        return useMockData;
    }

    public void setUseMockData(boolean useMockData) {
        this.useMockData = useMockData;
    }

    public boolean isUseCustomizationData() {
        return useCustomizationData;
    }

    public void setUseCustomizationData(boolean useCustomizationData) {
        this.useCustomizationData = useCustomizationData;
    }
}
