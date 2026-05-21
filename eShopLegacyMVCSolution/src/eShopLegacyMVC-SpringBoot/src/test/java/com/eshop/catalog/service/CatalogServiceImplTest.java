package com.eshop.catalog.service;

import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogItemHiLoGenerator;
import com.eshop.catalog.model.CatalogType;
import com.eshop.catalog.repository.CatalogBrandRepository;
import com.eshop.catalog.repository.CatalogItemRepository;
import com.eshop.catalog.repository.CatalogTypeRepository;
import com.eshop.catalog.viewmodel.PaginatedItemsViewModel;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageImpl;
import org.springframework.data.domain.PageRequest;

import java.math.BigDecimal;
import java.util.List;
import java.util.Optional;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertNull;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

@ExtendWith(MockitoExtension.class)
class CatalogServiceImplTest {

    @Mock
    private CatalogItemRepository itemRepository;

    @Mock
    private CatalogBrandRepository brandRepository;

    @Mock
    private CatalogTypeRepository typeRepository;

    @Mock
    private CatalogItemHiLoGenerator hiLoGenerator;

    private CatalogServiceImpl service;

    @BeforeEach
    void setUp() {
        service = new CatalogServiceImpl(itemRepository, brandRepository, typeRepository, hiLoGenerator);
    }

    @Test
    void testGetCatalogItemsPaginated() {
        CatalogItem item1 = createItem(1, "Item 1", new BigDecimal("10.00"));
        CatalogItem item2 = createItem(2, "Item 2", new BigDecimal("20.00"));
        List<CatalogItem> items = List.of(item1, item2);
        Page<CatalogItem> page = new PageImpl<>(items, PageRequest.of(0, 10), 2);

        when(itemRepository.findAllWithBrandAndType(PageRequest.of(0, 10))).thenReturn(page);

        PaginatedItemsViewModel<CatalogItem> result = service.getCatalogItemsPaginated(10, 0);

        assertNotNull(result);
        assertEquals(0, result.getActualPage());
        assertEquals(10, result.getItemsPerPage());
        assertEquals(2, result.getTotalItems());
        assertEquals(2, result.getData().size());
        assertEquals("Item 1", result.getData().get(0).getName());
        verify(itemRepository).findAllWithBrandAndType(PageRequest.of(0, 10));
    }

    @Test
    void testGetCatalogItemsPaginatedSecondPage() {
        CatalogItem item3 = createItem(3, "Item 3", new BigDecimal("30.00"));
        List<CatalogItem> items = List.of(item3);
        Page<CatalogItem> page = new PageImpl<>(items, PageRequest.of(1, 2), 3);

        when(itemRepository.findAllWithBrandAndType(PageRequest.of(1, 2))).thenReturn(page);

        PaginatedItemsViewModel<CatalogItem> result = service.getCatalogItemsPaginated(2, 1);

        assertNotNull(result);
        assertEquals(1, result.getActualPage());
        assertEquals(2, result.getItemsPerPage());
        assertEquals(3, result.getTotalItems());
        assertEquals(1, result.getData().size());
    }

    @Test
    void testFindCatalogItemFound() {
        CatalogItem item = createItem(1, "Test Item", new BigDecimal("15.00"));
        when(itemRepository.findByIdWithBrandAndType(1)).thenReturn(Optional.of(item));

        CatalogItem result = service.findCatalogItem(1);

        assertNotNull(result);
        assertEquals(1, result.getId());
        assertEquals("Test Item", result.getName());
        verify(itemRepository).findByIdWithBrandAndType(1);
    }

    @Test
    void testFindCatalogItemNotFound() {
        when(itemRepository.findByIdWithBrandAndType(999)).thenReturn(Optional.empty());

        CatalogItem result = service.findCatalogItem(999);

        assertNull(result);
        verify(itemRepository).findByIdWithBrandAndType(999);
    }

    @Test
    void testGetCatalogBrands() {
        CatalogBrand brand1 = createBrand(1, "Azure");
        CatalogBrand brand2 = createBrand(2, ".NET");
        List<CatalogBrand> brands = List.of(brand1, brand2);
        when(brandRepository.findAll()).thenReturn(brands);

        List<CatalogBrand> result = service.getCatalogBrands();

        assertNotNull(result);
        assertEquals(2, result.size());
        assertEquals("Azure", result.get(0).getBrand());
        assertEquals(".NET", result.get(1).getBrand());
        verify(brandRepository).findAll();
    }

    @Test
    void testGetCatalogTypes() {
        CatalogType type1 = createType(1, "Mug");
        CatalogType type2 = createType(2, "T-Shirt");
        List<CatalogType> types = List.of(type1, type2);
        when(typeRepository.findAll()).thenReturn(types);

        List<CatalogType> result = service.getCatalogTypes();

        assertNotNull(result);
        assertEquals(2, result.size());
        assertEquals("Mug", result.get(0).getType());
        assertEquals("T-Shirt", result.get(1).getType());
        verify(typeRepository).findAll();
    }

    @Test
    void testCreateCatalogItem() {
        CatalogItem item = createItem(0, "New Item", new BigDecimal("25.00"));
        when(hiLoGenerator.getNextSequenceValue()).thenReturn(42);
        when(itemRepository.save(any(CatalogItem.class))).thenAnswer(inv -> inv.getArgument(0));

        CatalogItem result = service.createCatalogItem(item);

        assertNotNull(result);
        assertEquals(42, result.getId());
        verify(hiLoGenerator).getNextSequenceValue();
        verify(itemRepository).save(item);
    }

    @Test
    void testUpdateCatalogItem() {
        CatalogItem item = createItem(1, "Updated Item", new BigDecimal("30.00"));
        when(itemRepository.save(item)).thenReturn(item);

        CatalogItem result = service.updateCatalogItem(item);

        assertNotNull(result);
        assertEquals("Updated Item", result.getName());
        verify(itemRepository).save(item);
    }

    @Test
    void testRemoveCatalogItem() {
        CatalogItem item = createItem(1, "To Delete", new BigDecimal("10.00"));

        service.removeCatalogItem(item);

        verify(itemRepository).delete(item);
    }

    private CatalogItem createItem(int id, String name, BigDecimal price) {
        CatalogItem item = new CatalogItem();
        item.setId(id);
        item.setName(name);
        item.setPrice(price);
        item.setPictureFileName("test.png");
        return item;
    }

    private CatalogBrand createBrand(int id, String brand) {
        CatalogBrand b = new CatalogBrand();
        b.setId(id);
        b.setBrand(brand);
        return b;
    }

    private CatalogType createType(int id, String type) {
        CatalogType t = new CatalogType();
        t.setId(id);
        t.setType(type);
        return t;
    }
}
