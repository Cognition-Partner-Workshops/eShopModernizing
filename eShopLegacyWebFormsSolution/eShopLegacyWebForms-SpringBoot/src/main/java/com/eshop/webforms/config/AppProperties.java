package com.eshop.webforms.config;

import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.stereotype.Component;

@Component
@ConfigurationProperties(prefix = "app")
public class AppProperties {

    private boolean useMockData;
    private boolean useCustomizationData;

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
