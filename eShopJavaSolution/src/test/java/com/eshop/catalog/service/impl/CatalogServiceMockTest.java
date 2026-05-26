package com.eshop.catalog.service.impl;

import static org.assertj.core.api.Assertions.assertThat;

import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogType;
import com.eshop.catalog.service.CatalogService;
import java.math.BigDecimal;
import java.util.List;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.data.domain.Page;

class CatalogServiceMockTest {

  private CatalogService service;

  @BeforeEach
  void setUp() {
    service = new CatalogServiceMock();
  }

  @Test
  void implementsCatalogServiceInterface() {
    assertThat(service).isInstanceOf(CatalogService.class);
  }

  @Test
  void getCatalogItemsPaginatedReturnsFirstPage() {
    Page<CatalogItem> result = service.getCatalogItemsPaginated(5, 0);

    assertThat(result.getNumber()).isZero();
    assertThat(result.getSize()).isEqualTo(5);
    assertThat(result.getTotalElements()).isEqualTo(12);
    assertThat(result.getContent()).hasSize(5);
  }

  @Test
  void getCatalogItemsPaginatedReturnsSecondPage() {
    Page<CatalogItem> result = service.getCatalogItemsPaginated(5, 1);

    assertThat(result.getNumber()).isEqualTo(1);
    assertThat(result.getContent()).hasSize(5);
  }

  @Test
  void getCatalogItemsPaginatedReturnsLastPage() {
    Page<CatalogItem> result = service.getCatalogItemsPaginated(5, 2);

    assertThat(result.getContent()).hasSize(2);
  }

  @Test
  void getCatalogItemsPaginatedItemsSortedById() {
    Page<CatalogItem> result = service.getCatalogItemsPaginated(12, 0);

    List<CatalogItem> data = result.getContent();
    for (int i = 1; i < data.size(); i++) {
      assertThat(data.get(i).getId()).isGreaterThan(data.get(i - 1).getId());
    }
  }

  @Test
  void getCatalogItemsPaginatedComposesAssociations() {
    Page<CatalogItem> result = service.getCatalogItemsPaginated(12, 0);

    for (CatalogItem item : result.getContent()) {
      assertThat(item.getCatalogBrand()).isNotNull();
      assertThat(item.getCatalogType()).isNotNull();
    }
  }

  @Test
  void findCatalogItemReturnsItemById() {
    CatalogItem item = service.findCatalogItem(1);

    assertThat(item).isNotNull();
    assertThat(item.getId()).isEqualTo(1);
    assertThat(item.getName()).isEqualTo(".NET Bot Black Hoodie");
  }

  @Test
  void findCatalogItemReturnsComposedAssociations() {
    CatalogItem item = service.findCatalogItem(1);

    assertThat(item).isNotNull();
    assertThat(item.getCatalogBrand()).isNotNull();
    assertThat(item.getCatalogType()).isNotNull();
  }

  @Test
  void findCatalogItemReturnsNullForUnknownId() {
    assertThat(service.findCatalogItem(999)).isNull();
  }

  @Test
  void getCatalogTypesReturnsPreconfiguredTypes() {
    List<CatalogType> types = service.getCatalogTypes();

    assertThat(types).hasSize(4);
    assertThat(types).extracting(CatalogType::getType).contains("Mug", "T-Shirt", "Sheet");
  }

  @Test
  void getCatalogBrandsReturnsPreconfiguredBrands() {
    List<CatalogBrand> brands = service.getCatalogBrands();

    assertThat(brands).hasSize(5);
    assertThat(brands).extracting(CatalogBrand::getBrand).contains("Azure", ".NET", "Other");
  }

  @Test
  void createCatalogItemAssignsNextId() {
    CatalogItem newItem = new CatalogItem();
    newItem.setName("New Item");
    newItem.setPrice(new BigDecimal("10.00"));
    newItem.setCatalogTypeId(1);
    newItem.setCatalogBrandId(1);

    service.createCatalogItem(newItem);

    assertThat(newItem.getId()).isEqualTo(13);
    assertThat(service.findCatalogItem(13)).isNotNull();
  }

  @Test
  void updateCatalogItemReplacesExistingItem() {
    CatalogItem original = service.findCatalogItem(1);
    assertThat(original).isNotNull();

    CatalogItem updated = new CatalogItem();
    updated.setId(1);
    updated.setName("Updated Name");
    updated.setPrice(new BigDecimal("99.99"));
    updated.setCatalogTypeId(original.getCatalogTypeId());
    updated.setCatalogBrandId(original.getCatalogBrandId());

    service.updateCatalogItem(updated);

    CatalogItem result = service.findCatalogItem(1);
    assertThat(result.getName()).isEqualTo("Updated Name");
    assertThat(result.getPrice()).isEqualByComparingTo(new BigDecimal("99.99"));
  }

  @Test
  void removeCatalogItemDeletesFromList() {
    CatalogItem item = service.findCatalogItem(1);
    assertThat(item).isNotNull();

    service.removeCatalogItem(item);

    assertThat(service.findCatalogItem(1)).isNull();
  }

  @Test
  void totalPagesCalculation() {
    Page<CatalogItem> result = service.getCatalogItemsPaginated(5, 0);

    assertThat(result.getTotalPages()).isEqualTo(3);
  }
}
