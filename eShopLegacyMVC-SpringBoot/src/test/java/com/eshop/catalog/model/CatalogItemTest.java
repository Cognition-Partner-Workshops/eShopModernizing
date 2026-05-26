package com.eshop.catalog.model;

import jakarta.validation.ConstraintViolation;
import jakarta.validation.Validation;
import jakarta.validation.Validator;
import jakarta.validation.ValidatorFactory;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;
import java.util.Set;

import static org.assertj.core.api.Assertions.assertThat;

class CatalogItemTest {

    private static Validator validator;

    @BeforeAll
    static void setUpValidator() {
        try (ValidatorFactory factory = Validation.buildDefaultValidatorFactory()) {
            validator = factory.getValidator();
        }
    }

    @Test
    void defaultConstructorSetsPictureFileName() {
        CatalogItem item = new CatalogItem();
        assertThat(item.getPictureFileName()).isEqualTo("dummy.png");
    }

    @Test
    void validItemHasNoViolations() {
        CatalogItem item = new CatalogItem();
        item.setName("Test Item");
        item.setPrice(new BigDecimal("19.99"));

        Set<ConstraintViolation<CatalogItem>> violations = validator.validate(item);
        assertThat(violations).isEmpty();
    }

    @Test
    void blankNameCausesViolation() {
        CatalogItem item = new CatalogItem();
        item.setName("  ");
        item.setPrice(new BigDecimal("10.00"));

        Set<ConstraintViolation<CatalogItem>> violations = validator.validate(item);
        assertThat(violations).isNotEmpty();
        assertThat(violations).anyMatch(v -> v.getPropertyPath().toString().equals("name"));
    }

    @Test
    void nullPriceCausesViolation() {
        CatalogItem item = new CatalogItem();
        item.setName("Test Item");
        item.setPrice(null);

        Set<ConstraintViolation<CatalogItem>> violations = validator.validate(item);
        assertThat(violations).isNotEmpty();
        assertThat(violations).anyMatch(v -> v.getPropertyPath().toString().equals("price"));
    }

    @Test
    void negativePriceCausesViolation() {
        CatalogItem item = new CatalogItem();
        item.setName("Test Item");
        item.setPrice(new BigDecimal("-1.00"));

        Set<ConstraintViolation<CatalogItem>> violations = validator.validate(item);
        assertThat(violations).isNotEmpty();
        assertThat(violations).anyMatch(v -> v.getPropertyPath().toString().equals("price"));
    }

    @Test
    void priceExceedingMaxCausesViolation() {
        CatalogItem item = new CatalogItem();
        item.setName("Test Item");
        item.setPrice(new BigDecimal("1000001.00"));

        Set<ConstraintViolation<CatalogItem>> violations = validator.validate(item);
        assertThat(violations).isNotEmpty();
        assertThat(violations).anyMatch(v -> v.getPropertyPath().toString().equals("price"));
    }

    @Test
    void priceWithTooManyDecimalsCausesViolation() {
        CatalogItem item = new CatalogItem();
        item.setName("Test Item");
        item.setPrice(new BigDecimal("10.999"));

        Set<ConstraintViolation<CatalogItem>> violations = validator.validate(item);
        assertThat(violations).isNotEmpty();
        assertThat(violations).anyMatch(v -> v.getPropertyPath().toString().equals("price"));
    }

    @Test
    void settersAndGettersWork() {
        CatalogItem item = new CatalogItem();
        item.setId(1);
        item.setName("Widget");
        item.setDescription("A fine widget");
        item.setPrice(new BigDecimal("29.95"));
        item.setPictureFileName("widget.png");
        item.setPictureUri("/pics/widget.png");
        item.setCatalogTypeId(2);
        item.setCatalogBrandId(3);
        item.setAvailableStock(100);
        item.setRestockThreshold(10);
        item.setMaxStockThreshold(500);
        item.setOnReorder(true);

        CatalogBrand brand = new CatalogBrand();
        brand.setId(3);
        brand.setBrand("Acme");
        item.setCatalogBrand(brand);

        CatalogType type = new CatalogType();
        type.setId(2);
        type.setType("Gadget");
        item.setCatalogType(type);

        assertThat(item.getId()).isEqualTo(1);
        assertThat(item.getName()).isEqualTo("Widget");
        assertThat(item.getDescription()).isEqualTo("A fine widget");
        assertThat(item.getPrice()).isEqualByComparingTo("29.95");
        assertThat(item.getPictureFileName()).isEqualTo("widget.png");
        assertThat(item.getPictureUri()).isEqualTo("/pics/widget.png");
        assertThat(item.getCatalogTypeId()).isEqualTo(2);
        assertThat(item.getCatalogBrandId()).isEqualTo(3);
        assertThat(item.getAvailableStock()).isEqualTo(100);
        assertThat(item.getRestockThreshold()).isEqualTo(10);
        assertThat(item.getMaxStockThreshold()).isEqualTo(500);
        assertThat(item.isOnReorder()).isTrue();
        assertThat(item.getCatalogBrand()).isEqualTo(brand);
        assertThat(item.getCatalogType()).isEqualTo(type);
    }
}
