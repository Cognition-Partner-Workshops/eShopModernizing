package com.eshop.webforms.config;

import com.eshop.webforms.repository.CatalogItemRepository;
import jakarta.persistence.EntityManager;
import org.springframework.boot.ApplicationArguments;
import org.springframework.boot.ApplicationRunner;
import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

@Component
@Profile("!mock")
public class CatalogDataInitializer implements ApplicationRunner {

    private final CatalogItemRepository catalogItemRepository;
    private final EntityManager entityManager;

    public CatalogDataInitializer(CatalogItemRepository catalogItemRepository,
                                  EntityManager entityManager) {
        this.catalogItemRepository = catalogItemRepository;
        this.entityManager = entityManager;
    }

    @Override
    @Transactional
    public void run(ApplicationArguments args) {
        if (catalogItemRepository.count() == 0) {
            PreconfiguredData.getCatalogBrands().forEach(entityManager::persist);
            PreconfiguredData.getCatalogTypes().forEach(entityManager::persist);
            PreconfiguredData.getCatalogItems().forEach(entityManager::persist);
        }
    }
}
