package com.eshop.catalog.service;

import java.math.BigDecimal;
import java.util.List;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import com.eshop.catalog.domain.entity.CatalogBrand;
import com.eshop.catalog.domain.entity.CatalogItem;
import com.eshop.catalog.domain.entity.CatalogType;
import com.eshop.catalog.dto.PaginatedItemsDto;
import com.eshop.catalog.util.PreconfiguredData;

import static org.assertj.core.api.Assertions.assertThat;

class CatalogServiceMockTest {

    private CatalogServiceMock catalogService;

    @BeforeEach
    void setUp() {
        catalogService = new CatalogServiceMock();
    }

    // --- findCatalogItem ---

    @Test
    void findCatalogItem_existingId_returnsItem() {
        CatalogItem result = catalogService.findCatalogItem(1);

        assertThat(result).isNotNull();
        assertThat(result.getId()).isEqualTo(1);
        assertThat(result.getName()).isEqualTo(".NET Bot Black Hoodie");
    }

    @Test
    void findCatalogItem_unknownId_returnsNull() {
        CatalogItem result = catalogService.findCatalogItem(9999);

        assertThat(result).isNull();
    }

    @Test
    void findCatalogItem_composedWithBrandAndType() {
        CatalogItem result = catalogService.findCatalogItem(1);

        assertThat(result).isNotNull();
        assertThat(result.getCatalogBrand()).isNotNull();
        assertThat(result.getCatalogBrand().getBrand()).isEqualTo(".NET");
        assertThat(result.getCatalogType()).isNotNull();
        assertThat(result.getCatalogType().getType()).isEqualTo("T-Shirt");
    }

    // --- getCatalogBrands ---

    @Test
    void getCatalogBrands_returnsAllBrands() {
        List<CatalogBrand> brands = catalogService.getCatalogBrands();

        assertThat(brands).hasSize(5);
        assertThat(brands).extracting(CatalogBrand::getBrand)
                .containsExactly("Azure", ".NET", "Visual Studio", "SQL Server", "Other");
    }

    @Test
    void getCatalogBrands_returnsDefensiveCopy() {
        List<CatalogBrand> first = catalogService.getCatalogBrands();
        List<CatalogBrand> second = catalogService.getCatalogBrands();

        assertThat(first).isNotSameAs(second);
        assertThat(first).hasSize(second.size());
    }

    // --- getCatalogTypes ---

    @Test
    void getCatalogTypes_returnsAllTypes() {
        List<CatalogType> types = catalogService.getCatalogTypes();

        assertThat(types).hasSize(4);
        assertThat(types).extracting(CatalogType::getType)
                .containsExactly("Mug", "T-Shirt", "Sheet", "USB Memory Stick");
    }

    @Test
    void getCatalogTypes_returnsDefensiveCopy() {
        List<CatalogType> first = catalogService.getCatalogTypes();
        List<CatalogType> second = catalogService.getCatalogTypes();

        assertThat(first).isNotSameAs(second);
        assertThat(first).hasSize(second.size());
    }

    // --- getCatalogItemsPaginated ---

    @Test
    void getCatalogItemsPaginated_firstPage_returnsCorrectSlice() {
        PaginatedItemsDto<CatalogItem> result = catalogService.getCatalogItemsPaginated(5, 0);

        assertThat(result.pageIndex()).isZero();
        assertThat(result.pageSize()).isEqualTo(5);
        assertThat(result.count()).isEqualTo(12);
        assertThat(result.data()).hasSize(5);
    }

    @Test
    void getCatalogItemsPaginated_secondPage_returnsCorrectSlice() {
        PaginatedItemsDto<CatalogItem> result = catalogService.getCatalogItemsPaginated(5, 1);

        assertThat(result.pageIndex()).isEqualTo(1);
        assertThat(result.data()).hasSize(5);
    }

    @Test
    void getCatalogItemsPaginated_lastPage_returnsRemainingItems() {
        PaginatedItemsDto<CatalogItem> result = catalogService.getCatalogItemsPaginated(5, 2);

        assertThat(result.data()).hasSize(2);
        assertThat(result.count()).isEqualTo(12);
    }

    @Test
    void getCatalogItemsPaginated_beyondLastPage_returnsEmptyData() {
        PaginatedItemsDto<CatalogItem> result = catalogService.getCatalogItemsPaginated(5, 10);

        assertThat(result.data()).isEmpty();
        assertThat(result.count()).isEqualTo(12);
    }

    @Test
    void getCatalogItemsPaginated_allOnOnePage_returnsAll() {
        PaginatedItemsDto<CatalogItem> result = catalogService.getCatalogItemsPaginated(100, 0);

        assertThat(result.data()).hasSize(12);
        assertThat(result.count()).isEqualTo(12);
    }

    @Test
    void getCatalogItemsPaginated_itemsSortedById() {
        PaginatedItemsDto<CatalogItem> result = catalogService.getCatalogItemsPaginated(12, 0);

        List<Integer> ids = result.data().stream().map(CatalogItem::getId).toList();
        assertThat(ids).isSorted();
    }

    // --- createCatalogItem ---

    @Test
    void createCatalogItem_assignsNewId() {
        CatalogItem newItem = new CatalogItem();
        newItem.setName("New Test Item");
        newItem.setPrice(new BigDecimal("15.00"));
        newItem.setCatalogBrandId(1);
        newItem.setCatalogTypeId(1);

        catalogService.createCatalogItem(newItem);

        assertThat(newItem.getId()).isEqualTo(13);
    }

