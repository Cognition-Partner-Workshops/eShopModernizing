package com.eshop.catalog.config;

import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Configuration;

@Configuration
@ConfigurationProperties(prefix = "app")
public class AppConfig {

    private boolean useMockData;
    private boolean useCustomizationData;
    private String picsDirectory;

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

    public String getPicsDirectory() {
        return picsDirectory;
    }

    public void setPicsDirectory(String picsDirectory) {
        this.picsDirectory = picsDirectory;
    }
}
