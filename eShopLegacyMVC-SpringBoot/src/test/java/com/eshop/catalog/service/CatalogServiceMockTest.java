package com.eshop.catalog.service;

import static org.assertj.core.api.Assertions.assertThat;

import com.eshop.catalog.dto.PaginatedItemsDto;
import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogType;
import com.eshop.catalog.service.impl.CatalogServiceMock;
import java.math.BigDecimal;
import java.util.List;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

class CatalogServiceMockTest {

  private CatalogServiceMock catalogServiceMock;

  @BeforeEach
  void setUp() {
    catalogServiceMock = new CatalogServiceMock();
  }

  @Test
  void getCatalogItemsPaginated_returnsFirstPage() {
    PaginatedItemsDto<CatalogItem> result = catalogServiceMock.getCatalogItemsPaginated(5, 0);

    assertThat(result).isNotNull();
    assertThat(result.actualPage()).isZero();
    assertThat(result.itemsPerPage()).isEqualTo(5);
    assertThat(result.data()).hasSizeLessThanOrEqualTo(5);
    assertThat(result.totalItems()).isPositive();
  }

  @Test
  void getCatalogItemsPaginated_secondPage() {
    PaginatedItemsDto<CatalogItem> result = catalogServiceMock.getCatalogItemsPaginated(5, 1);

    assertThat(result.actualPage()).isEqualTo(1);
  }

  @Test
  void findCatalogItem_existingItem_returnsItem() {
    PaginatedItemsDto<CatalogItem> paginated = catalogServiceMock.getCatalogItemsPaginated(100, 0);
    int firstItemId = paginated.data().get(0).getId();

    CatalogItem result = catalogServiceMock.findCatalogItem(firstItemId);

    assertThat(result).isNotNull();
    assertThat(result.getId()).isEqualTo(firstItemId);
  }

  @Test
  void findCatalogItem_nonExistingItem_returnsNull() {
    CatalogItem result = catalogServiceMock.findCatalogItem(-1);

    assertThat(result).isNull();
  }

  @Test
  void getCatalogTypes_returnsTypes() {
    List<CatalogType> types = catalogServiceMock.getCatalogTypes();

    assertThat(types).isNotEmpty();
  }

  @Test
  void getCatalogBrands_returnsBrands() {
    List<CatalogBrand> brands = catalogServiceMock.getCatalogBrands();

    assertThat(brands).isNotEmpty();
  }

  @Test
  void createCatalogItem_addsItemWithNewId() {
    long initialCount = catalogServiceMock.getCatalogItemsPaginated(100, 0).totalItems();

    CatalogItem newItem = new CatalogItem();
    newItem.setName("New Test Item");
    newItem.setPrice(new BigDecimal("49.99"));
    newItem.setCatalogBrandId(1);
    newItem.setCatalogTypeId(1);

    catalogServiceMock.createCatalogItem(newItem);

    long newCount = catalogServiceMock.getCatalogItemsPaginated(100, 0).totalItems();
    assertThat(newCount).isEqualTo(initialCount + 1);
    assertThat(newItem.getId()).isPositive();
  }

  @Test
  void updateCatalogItem_updatesExistingItem() {
    PaginatedItemsDto<CatalogItem> paginated = catalogServiceMock.getCatalogItemsPaginated(100, 0);
    CatalogItem existing = paginated.data().get(0);
    int existingId = existing.getId();

    CatalogItem updated = new CatalogItem();
    updated.setId(existingId);
    updated.setName("Updated Name");
    updated.setPrice(new BigDecimal("99.99"));

    catalogServiceMock.updateCatalogItem(updated);

    CatalogItem found = catalogServiceMock.findCatalogItem(existingId);
    assertThat(found.getName()).isEqualTo("Updated Name");
  }

  @Test
  void removeCatalogItem_removesItem() {
    PaginatedItemsDto<CatalogItem> paginated = catalogServiceMock.getCatalogItemsPaginated(100, 0);
    long initialCount = paginated.totalItems();
    CatalogItem toRemove = paginated.data().get(0);

    catalogServiceMock.removeCatalogItem(toRemove);

    long newCount = catalogServiceMock.getCatalogItemsPaginated(100, 0).totalItems();
    assertThat(newCount).isEqualTo(initialCount - 1);
    assertThat(catalogServiceMock.findCatalogItem(toRemove.getId())).isNull();
  }

  @Test
  void updateCatalogItem_nonExistingItem_noEffect() {
    long initialCount = catalogServiceMock.getCatalogItemsPaginated(100, 0).totalItems();

    CatalogItem nonExisting = new CatalogItem();
    nonExisting.setId(-999);
    nonExisting.setName("Non-existing");

    catalogServiceMock.updateCatalogItem(nonExisting);

    long newCount = catalogServiceMock.getCatalogItemsPaginated(100, 0).totalItems();
    assertThat(newCount).isEqualTo(initialCount);
  }

  @Test
  void getCatalogItemsPaginated_itemsHaveBrandAndTypeAssociations() {
    PaginatedItemsDto<CatalogItem> result = catalogServiceMock.getCatalogItemsPaginated(5, 0);

    for (CatalogItem item : result.data()) {
      assertThat(item.getCatalogBrand()).isNotNull();
      assertThat(item.getCatalogType()).isNotNull();
    }
  }

  @Test
  void getCatalogItemsPaginated_beyondLastPage_returnsEmptyData() {
    PaginatedItemsDto<CatalogItem> result = catalogServiceMock.getCatalogItemsPaginated(5, 100);

    assertThat(result.data()).isEmpty();
    assertThat(result.actualPage()).isEqualTo(100);
  }

  @Test
  void getCatalogTypes_returnsKnownTypes() {
    List<CatalogType> types = catalogServiceMock.getCatalogTypes();

    assertThat(types).hasSizeGreaterThanOrEqualTo(4);
    assertThat(types).extracting(CatalogType::getType).contains("Mug", "T-Shirt");
  }

  @Test
  void getCatalogBrands_returnsKnownBrands() {
    List<CatalogBrand> brands = catalogServiceMock.getCatalogBrands();

    assertThat(brands).hasSizeGreaterThanOrEqualTo(5);
    assertThat(brands).extracting(CatalogBrand::getBrand).contains("Azure", ".NET");
  }

  @Test
  void removeCatalogItem_nonExistingItem_noEffect() {
    long initialCount = catalogServiceMock.getCatalogItemsPaginated(100, 0).totalItems();

    CatalogItem nonExisting = new CatalogItem();
    nonExisting.setId(-999);

    catalogServiceMock.removeCatalogItem(nonExisting);

    long newCount = catalogServiceMock.getCatalogItemsPaginated(100, 0).totalItems();
    assertThat(newCount).isEqualTo(initialCount);
  }

  @Test
  void createCatalogItem_assignsIdGreaterThanExistingMax() {
    PaginatedItemsDto<CatalogItem> paginated = catalogServiceMock.getCatalogItemsPaginated(100, 0);
    int maxId = paginated.data().stream().mapToInt(CatalogItem::getId).max().orElse(0);

    CatalogItem newItem = new CatalogItem();
    newItem.setName("Brand New Item");
    newItem.setPrice(new BigDecimal("25.00"));

    catalogServiceMock.createCatalogItem(newItem);

    assertThat(newItem.getId()).isGreaterThan(maxId);
  }
}
