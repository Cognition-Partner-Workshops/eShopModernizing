package com.eshop.catalog.service;

import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.infrastructure.PreconfiguredData;
import com.eshop.catalog.viewmodel.PaginatedItemsViewModel;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertNull;
import static org.junit.jupiter.api.Assertions.assertTrue;

class CatalogServiceMockTest {

    private CatalogServiceMock service;

    @BeforeEach
    void setUp() {
        service = new CatalogServiceMock();
    }

    @Test
    void testGetPaginated() {
        int totalSeedItems = PreconfiguredData.getCatalogItems().size();

        PaginatedItemsViewModel<CatalogItem> result = service.getCatalogItemsPaginated(5, 0);

        assertNotNull(result);
        assertEquals(0, result.getActualPage());
        assertEquals(5, result.getItemsPerPage());
        assertEquals(totalSeedItems, result.getTotalItems());
        assertEquals(5, result.getData().size());
    }

    @Test
    void testGetPaginatedSecondPage() {
        int totalSeedItems = PreconfiguredData.getCatalogItems().size();

        PaginatedItemsViewModel<CatalogItem> result = service.getCatalogItemsPaginated(5, 1);

        assertNotNull(result);
        assertEquals(1, result.getActualPage());
        assertEquals(5, result.getData().size());
        assertEquals(totalSeedItems, result.getTotalItems());
    }

    @Test
    void testGetPaginatedLastPage() {
        int totalSeedItems = PreconfiguredData.getCatalogItems().size();

        PaginatedItemsViewModel<CatalogItem> result = service.getCatalogItemsPaginated(5, 2);

        assertNotNull(result);
        assertEquals(2, result.getActualPage());
        assertEquals(totalSeedItems - 10, result.getData().size());
    }

    @Test
    void testGetPaginatedBeyondData() {
        PaginatedItemsViewModel<CatalogItem> result = service.getCatalogItemsPaginated(5, 100);

        assertNotNull(result);
        assertTrue(result.getData().isEmpty());
    }

    @Test
    void testFindByIdExisting() {
        CatalogItem result = service.findCatalogItem(1);

        assertNotNull(result);
        assertEquals(1, result.getId());
        assertEquals(".NET Bot Black Hoodie", result.getName());
    }

    @Test
    void testFindByIdNonExisting() {
        CatalogItem result = service.findCatalogItem(9999);

        assertNull(result);
    }

    @Test
    void testCreate() {
        int initialSize = PreconfiguredData.getCatalogItems().size();
        CatalogItem newItem = new CatalogItem();
        newItem.setName("New Test Item");
        newItem.setPrice(new BigDecimal("50.00"));
        newItem.setPictureFileName("new.png");

        CatalogItem created = service.createCatalogItem(newItem);

        assertNotNull(created);
        assertEquals(initialSize + 1, created.getId());
        assertEquals("New Test Item", created.getName());

        PaginatedItemsViewModel<CatalogItem> paginated = service.getCatalogItemsPaginated(100, 0);
        assertEquals(initialSize + 1, paginated.getTotalItems());
    }

    @Test
    void testUpdate() {
        CatalogItem existing = service.findCatalogItem(1);
        assertNotNull(existing);

        CatalogItem updated = new CatalogItem();
        updated.setId(1);
        updated.setName("Updated Name");
        updated.setPrice(new BigDecimal("99.99"));
        updated.setPictureFileName("updated.png");

        service.updateCatalogItem(updated);

        CatalogItem found = service.findCatalogItem(1);
        assertNotNull(found);
        assertEquals("Updated Name", found.getName());
        assertEquals(new BigDecimal("99.99"), found.getPrice());
    }

    @Test
    void testRemove() {
        int initialSize = PreconfiguredData.getCatalogItems().size();
        CatalogItem toRemove = service.findCatalogItem(1);
        assertNotNull(toRemove);

        service.removeCatalogItem(toRemove);

        assertNull(service.findCatalogItem(1));
        PaginatedItemsViewModel<CatalogItem> paginated = service.getCatalogItemsPaginated(100, 0);
        assertEquals(initialSize - 1, paginated.getTotalItems());
    }
}
