package com.eshop.catalog.config;

import java.io.BufferedReader;
import java.io.InputStreamReader;
import java.math.BigDecimal;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.boot.ApplicationArguments;
import org.springframework.boot.ApplicationRunner;
import org.springframework.core.io.ClassPathResource;
import org.springframework.stereotype.Component;

import com.eshop.catalog.domain.entity.CatalogBrand;
import com.eshop.catalog.domain.entity.CatalogItem;
import com.eshop.catalog.domain.entity.CatalogType;
import com.eshop.catalog.repository.CatalogBrandRepository;
import com.eshop.catalog.repository.CatalogItemRepository;
import com.eshop.catalog.repository.CatalogTypeRepository;

@Component
public class DataInitializer implements ApplicationRunner {

    private static final Logger logger = LoggerFactory.getLogger(DataInitializer.class);

    private final AppProperties appProperties;
    private final CatalogTypeRepository catalogTypeRepository;
    private final CatalogBrandRepository catalogBrandRepository;
    private final CatalogItemRepository catalogItemRepository;

    public DataInitializer(AppProperties appProperties,
                           CatalogTypeRepository catalogTypeRepository,
                           CatalogBrandRepository catalogBrandRepository,
                           CatalogItemRepository catalogItemRepository) {
        this.appProperties = appProperties;
        this.catalogTypeRepository = catalogTypeRepository;
        this.catalogBrandRepository = catalogBrandRepository;
        this.catalogItemRepository = catalogItemRepository;
    }

    @Override
    public void run(ApplicationArguments args) throws Exception {
        if (!appProperties.isUseCustomizationData()) {
            logger.info("UseCustomizationData is disabled. Relying on Flyway seed data.");
            return;
        }

        logger.info("UseCustomizationData is enabled. Loading CSV data...");
        seedCatalogTypes();
        seedCatalogBrands();
        seedCatalogItems();
        logger.info("CSV data loading complete.");
    }

    private void seedCatalogTypes() throws Exception {
        List<String[]> records = parseCsv("data/CatalogTypes.csv");
        int headerIndex = getColumnIndex(records.get(0), "catalogtype");

        for (int i = 1; i < records.size(); i++) {
            String[] fields = records.get(i);
            String typeName = trimQuotes(fields[headerIndex]);
            if (typeName.isEmpty()) {
                continue;
            }
            CatalogType type = new CatalogType(typeName);
            catalogTypeRepository.save(type);
        }
        logger.info("Seeded {} catalog types from CSV.", records.size() - 1);
    }

    private void seedCatalogBrands() throws Exception {
        List<String[]> records = parseCsv("data/CatalogBrands.csv");
        int headerIndex = getColumnIndex(records.get(0), "catalogbrand");

        for (int i = 1; i < records.size(); i++) {
            String[] fields = records.get(i);
            String brandName = trimQuotes(fields[headerIndex]);
            if (brandName.isEmpty()) {
                continue;
            }
            CatalogBrand brand = new CatalogBrand(brandName);
            catalogBrandRepository.save(brand);
        }
        logger.info("Seeded {} catalog brands from CSV.", records.size() - 1);
    }

