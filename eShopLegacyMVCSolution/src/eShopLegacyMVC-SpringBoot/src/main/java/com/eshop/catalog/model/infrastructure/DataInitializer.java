package com.eshop.catalog.model.infrastructure;

import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogType;
import com.eshop.catalog.repository.CatalogBrandRepository;
import com.eshop.catalog.repository.CatalogItemRepository;
import com.eshop.catalog.repository.CatalogTypeRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.ApplicationArguments;
import org.springframework.boot.ApplicationRunner;
import org.springframework.stereotype.Component;

@Component
public class DataInitializer implements ApplicationRunner {

    private static final Logger log = LoggerFactory.getLogger(DataInitializer.class);

    private final CatalogBrandRepository brandRepository;
    private final CatalogTypeRepository typeRepository;
    private final CatalogItemRepository itemRepository;

    @Value("${app.use-customization-data:false}")
    private boolean useCustomizationData;

    public DataInitializer(CatalogBrandRepository brandRepository,
                           CatalogTypeRepository typeRepository,
                           CatalogItemRepository itemRepository) {
        this.brandRepository = brandRepository;
        this.typeRepository = typeRepository;
        this.itemRepository = itemRepository;
    }

    @Override
    public void run(ApplicationArguments args) {
        if (!useCustomizationData) {
            log.info("Customization data seeding is disabled");
            return;
        }

        if (brandRepository.count() > 0 || typeRepository.count() > 0 || itemRepository.count() > 0) {
            log.info("Database already contains data, skipping seed");
            return;
        }

        log.info("Seeding database with preconfigured data");

        for (CatalogBrand brand : PreconfiguredData.getCatalogBrands()) {
            brandRepository.save(brand);
        }

        for (CatalogType type : PreconfiguredData.getCatalogTypes()) {
            typeRepository.save(type);
        }

        for (CatalogItem item : PreconfiguredData.getCatalogItems()) {
            itemRepository.save(item);
        }

        log.info("Seeding complete: {} brands, {} types, {} items",
                brandRepository.count(), typeRepository.count(), itemRepository.count());
    }
}
