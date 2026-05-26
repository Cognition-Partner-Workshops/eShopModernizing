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
import java.util.Arrays;
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
import org.springframework.transaction.annotation.Transactional;

@Component
public class DataInitializer implements ApplicationRunner {

  private static final Logger log = LoggerFactory.getLogger(DataInitializer.class);

  private static final Pattern CSV_SPLIT = Pattern.compile(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

  private static final String TYPES_CSV = "data/CatalogTypes.csv";
  private static final String BRANDS_CSV = "data/CatalogBrands.csv";
  private static final String ITEMS_CSV = "data/CatalogItems.csv";

  private final CatalogProperties properties;
  private final CatalogTypeRepository typeRepository;
  private final CatalogBrandRepository brandRepository;
  private final CatalogItemRepository itemRepository;

  public DataInitializer(
      CatalogProperties properties,
      CatalogTypeRepository typeRepository,
      CatalogBrandRepository brandRepository,
      CatalogItemRepository itemRepository) {
    this.properties = properties;
    this.typeRepository = typeRepository;
    this.brandRepository = brandRepository;
    this.itemRepository = itemRepository;
  }

  @Override
  @Transactional
  public void run(ApplicationArguments args) {
    if (!properties.useCustomizationData()) {
      log.info("UseCustomizationData is disabled — relying on Flyway seed data");
      return;
    }

    log.info("UseCustomizationData is enabled — loading catalog data from CSV files");

    itemRepository.deleteAllInBatch();
    brandRepository.deleteAllInBatch();
    typeRepository.deleteAllInBatch();

    List<CatalogType> types = loadCatalogTypes();
    List<CatalogBrand> brands = loadCatalogBrands();
    loadCatalogItems(types, brands);

    log.info(
        "CSV data loading complete — {} types, {} brands, {} items",
        types.size(),
        brands.size(),
        itemRepository.count());
  }

  private List<CatalogType> loadCatalogTypes() {
    List<String> lines = readCsvLines(TYPES_CSV);
    if (lines.isEmpty()) {
      return List.of();
    }

    String[] headers = parseHeaders(lines.get(0));
    validateRequiredHeaders(headers, new String[] {"catalogtype"}, TYPES_CSV);

    List<CatalogType> types = new ArrayList<>();
    for (int i = 1; i < lines.size(); i++) {
      String line = lines.get(i).trim();
      if (line.isEmpty()) {
        continue;
      }
      String value = stripQuotes(line);
      if (value.isEmpty()) {
        throw new CsvParseException("Catalog type name is empty at line " + (i + 1));
      }
      types.add(new CatalogType(value));
    }

    return typeRepository.saveAll(types);
  }

  private List<CatalogBrand> loadCatalogBrands() {
    List<String> lines = readCsvLines(BRANDS_CSV);
    if (lines.isEmpty()) {
      return List.of();
    }

    String[] headers = parseHeaders(lines.get(0));
    validateRequiredHeaders(headers, new String[] {"catalogbrand"}, BRANDS_CSV);

    List<CatalogBrand> brands = new ArrayList<>();
    for (int i = 1; i < lines.size(); i++) {
      String line = lines.get(i).trim();
      if (line.isEmpty()) {
        continue;
      }
      String value = stripQuotes(line);
      if (value.isEmpty()) {
        throw new CsvParseException("Catalog brand name is empty at line " + (i + 1));
      }
      brands.add(new CatalogBrand(value));
    }

    return brandRepository.saveAll(brands);
  }

  private void loadCatalogItems(List<CatalogType> types, List<CatalogBrand> brands) {
    List<String> lines = readCsvLines(ITEMS_CSV);
    if (lines.isEmpty()) {
      return;
    }

    String[] headers = parseHeaders(lines.get(0));
    String[] requiredHeaders = {
      "catalogtypename", "catalogbrandname", "description", "name", "price", "picturefilename"
    };
    validateRequiredHeaders(headers, requiredHeaders, ITEMS_CSV);

    Map<String, CatalogType> typesByName =
        types.stream().collect(Collectors.toMap(CatalogType::getType, t -> t));
    Map<String, CatalogBrand> brandsByName =
        brands.stream().collect(Collectors.toMap(CatalogBrand::getBrand, b -> b));

    List<CatalogItem> items = new ArrayList<>();
    int nextId = 1;

    for (int i = 1; i < lines.size(); i++) {
      String line = lines.get(i).trim();
      if (line.isEmpty()) {
        continue;
      }

      String[] columns = CSV_SPLIT.split(line, -1);
      if (columns.length != headers.length) {
        throw new CsvParseException(
            "Column count "
                + columns.length
                + " does not match header count "
                + headers.length
                + " at line "
                + (i + 1));
      }

      CatalogItem item = createCatalogItem(columns, headers, typesByName, brandsByName, i + 1);
      item.setId(nextId++);
      items.add(item);
    }

    itemRepository.saveAll(items);
  }

  private CatalogItem createCatalogItem(
      String[] columns,
      String[] headers,
      Map<String, CatalogType> typesByName,
      Map<String, CatalogBrand> brandsByName,
      int lineNumber) {

    String typeName = columnValue(columns, headers, "catalogtypename");
    CatalogType type = typesByName.get(typeName);
    if (type == null) {
      throw new CsvParseException(
          "Catalog type '" + typeName + "' not found at line " + lineNumber);
    }

    String brandName = columnValue(columns, headers, "catalogbrandname");
    CatalogBrand brand = brandsByName.get(brandName);
    if (brand == null) {
      throw new CsvParseException(
          "Catalog brand '" + brandName + "' not found at line " + lineNumber);
    }

    String priceStr = columnValue(columns, headers, "price");
    BigDecimal price;
    try {
      price = new BigDecimal(priceStr);
    } catch (NumberFormatException e) {
      throw new CsvParseException("Invalid price '" + priceStr + "' at line " + lineNumber);
    }

    CatalogItem item = new CatalogItem();
    item.setName(columnValue(columns, headers, "name"));
    item.setDescription(columnValue(columns, headers, "description"));
    item.setPrice(price);
    item.setPictureFileName(columnValue(columns, headers, "picturefilename"));
    item.setCatalogType(type);
    item.setCatalogBrand(brand);

    parseOptionalInt(columns, headers, "availablestock", lineNumber)
        .ifPresent(item::setAvailableStock);
    parseOptionalInt(columns, headers, "restockthreshold", lineNumber)
        .ifPresent(item::setRestockThreshold);
    parseOptionalInt(columns, headers, "maxstockthreshold", lineNumber)
        .ifPresent(item::setMaxStockThreshold);
    parseOptionalBoolean(columns, headers, "onreorder", lineNumber).ifPresent(item::setOnReorder);

    return item;
  }

  private java.util.Optional<Integer> parseOptionalInt(
      String[] columns, String[] headers, String header, int lineNumber) {
    int idx = indexOf(headers, header);
    if (idx == -1) {
      return java.util.Optional.empty();
    }
    String value = stripQuotes(columns[idx]);
    if (value.isEmpty()) {
      return java.util.Optional.empty();
    }
    try {
      return java.util.Optional.of(Integer.parseInt(value));
    } catch (NumberFormatException e) {
      throw new CsvParseException(
          "Invalid integer for " + header + " '" + value + "' at line " + lineNumber);
    }
  }

  private java.util.Optional<Boolean> parseOptionalBoolean(
      String[] columns, String[] headers, String header, int lineNumber) {
    int idx = indexOf(headers, header);
    if (idx == -1) {
      return java.util.Optional.empty();
    }
    String value = stripQuotes(columns[idx]);
    if (value.isEmpty()) {
      return java.util.Optional.empty();
    }
    if ("true".equalsIgnoreCase(value) || "false".equalsIgnoreCase(value)) {
      return java.util.Optional.of(Boolean.parseBoolean(value));
    }
    throw new CsvParseException(
        "Invalid boolean for " + header + " '" + value + "' at line " + lineNumber);
  }

  private String columnValue(String[] columns, String[] headers, String header) {
    int idx = indexOf(headers, header);
    if (idx == -1) {
      throw new CsvParseException("Required header '" + header + "' not found");
    }
    return stripQuotes(columns[idx]);
  }

  private static int indexOf(String[] headers, String header) {
    for (int i = 0; i < headers.length; i++) {
      if (headers[i].equals(header)) {
        return i;
      }
    }
    return -1;
  }

  private static String[] parseHeaders(String headerLine) {
    return Arrays.stream(headerLine.toLowerCase().split(","))
        .map(String::trim)
        .toArray(String[]::new);
  }

  private static void validateRequiredHeaders(
      String[] headers, String[] required, String fileName) {
    List<String> headerList = Arrays.asList(headers);
    for (String req : required) {
      if (!headerList.contains(req)) {
        throw new CsvParseException(
            "CSV file " + fileName + " is missing required header '" + req + "'");
      }
    }
  }

  private static String stripQuotes(String value) {
    String trimmed = value.trim();
    if (trimmed.length() >= 2 && trimmed.startsWith("\"") && trimmed.endsWith("\"")) {
      trimmed = trimmed.substring(1, trimmed.length() - 1);
    }
    return trimmed.trim();
  }

  private static List<String> readCsvLines(String resourcePath) {
    ClassPathResource resource = new ClassPathResource(resourcePath);
    if (!resource.exists()) {
      log.warn("CSV file not found on classpath: {}", resourcePath);
      return List.of();
    }
    try (BufferedReader reader =
        new BufferedReader(
            new InputStreamReader(resource.getInputStream(), StandardCharsets.UTF_8))) {
      return reader.lines().collect(Collectors.toList());
    } catch (IOException e) {
      throw new CsvParseException("Failed to read CSV file: " + resourcePath, e);
    }
  }

  static class CsvParseException extends RuntimeException {
    CsvParseException(String message) {
      super(message);
    }

    CsvParseException(String message, Throwable cause) {
      super(message, cause);
    }
  }
}
