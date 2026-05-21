package com.eshop.catalog.infrastructure;

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
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.core.io.ClassPathResource;
import org.springframework.stereotype.Component;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.math.BigDecimal;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.regex.Pattern;

@Component
@ConditionalOnProperty(name = "app.use-mock-data", havingValue = "false", matchIfMissing = true)
public class CatalogDbInitializer implements ApplicationRunner {

    private static final Logger log = LoggerFactory.getLogger(CatalogDbInitializer.class);
    private static final Pattern CSV_SPLIT = Pattern.compile(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

    private final CatalogItemRepository catalogItemRepository;
    private final CatalogBrandRepository catalogBrandRepository;
    private final CatalogTypeRepository catalogTypeRepository;

    @Value("${app.use-customization-data:false}")
    private boolean useCustomizationData;

    public CatalogDbInitializer(CatalogItemRepository catalogItemRepository,
                                CatalogBrandRepository catalogBrandRepository,
                                CatalogTypeRepository catalogTypeRepository) {
        this.catalogItemRepository = catalogItemRepository;
        this.catalogBrandRepository = catalogBrandRepository;
        this.catalogTypeRepository = catalogTypeRepository;
    }

    @Override
    public void run(ApplicationArguments args) {
        if (catalogBrandRepository.count() > 0) {
            log.info("Database already seeded, skipping initialization");
            return;
        }

        log.info("Seeding catalog database...");
        seedCatalogBrands();
        seedCatalogTypes();
        seedCatalogItems();
        log.info("Catalog database seeding complete");
    }

    private void seedCatalogBrands() {
        List<CatalogBrand> brands = useCustomizationData
                ? getCatalogBrandsFromFile()
                : PreconfiguredData.getPreconfiguredCatalogBrands();
        catalogBrandRepository.saveAll(brands);
        log.info("Seeded {} catalog brands", brands.size());
    }

    private void seedCatalogTypes() {
        List<CatalogType> types = useCustomizationData
                ? getCatalogTypesFromFile()
                : PreconfiguredData.getPreconfiguredCatalogTypes();
        catalogTypeRepository.saveAll(types);
        log.info("Seeded {} catalog types", types.size());
    }

    private void seedCatalogItems() {
        List<CatalogItem> items = useCustomizationData
                ? getCatalogItemsFromFile()
                : PreconfiguredData.getPreconfiguredCatalogItems();
        resolveItemEntityReferences(items);
        catalogItemRepository.saveAll(items);
        log.info("Seeded {} catalog items", items.size());
    }

    private void resolveItemEntityReferences(List<CatalogItem> items) {
        Map<Integer, CatalogBrand> brandsById = new HashMap<>();
        catalogBrandRepository.findAll().forEach(b -> brandsById.put(b.getId(), b));
        Map<Integer, CatalogType> typesById = new HashMap<>();
        catalogTypeRepository.findAll().forEach(t -> typesById.put(t.getId(), t));
        for (CatalogItem item : items) {
            item.setCatalogBrand(brandsById.get(item.getCatalogBrandId()));
            item.setCatalogType(typesById.get(item.getCatalogTypeId()));
        }
    }

    private List<CatalogBrand> getCatalogBrandsFromFile() {
        try {
            ClassPathResource resource = new ClassPathResource("setup/CatalogBrands.csv");
            if (!resource.exists()) {
                return PreconfiguredData.getPreconfiguredCatalogBrands();
            }
            List<CatalogBrand> brands = new ArrayList<>();
            try (BufferedReader reader = new BufferedReader(
                    new InputStreamReader(resource.getInputStream(), StandardCharsets.UTF_8))) {
                reader.readLine(); // skip header
                String line;
                while ((line = reader.readLine()) != null) {
                    String value = line.trim().replace("\"", "");
                    if (!value.isEmpty()) {
                        CatalogBrand brand = new CatalogBrand();
                        brand.setBrand(value);
                        brands.add(brand);
                    }
                }
            }
            return brands;
        } catch (IOException e) {
            log.warn("Failed to read CatalogBrands.csv, using preconfigured data", e);
            return PreconfiguredData.getPreconfiguredCatalogBrands();
        }
    }

    private List<CatalogType> getCatalogTypesFromFile() {
        try {
            ClassPathResource resource = new ClassPathResource("setup/CatalogTypes.csv");
            if (!resource.exists()) {
                return PreconfiguredData.getPreconfiguredCatalogTypes();
            }
            List<CatalogType> types = new ArrayList<>();
            try (BufferedReader reader = new BufferedReader(
                    new InputStreamReader(resource.getInputStream(), StandardCharsets.UTF_8))) {
                reader.readLine(); // skip header
                String line;
                while ((line = reader.readLine()) != null) {
                    String value = line.trim().replace("\"", "");
                    if (!value.isEmpty()) {
                        CatalogType type = new CatalogType();
                        type.setType(value);
                        types.add(type);
                    }
                }
            }
            return types;
        } catch (IOException e) {
            log.warn("Failed to read CatalogTypes.csv, using preconfigured data", e);
            return PreconfiguredData.getPreconfiguredCatalogTypes();
        }
    }

    private List<CatalogItem> getCatalogItemsFromFile() {
        try {
            ClassPathResource resource = new ClassPathResource("setup/CatalogItems.csv");
            if (!resource.exists()) {
                return PreconfiguredData.getPreconfiguredCatalogItems();
            }

            Map<String, Integer> catalogTypeIdLookup = new HashMap<>();
            catalogTypeRepository.findAll().forEach(ct -> catalogTypeIdLookup.put(ct.getType(), ct.getId()));

            Map<String, Integer> catalogBrandIdLookup = new HashMap<>();
            catalogBrandRepository.findAll().forEach(cb -> catalogBrandIdLookup.put(cb.getBrand(), cb.getId()));

            List<CatalogItem> items = new ArrayList<>();
            try (BufferedReader reader = new BufferedReader(
                    new InputStreamReader(resource.getInputStream(), StandardCharsets.UTF_8))) {
                String headerLine = reader.readLine();
                if (headerLine == null) {
                    return PreconfiguredData.getPreconfiguredCatalogItems();
                }
                String[] headers = headerLine.toLowerCase().split(",");

                String line;
                while ((line = reader.readLine()) != null) {
                    String[] columns = CSV_SPLIT.split(line);
                    CatalogItem item = createCatalogItemFromCsv(columns, headers,
                            catalogTypeIdLookup, catalogBrandIdLookup);
                    if (item != null) {
                        items.add(item);
                    }
                }
            }
            return items;
        } catch (IOException e) {
            log.warn("Failed to read CatalogItems.csv, using preconfigured data", e);
            return PreconfiguredData.getPreconfiguredCatalogItems();
        }
    }

    private CatalogItem createCatalogItemFromCsv(String[] columns, String[] headers,
                                                  Map<String, Integer> typeIdLookup,
                                                  Map<String, Integer> brandIdLookup) {
        if (columns.length != headers.length) {
            log.warn("Column count {} does not match header count {}", columns.length, headers.length);
            return null;
        }

        int typeNameIdx = Arrays.asList(headers).indexOf("catalogtypename");
        int brandNameIdx = Arrays.asList(headers).indexOf("catalogbrandname");
        int descIdx = Arrays.asList(headers).indexOf("description");
        int nameIdx = Arrays.asList(headers).indexOf("name");
        int priceIdx = Arrays.asList(headers).indexOf("price");
        int picIdx = Arrays.asList(headers).indexOf("picturefilename");

        if (typeNameIdx < 0 || brandNameIdx < 0 || descIdx < 0 || nameIdx < 0 || priceIdx < 0 || picIdx < 0) {
            log.warn("Missing required CSV headers. Found: {}", Arrays.asList(headers));
            return null;
        }

        String typeName = columns[typeNameIdx].replace("\"", "").trim();
        String brandName = columns[brandNameIdx].replace("\"", "").trim();

        if (!typeIdLookup.containsKey(typeName) || !brandIdLookup.containsKey(brandName)) {
            log.warn("Unknown type '{}' or brand '{}'", typeName, brandName);
            return null;
        }

        CatalogItem item = new CatalogItem();
        item.setCatalogTypeId(typeIdLookup.get(typeName));
        item.setCatalogBrandId(brandIdLookup.get(brandName));
        item.setDescription(columns[descIdx].replace("\"", "").trim());
        item.setName(columns[nameIdx].replace("\"", "").trim());
        item.setPrice(new BigDecimal(columns[priceIdx].replace("\"", "").trim()));
        item.setPictureFileName(columns[picIdx].replace("\"", "").trim());

        int stockIdx = Arrays.asList(headers).indexOf("availablestock");
        if (stockIdx >= 0 && stockIdx < columns.length) {
            String val = columns[stockIdx].replace("\"", "").trim();
            if (!val.isEmpty()) {
                item.setAvailableStock(Integer.parseInt(val));
            }
        }

        int restockIdx = Arrays.asList(headers).indexOf("restockthreshold");
        if (restockIdx >= 0 && restockIdx < columns.length) {
            String val = columns[restockIdx].replace("\"", "").trim();
            if (!val.isEmpty()) {
                item.setRestockThreshold(Integer.parseInt(val));
            }
        }

        int maxStockIdx = Arrays.asList(headers).indexOf("maxstockthreshold");
        if (maxStockIdx >= 0 && maxStockIdx < columns.length) {
            String val = columns[maxStockIdx].replace("\"", "").trim();
            if (!val.isEmpty()) {
                item.setMaxStockThreshold(Integer.parseInt(val));
            }
        }

        int reorderIdx = Arrays.asList(headers).indexOf("onreorder");
        if (reorderIdx >= 0 && reorderIdx < columns.length) {
            String val = columns[reorderIdx].replace("\"", "").trim();
            if (!val.isEmpty()) {
                item.setOnReorder(Boolean.parseBoolean(val));
            }
        }

        return item;
    }
}
