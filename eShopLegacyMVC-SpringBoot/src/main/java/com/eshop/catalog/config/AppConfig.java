package com.eshop.catalog.config;

import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import com.eshop.catalog.repository.CatalogBrandRepository;
import com.eshop.catalog.repository.CatalogItemRepository;
import com.eshop.catalog.repository.CatalogTypeRepository;
import com.eshop.catalog.service.CatalogService;
import com.eshop.catalog.service.CatalogServiceImpl;
import com.eshop.catalog.service.CatalogServiceMock;

@Configuration
public class AppConfig {

    @Bean
    @ConditionalOnProperty(name = "app.use-mock-data", havingValue = "true")
    public CatalogService catalogServiceMock() {
        return new CatalogServiceMock();
    }

    @Bean
    @ConditionalOnProperty(name = "app.use-mock-data", havingValue = "false", matchIfMissing = true)
    public CatalogService catalogService(CatalogItemRepository catalogItemRepository,
                                         CatalogBrandRepository catalogBrandRepository,
                                         CatalogTypeRepository catalogTypeRepository,
                                         CatalogItemHiLoGenerator hiLoGenerator) {
        return new CatalogServiceImpl(catalogItemRepository, catalogBrandRepository,
                catalogTypeRepository, hiLoGenerator);
    }
}
