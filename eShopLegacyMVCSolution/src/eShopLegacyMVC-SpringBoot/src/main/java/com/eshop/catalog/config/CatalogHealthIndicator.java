package com.eshop.catalog.config;

import org.springframework.boot.actuate.health.Health;
import org.springframework.boot.actuate.health.HealthIndicator;
import org.springframework.stereotype.Component;

@Component
public class CatalogHealthIndicator implements HealthIndicator {
    @Override
    public Health health() {
        return Health.up()
            .withDetail("service", "eshop-catalog")
            .withDetail("description", "eShop Catalog Service")
            .build();
    }
}
