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
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.ApplicationArguments;
import org.springframework.boot.ApplicationRunner;
import org.springframework.core.io.ClassPathResource;
import org.springframework.stereotype.Component;

@Component
public class DataInitializer implements ApplicationRunner {

  private static final Logger log = LoggerFactory.getLogger(DataInitializer.class);
  private static final Pattern CSV_SPLIT = Pattern.compile(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

  private final CatalogTypeRepository catalogTypeRepository;
  private final CatalogBrandRepository catalogBrandRepository;
  private final CatalogItemRepository catalogItemRepository;
  private final boolean useCustomizationData;

  public DataInitializer(
      CatalogTypeRepository catalogTypeRepository,
      CatalogBrandRepository catalogBrandRepository,
      CatalogItemRepository catalogItemRepository,
      @Value("${app.use-customization-data:false}") boolean useCustomizationData) {
    this.catalogTypeRepository = catalogTypeRepository;
    this.catalogBrandRepository = catalogBrandRepository;
    this.catalogItemRepository = catalogItemRepository;
    this.useCustomizationData = useCustomizationData;
  }

  @Override
  public void run(ApplicationArguments args) {
    if (!useCustomizationData) {
      log.info("Customization data loading disabled (app.use-customization-data=false)");
      return;
    }

    if (catalogTypeRepository.count() > 0
        || catalogBrandRepository.count() > 0
        || catalogItemRepository.count() > 0) {
      log.info("Database already contains data — skipping CSV import");
      return;
    }

    log.info("Loading customization data from CSV files");
    loadCatalogTypes();
    loadCatalogBrands();
    loadCatalogItems();
    log.info("CSV data import complete");
  }

  private void loadCatalogTypes() {
    List<String> lines = readCsvLines("data/CatalogTypes.csv");
    if (lines.isEmpty()) {
      return;
    }

    for (String line : lines) {
      String typeName = line.trim().replaceAll("^\"|\"$", "").trim();
      if (typeName.isEmpty()) {
        throw new IllegalArgumentException("Catalog Type name is empty");
      }
      CatalogType ct = new CatalogType();
      ct.setType(typeName);
      catalogTypeRepository.save(ct);
    }
    log.info("Loaded {} catalog types from CSV", lines.size());
  }

  private void loadCatalogBrands() {
    List<String> lines = readCsvLines("data/CatalogBrands.csv");
    if (lines.isEmpty()) {
      return;
    }

    for (String line : lines) {
      String brandName = line.trim().replaceAll("^\"|\"$", "").trim();
      if (brandName.isEmpty()) {
        throw new IllegalArgumentException("Catalog Brand name is empty");
      }
      CatalogBrand cb = new CatalogBrand();
      cb.setBrand(brandName);
      catalogBrandRepository.save(cb);
    }
    log.info("Loaded {} catalog brands from CSV", lines.size());
  }

  private void loadCatalogItems() {
    List<String> lines = readCsvLines("data/CatalogItems.csv");
    if (lines.isEmpty()) {
      return;
    }

    Map<String, Integer> typeIdLookup =
        catalogTypeRepository.findAll().stream()
            .collect(Collectors.toMap(CatalogType::getType, CatalogType::getId));
    Map<String, Integer> brandIdLookup =
        catalogBrandRepository.findAll().stream()
            .collect(Collectors.toMap(CatalogBrand::getBrand, CatalogBrand::getId));

    String headerLine = readCsvHeader("data/CatalogItems.csv");
    if (headerLine == null) {
      return;
    }
    String[] headers = headerLine.toLowerCase().split(",");

    for (String line : lines) {
      String[] columns = CSV_SPLIT.split(line, -1);
      if (columns.length != headers.length) {
        throw new IllegalArgumentException(
            "Column count " + columns.length + " does not match header count " + headers.length);
      }

      String catalogTypeName = cleanField(columns, headers, "catalogtypename");
      if (!typeIdLookup.containsKey(catalogTypeName)) {
        throw new IllegalArgumentException(
            "Type '" + catalogTypeName + "' not found in CatalogTypes");
      }

      String catalogBrandName = cleanField(columns, headers, "catalogbrandname");
      if (!brandIdLookup.containsKey(catalogBrandName)) {
        throw new IllegalArgumentException(
            "Brand '" + catalogBrandName + "' not found in CatalogBrands");
      }

      String priceStr = cleanField(columns, headers, "price");
      BigDecimal price;
      try {
        price = new BigDecimal(priceStr);
      } catch (NumberFormatException e) {
        throw new IllegalArgumentException("price=" + priceStr + " is not a valid decimal number");
      }

      CatalogItem item = new CatalogItem();
      item.setCatalogTypeId(typeIdLookup.get(catalogTypeName));
      item.setCatalogBrandId(brandIdLookup.get(catalogBrandName));
      item.setDescription(cleanField(columns, headers, "description"));
      item.setName(cleanField(columns, headers, "name"));
      item.setPrice(price);
      item.setPictureFileName(cleanField(columns, headers, "picturefilename"));

      setOptionalInt(columns, headers, "availablestock", item::setAvailableStock);
      setOptionalInt(columns, headers, "restockthreshold", item::setRestockThreshold);
      setOptionalInt(columns, headers, "maxstockthreshold", item::setMaxStockThreshold);
      setOptionalBoolean(columns, headers, "onreorder", item::setOnReorder);

      catalogItemRepository.save(item);
    }
    log.info("Loaded {} catalog items from CSV", lines.size());
  }

  private String cleanField(String[] columns, String[] headers, String headerName) {
    int idx = indexOf(headers, headerName);
    if (idx == -1) {
      throw new IllegalArgumentException("Required header '" + headerName + "' not found");
    }
    return columns[idx].trim().replaceAll("^\"|\"$", "").trim();
  }

  private void setOptionalInt(
      String[] columns,
      String[] headers,
      String headerName,
      java.util.function.IntConsumer setter) {
    int idx = indexOf(headers, headerName);
    if (idx == -1) {
      return;
    }
    String value = columns[idx].trim().replaceAll("^\"|\"$", "").trim();
    if (!value.isEmpty()) {
      try {
        setter.accept(Integer.parseInt(value));
      } catch (NumberFormatException e) {
        throw new IllegalArgumentException(headerName + "=" + value + " is not a valid integer");
      }
    }
  }

  private void setOptionalBoolean(
      String[] columns,
      String[] headers,
      String headerName,
      java.util.function.Consumer<Boolean> setter) {
    int idx = indexOf(headers, headerName);
    if (idx == -1) {
      return;
    }
    String value = columns[idx].trim().replaceAll("^\"|\"$", "").trim();
    if (!value.isEmpty()) {
      setter.accept(Boolean.parseBoolean(value));
    }
  }

  private int indexOf(String[] headers, String name) {
    for (int i = 0; i < headers.length; i++) {
      if (headers[i].trim().equals(name)) {
        return i;
      }
    }
    return -1;
  }

  private String readCsvHeader(String resourcePath) {
    try {
      ClassPathResource resource = new ClassPathResource(resourcePath);
      try (BufferedReader reader =
          new BufferedReader(
              new InputStreamReader(resource.getInputStream(), StandardCharsets.UTF_8))) {
        return reader.readLine();
      }
    } catch (IOException e) {
      log.warn("Could not read CSV header from {}: {}", resourcePath, e.getMessage());
      return null;
    }
  }

  private List<String> readCsvLines(String resourcePath) {
    try {
      ClassPathResource resource = new ClassPathResource(resourcePath);
      try (BufferedReader reader =
          new BufferedReader(
              new InputStreamReader(resource.getInputStream(), StandardCharsets.UTF_8))) {
        List<String> allLines = reader.lines().collect(Collectors.toList());
        if (allLines.size() <= 1) {
          return new ArrayList<>();
        }
        return allLines.subList(1, allLines.size());
      }
    } catch (IOException e) {
      log.warn("CSV file {} not found on classpath — skipping", resourcePath);
      return new ArrayList<>();
    }
  }
}