    private void seedCatalogItems() throws Exception {
        List<String[]> records = parseCsv("data/CatalogItems.csv");
        String[] headers = records.get(0);

        int typeNameIdx = getColumnIndex(headers, "catalogtypename");
        int brandNameIdx = getColumnIndex(headers, "catalogbrandname");
        int descriptionIdx = getColumnIndex(headers, "description");
        int nameIdx = getColumnIndex(headers, "name");
        int priceIdx = getColumnIndex(headers, "price");
        int pictureFileIdx = getColumnIndex(headers, "pictureFileName");
        int availableStockIdx = getOptionalColumnIndex(headers, "availablestock");
        int restockThresholdIdx = getOptionalColumnIndex(headers, "restockthreshold");
        int maxStockThresholdIdx = getOptionalColumnIndex(headers, "maxstockthreshold");
        int onReorderIdx = getOptionalColumnIndex(headers, "onreorder");

        List<CatalogType> allTypes = catalogTypeRepository.findAll();
        List<CatalogBrand> allBrands = catalogBrandRepository.findAll();

        for (int i = 1; i < records.size(); i++) {
            String[] fields = records.get(i);

            String typeName = trimQuotes(fields[typeNameIdx]);
            String brandName = trimQuotes(fields[brandNameIdx]);
            String description = trimQuotes(fields[descriptionIdx]);
            String name = trimQuotes(fields[nameIdx]);
            BigDecimal price = new BigDecimal(trimQuotes(fields[priceIdx]));
            String pictureFileName = trimQuotes(fields[pictureFileIdx]);

            CatalogType catalogType = findTypeByName(allTypes, typeName);
            CatalogBrand catalogBrand = findBrandByName(allBrands, brandName);

            if (catalogType == null) {
                logger.warn("Skipping item '{}': CatalogType '{}' not found.", name, typeName);
                continue;
            }
            if (catalogBrand == null) {
                logger.warn("Skipping item '{}': CatalogBrand '{}' not found.", name, brandName);
                continue;
            }

            CatalogItem item = new CatalogItem();
            item.setId(i);
            item.setName(name);
            item.setDescription(description);
            item.setPrice(price);
            item.setPictureFileName(pictureFileName);
            item.setCatalogType(catalogType);
            item.setCatalogBrand(catalogBrand);

            if (availableStockIdx >= 0 && availableStockIdx < fields.length) {
                String val = trimQuotes(fields[availableStockIdx]);
                if (!val.isEmpty()) {
                    item.setAvailableStock(Integer.parseInt(val));
                }
            }
            if (restockThresholdIdx >= 0 && restockThresholdIdx < fields.length) {
                String val = trimQuotes(fields[restockThresholdIdx]);
                if (!val.isEmpty()) {
                    item.setRestockThreshold(Integer.parseInt(val));
                }
            }
            if (maxStockThresholdIdx >= 0 && maxStockThresholdIdx < fields.length) {
                String val = trimQuotes(fields[maxStockThresholdIdx]);
                if (!val.isEmpty()) {
                    item.setMaxStockThreshold(Integer.parseInt(val));
                }
            }
            if (onReorderIdx >= 0 && onReorderIdx < fields.length) {
                String val = trimQuotes(fields[onReorderIdx]);
                if (!val.isEmpty()) {
                    item.setOnReorder(Boolean.parseBoolean(val));
                }
            }

            catalogItemRepository.save(item);
        }
        logger.info("Seeded {} catalog items from CSV.", records.size() - 1);
    }

    private List<String[]> parseCsv(String resourcePath) throws Exception {
        List<String[]> records = new ArrayList<>();
        ClassPathResource resource = new ClassPathResource(resourcePath);

        try (BufferedReader reader = new BufferedReader(
                new InputStreamReader(resource.getInputStream(), StandardCharsets.UTF_8))) {
            String line;
            while ((line = reader.readLine()) != null) {
                if (line.trim().isEmpty()) {
                    continue;
                }
                String[] fields = line.split(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)", -1);
                records.add(fields);
            }
        }
        return records;
    }

    private int getColumnIndex(String[] headers, String columnName) {
        for (int i = 0; i < headers.length; i++) {
            if (trimQuotes(headers[i]).equalsIgnoreCase(columnName)) {
                return i;
            }
        }
        throw new IllegalArgumentException("Required header '" + columnName + "' not found in CSV.");
    }

    private int getOptionalColumnIndex(String[] headers, String columnName) {
        for (int i = 0; i < headers.length; i++) {
            if (trimQuotes(headers[i]).equalsIgnoreCase(columnName)) {
                return i;
            }
        }
        return -1;
    }

    private String trimQuotes(String value) {
        if (value == null) {
            return "";
        }
        String trimmed = value.trim();
        if (trimmed.length() >= 2 && trimmed.startsWith("\"") && trimmed.endsWith("\"")) {
            trimmed = trimmed.substring(1, trimmed.length() - 1);
        }
        return trimmed;
    }

    private CatalogType findTypeByName(List<CatalogType> types, String name) {
        for (CatalogType type : types) {
            if (type.getType().equalsIgnoreCase(name)) {
                return type;
            }
        }
        return null;
    }

    private CatalogBrand findBrandByName(List<CatalogBrand> brands, String name) {
        for (CatalogBrand brand : brands) {
            if (brand.getBrand().equalsIgnoreCase(name)) {
                return brand;
            }
        }
        return null;
    }
}