    @Test
    void createCatalogItem_itemRetrievableAfterCreation() {
        CatalogItem newItem = new CatalogItem();
        newItem.setName("Retrievable Item");
        newItem.setPrice(new BigDecimal("20.00"));
        newItem.setCatalogBrandId(1);
        newItem.setCatalogTypeId(2);

        catalogService.createCatalogItem(newItem);

        CatalogItem found = catalogService.findCatalogItem(newItem.getId());
        assertThat(found).isNotNull();
        assertThat(found.getName()).isEqualTo("Retrievable Item");
    }

    @Test
    void createCatalogItem_composesBrandAndType() {
        CatalogItem newItem = new CatalogItem();
        newItem.setName("Composed Item");
        newItem.setPrice(new BigDecimal("10.00"));
        newItem.setCatalogBrandId(2);
        newItem.setCatalogTypeId(1);

        catalogService.createCatalogItem(newItem);

        CatalogItem found = catalogService.findCatalogItem(newItem.getId());
        assertThat(found.getCatalogBrand()).isNotNull();
        assertThat(found.getCatalogBrand().getBrand()).isEqualTo(".NET");
        assertThat(found.getCatalogType()).isNotNull();
        assertThat(found.getCatalogType().getType()).isEqualTo("Mug");
    }

    @Test
    void createCatalogItem_incrementsIdSequentially() {
        CatalogItem first = new CatalogItem();
        first.setName("First");
        first.setPrice(BigDecimal.ONE);
        first.setCatalogBrandId(1);
        first.setCatalogTypeId(1);

        CatalogItem second = new CatalogItem();
        second.setName("Second");
        second.setPrice(BigDecimal.TEN);
        second.setCatalogBrandId(1);
        second.setCatalogTypeId(1);

        catalogService.createCatalogItem(first);
        catalogService.createCatalogItem(second);

        assertThat(second.getId()).isEqualTo(first.getId() + 1);
    }

    @Test
    void createCatalogItem_increasesTotalCount() {
        long countBefore = catalogService.getCatalogItemsPaginated(100, 0).count();

        CatalogItem newItem = new CatalogItem();
        newItem.setName("Count Item");
        newItem.setPrice(BigDecimal.ONE);
        newItem.setCatalogBrandId(1);
        newItem.setCatalogTypeId(1);
        catalogService.createCatalogItem(newItem);

        long countAfter = catalogService.getCatalogItemsPaginated(100, 0).count();
        assertThat(countAfter).isEqualTo(countBefore + 1);
    }

    // --- updateCatalogItem ---

    @Test
    void updateCatalogItem_existingItem_updatesSuccessfully() {
        CatalogItem item = catalogService.findCatalogItem(1);
        assertThat(item).isNotNull();

        item.setName("Updated Name");
        item.setPrice(new BigDecimal("99.99"));
        catalogService.updateCatalogItem(item);

        CatalogItem updated = catalogService.findCatalogItem(1);
        assertThat(updated.getName()).isEqualTo("Updated Name");
        assertThat(updated.getPrice()).isEqualByComparingTo(new BigDecimal("99.99"));
    }

    @Test
    void updateCatalogItem_nonExistentItem_doesNotAdd() {
        long countBefore = catalogService.getCatalogItemsPaginated(100, 0).count();

        CatalogItem nonExistent = new CatalogItem();
        nonExistent.setId(9999);
        nonExistent.setName("Ghost Item");
        nonExistent.setPrice(BigDecimal.ONE);
        catalogService.updateCatalogItem(nonExistent);

        long countAfter = catalogService.getCatalogItemsPaginated(100, 0).count();
        assertThat(countAfter).isEqualTo(countBefore);
        assertThat(catalogService.findCatalogItem(9999)).isNull();
    }

    // --- removeCatalogItem ---

    @Test
    void removeCatalogItem_existingItem_removesSuccessfully() {
        CatalogItem item = catalogService.findCatalogItem(1);
        assertThat(item).isNotNull();

        catalogService.removeCatalogItem(item);

        assertThat(catalogService.findCatalogItem(1)).isNull();
    }

    @Test
    void removeCatalogItem_decreasesTotalCount() {
        long countBefore = catalogService.getCatalogItemsPaginated(100, 0).count();

        CatalogItem item = catalogService.findCatalogItem(1);
        catalogService.removeCatalogItem(item);

        long countAfter = catalogService.getCatalogItemsPaginated(100, 0).count();
        assertThat(countAfter).isEqualTo(countBefore - 1);
    }

    @Test
    void removeCatalogItem_nonExistentItem_noEffect() {
        long countBefore = catalogService.getCatalogItemsPaginated(100, 0).count();

        CatalogItem nonExistent = new CatalogItem();
        nonExistent.setId(9999);
        catalogService.removeCatalogItem(nonExistent);

        long countAfter = catalogService.getCatalogItemsPaginated(100, 0).count();
        assertThat(countAfter).isEqualTo(countBefore);
    }

    // --- initialization ---

    @Test
    void initialization_loadsAllPreconfiguredItems() {
        int expectedCount = PreconfiguredData.getCatalogItems().size();
        PaginatedItemsDto<CatalogItem> result = catalogService.getCatalogItemsPaginated(100, 0);

        assertThat(result.count()).isEqualTo(expectedCount);
    }

    @Test
    void initialization_allItemsHaveBrandAndTypeComposed() {
        PaginatedItemsDto<CatalogItem> result = catalogService.getCatalogItemsPaginated(100, 0);

        for (CatalogItem item : result.data()) {
            assertThat(item.getCatalogBrand()).isNotNull();
            assertThat(item.getCatalogType()).isNotNull();
        }
    }
}
