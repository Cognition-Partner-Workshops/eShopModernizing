package com.eshop.catalog.config;

import com.eshop.catalog.model.CatalogItemHiLoGenerator;
import com.eshop.catalog.repository.CatalogBrandRepository;
import com.eshop.catalog.repository.CatalogItemRepository;
import com.eshop.catalog.repository.CatalogTypeRepository;
import com.eshop.catalog.service.CatalogServiceImpl;
import com.eshop.catalog.service.CatalogServiceMock;
import com.eshop.catalog.service.ICatalogService;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class CatalogConfig {

    @Bean
    @ConditionalOnProperty(name = "app.use-mock-data", havingValue = "true")
    public ICatalogService catalogServiceMock() {
        return new CatalogServiceMock();
    }

    @Bean
    @ConditionalOnProperty(name = "app.use-mock-data", havingValue = "false", matchIfMissing = true)
    public ICatalogService catalogService(CatalogItemRepository itemRepo,
                                          CatalogBrandRepository brandRepo,
                                          CatalogTypeRepository typeRepo,
                                          CatalogItemHiLoGenerator hiLoGenerator) {
        return new CatalogServiceImpl(itemRepo, brandRepo, typeRepo, hiLoGenerator);
    }
}
