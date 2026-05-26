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
    assertThat(result.getActualPage()).isZero();
    assertThat(result.getItemsPerPage()).isEqualTo(5);
    assertThat(result.getData()).hasSizeLessThanOrEqualTo(5);
    assertThat(result.getTotalItems()).isPositive();
  }

  @Test
  void getCatalogItemsPaginated_secondPage() {
    PaginatedItemsDto<CatalogItem> result = catalogServiceMock.getCatalogItemsPaginated(5, 1);

    assertThat(result.getActualPage()).isEqualTo(1);
  }

  @Test
  void findCatalogItem_existingItem_returnsItem() {
    PaginatedItemsDto<CatalogItem> paginated = catalogServiceMock.getCatalogItemsPaginated(100, 0);
    int firstItemId = paginated.getData().get(0).getId();

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
    long initialCount = catalogServiceMock.getCatalogItemsPaginated(100, 0).getTotalItems();

    CatalogItem newItem = new CatalogItem();
    newItem.setName("New Test Item");
    newItem.setPrice(new BigDecimal("49.99"));
    newItem.setCatalogBrandId(1);
    newItem.setCatalogTypeId(1);

    catalogServiceMock.createCatalogItem(newItem);

    long newCount = catalogServiceMock.getCatalogItemsPaginated(100, 0).getTotalItems();
    assertThat(newCount).isEqualTo(initialCount + 1);
    assertThat(newItem.getId()).isPositive();
  }

  @Test
  void updateCatalogItem_updatesExistingItem() {
    PaginatedItemsDto<CatalogItem> paginated = catalogServiceMock.getCatalogItemsPaginated(100, 0);
    CatalogItem existing = paginated.getData().get(0);
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
    long initialCount = paginated.getTotalItems();
    CatalogItem toRemove = paginated.getData().get(0);

    catalogServiceMock.removeCatalogItem(toRemove);

    long newCount = catalogServiceMock.getCatalogItemsPaginated(100, 0).getTotalItems();
    assertThat(newCount).isEqualTo(initialCount - 1);
    assertThat(catalogServiceMock.findCatalogItem(toRemove.getId())).isNull();
  }

  @Test
  void updateCatalogItem_nonExistingItem_noEffect() {
    long initialCount = catalogServiceMock.getCatalogItemsPaginated(100, 0).getTotalItems();

    CatalogItem nonExisting = new CatalogItem();
    nonExisting.setId(-999);
    nonExisting.setName("Non-existing");

    catalogServiceMock.updateCatalogItem(nonExisting);

    long newCount = catalogServiceMock.getCatalogItemsPaginated(100, 0).getTotalItems();
    assertThat(newCount).isEqualTo(initialCount);
  }

  @Test
  void getCatalogItemsPaginated_itemsHaveBrandAndTypeAssociations() {
    PaginatedItemsDto<CatalogItem> result = catalogServiceMock.getCatalogItemsPaginated(5, 0);

    for (CatalogItem item : result.getData()) {
      assertThat(item.getCatalogBrand()).isNotNull();
      assertThat(item.getCatalogType()).isNotNull();
    }
  }
}
