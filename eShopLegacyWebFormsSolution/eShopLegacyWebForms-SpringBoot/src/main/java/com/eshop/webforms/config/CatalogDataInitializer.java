package com.eshop.webforms.config;

import com.eshop.webforms.repository.CatalogBrandRepository;
import com.eshop.webforms.repository.CatalogItemRepository;
import com.eshop.webforms.repository.CatalogTypeRepository;
import org.springframework.boot.ApplicationArguments;
import org.springframework.boot.ApplicationRunner;
import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Component;

@Component
@Profile("!mock")
public class CatalogDataInitializer implements ApplicationRunner {

    private final CatalogBrandRepository catalogBrandRepository;
    private final CatalogTypeRepository catalogTypeRepository;
    private final CatalogItemRepository catalogItemRepository;

    public CatalogDataInitializer(CatalogBrandRepository catalogBrandRepository,
                                  CatalogTypeRepository catalogTypeRepository,
                                  CatalogItemRepository catalogItemRepository) {
        this.catalogBrandRepository = catalogBrandRepository;
        this.catalogTypeRepository = catalogTypeRepository;
        this.catalogItemRepository = catalogItemRepository;
    }

    @Override
    public void run(ApplicationArguments args) {
        if (catalogItemRepository.count() == 0) {
            catalogBrandRepository.saveAll(PreconfiguredData.getCatalogBrands());
            catalogTypeRepository.saveAll(PreconfiguredData.getCatalogTypes());
            catalogItemRepository.saveAll(PreconfiguredData.getCatalogItems());
        }
    }
}
