package com.eshop.catalog.config;

import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogType;
import com.eshop.catalog.repository.CatalogBrandRepository;
import com.eshop.catalog.repository.CatalogItemRepository;
import com.eshop.catalog.repository.CatalogTypeRepository;
import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.math.BigDecimal;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.regex.Pattern;
import java.util.stream.Collectors;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.boot.ApplicationArguments;
import org.springframework.boot.ApplicationRunner;
import org.springframework.core.io.ClassPathResource;
import org.springframework.stereotype.Component;

@Component
public class DataInitializer implements ApplicationRunner {

    private static final Logger log = LoggerFactory.getLogger(DataInitializer.class);
    private static final Pattern CSV_SPLIT_PATTERN = Pattern.compile(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

    private final CatalogProperties catalogProperties;
    private final CatalogTypeRepository catalogTypeRepository;
    private final CatalogBrandRepository catalogBrandRepository;
    private final CatalogItemRepository catalogItemRepository;
    private final CatalogItemHiLoGenerator hiLoGenerator;
    private final HiLoSequenceGenerator sequenceGenerator;

    public DataInitializer(CatalogProperties catalogProperties,
                           CatalogTypeRepository catalogTypeRepository,
                           CatalogBrandRepository catalogBrandRepository,
                           CatalogItemRepository catalogItemRepository,
                           CatalogItemHiLoGenerator hiLoGenerator,
                           HiLoSequenceGenerator sequenceGenerator) {
        this.catalogProperties = catalogProperties;
        this.catalogTypeRepository = catalogTypeRepository;
        this.catalogBrandRepository = catalogBrandRepository;
        this.catalogItemRepository = catalogItemRepository;
        this.hiLoGenerator = hiLoGenerator;
        this.sequenceGenerator = sequenceGenerator;
    }

    @Override
    public void run(ApplicationArguments args) {
        if (!catalogProperties.isUseCustomizationData()) {
            log.info("Customization data loading is disabled; relying on Flyway seed data");
            return;
        }

        log.info("Loading customization data from CSV files");
        loadCatalogTypes();
        loadCatalogBrands();
        loadCatalogItems();
        log.info("Customization data loading complete");
    }

    private void loadCatalogTypes() {
        List<String> lines = readCsvLines("data/CatalogTypes.csv");
        if (lines.isEmpty()) {
            return;
        }

        List<CatalogType> types = new ArrayList<>();
        for (String line : lines) {
            String value = line.trim().replace("\"", "").trim();
            if (value.isEmpty()) {
                continue;
            }
            CatalogType type = new CatalogType();
            type.setId(sequenceGenerator.getNextValue("catalog_type_hilo"));
            type.setType(value);
            types.add(type);
        }

        catalogTypeRepository.saveAll(types);
        log.info("Loaded {} catalog types from CSV", types.size());
    }

    private void loadCatalogBrands() {
        List<String> lines = readCsvLines("data/CatalogBrands.csv");
        if (lines.isEmpty()) {
            return;
        }

        List<CatalogBrand> brands = new ArrayList<>();
        for (String line : lines) {
            String value = line.trim().replace("\"", "").trim();
            if (value.isEmpty()) {
                continue;
            }
            CatalogBrand brand = new CatalogBrand();
            brand.setId(sequenceGenerator.getNextValue("catalog_brand_hilo"));
            brand.setBrand(value);
            brands.add(brand);
        }

        catalogBrandRepository.saveAll(brands);
        log.info("Loaded {} catalog brands from CSV", brands.size());
    }

    private void loadCatalogItems() {
        List<String> lines = readCsvLines("data/CatalogItems.csv", false);
        if (lines.isEmpty()) {
            return;
        }

        String headerLine = lines.get(0);
        lines = lines.subList(1, lines.size());

        String[] headers = headerLine.toLowerCase().split(",");

        Map<String, Integer> catalogTypeIdLookup = catalogTypeRepository.findAll().stream()
                .collect(Collectors.toMap(CatalogType::getType, CatalogType::getId));
        Map<String, Integer> catalogBrandIdLookup = catalogBrandRepository.findAll().stream()
                .collect(Collectors.toMap(CatalogBrand::getBrand, CatalogBrand::getId));

        List<CatalogItem> items = new ArrayList<>();
        for (String line : lines) {
            String[] columns = CSV_SPLIT_PATTERN.split(line, -1);
            CatalogItem item = createCatalogItem(columns, headers, catalogTypeIdLookup, catalogBrandIdLookup);
            if (item != null) {
                int id = hiLoGenerator.getNextSequenceValue();
                item.setId(id);
                items.add(item);
            }
        }

        catalogItemRepository.saveAll(items);
        log.info("Loaded {} catalog items from CSV", items.size());
    }

    private CatalogItem createCatalogItem(String[] columns, String[] headers,
                                          Map<String, Integer> catalogTypeIdLookup,
                                          Map<String, Integer> catalogBrandIdLookup) {
        if (columns.length != headers.length) {
            log.warn("Skipping row: column count {} does not match header count {}", columns.length, headers.length);
            return null;
        }

        String catalogTypeName = getColumn(columns, headers, "catalogtypename");
        if (!catalogTypeIdLookup.containsKey(catalogTypeName)) {
            log.warn("Skipping row: type '{}' does not exist", catalogTypeName);
            return null;
        }

        String catalogBrandName = getColumn(columns, headers, "catalogbrandname");
        if (!catalogBrandIdLookup.containsKey(catalogBrandName)) {
            log.warn("Skipping row: brand '{}' does not exist", catalogBrandName);
            return null;
        }

        String priceString = getColumn(columns, headers, "price");
        BigDecimal price;
        try {
            price = new BigDecimal(priceString);
        } catch (NumberFormatException e) {
            log.warn("Skipping row: price '{}' is not a valid decimal", priceString);
            return null;
        }

        CatalogItem item = new CatalogItem();
        item.setCatalogTypeId(catalogTypeIdLookup.get(catalogTypeName));
        item.setCatalogBrandId(catalogBrandIdLookup.get(catalogBrandName));
        item.setDescription(getColumn(columns, headers, "description"));
        item.setName(getColumn(columns, headers, "name"));
        item.setPrice(price);
        item.setPictureFileName(getColumn(columns, headers, "picturefilename"));

        int availableStockIndex = indexOf(headers, "availablestock");
        if (availableStockIndex != -1) {
            String value = columns[availableStockIndex].trim().replace("\"", "").trim();
            if (!value.isEmpty()) {
                item.setAvailableStock(Integer.parseInt(value));
            }
        }

        int restockThresholdIndex = indexOf(headers, "restockthreshold");
        if (restockThresholdIndex != -1) {
            String value = columns[restockThresholdIndex].trim().replace("\"", "").trim();
            if (!value.isEmpty()) {
                item.setRestockThreshold(Integer.parseInt(value));
            }
        }

        int maxStockThresholdIndex = indexOf(headers, "maxstockthreshold");
        if (maxStockThresholdIndex != -1) {
            String value = columns[maxStockThresholdIndex].trim().replace("\"", "").trim();
            if (!value.isEmpty()) {
                item.setMaxStockThreshold(Integer.parseInt(value));
            }
        }

        int onReorderIndex = indexOf(headers, "onreorder");
        if (onReorderIndex != -1) {
            String value = columns[onReorderIndex].trim().replace("\"", "").trim();
            if (!value.isEmpty()) {
                item.setOnReorder(Boolean.parseBoolean(value));
            }
        }

        return item;
    }

    private String getColumn(String[] columns, String[] headers, String headerName) {
        int index = indexOf(headers, headerName);
        if (index == -1) {
            return "";
        }
        return columns[index].trim().replace("\"", "").trim();
    }

    private int indexOf(String[] headers, String headerName) {
        for (int i = 0; i < headers.length; i++) {
            if (headers[i].trim().equalsIgnoreCase(headerName)) {
                return i;
            }
        }
        return -1;
    }

    private List<String> readCsvLines(String resourcePath) {
        return readCsvLines(resourcePath, true);
    }

    private List<String> readCsvLines(String resourcePath, boolean skipHeader) {
        ClassPathResource resource = new ClassPathResource(resourcePath);
        if (!resource.exists()) {
            log.warn("CSV file not found: {}", resourcePath);
            return List.of();
        }

        try (BufferedReader reader = new BufferedReader(
                new InputStreamReader(resource.getInputStream(), StandardCharsets.UTF_8))) {
            List<String> allLines = reader.lines().collect(Collectors.toList());
            if (allLines.isEmpty()) {
                return List.of();
            }
            if (skipHeader) {
                return allLines.size() > 1 ? allLines.subList(1, allLines.size()) : List.of();
            }
            return allLines;
        } catch (IOException e) {
            log.error("Failed to read CSV file: {}", resourcePath, e);
            return List.of();
        }
    }
}
