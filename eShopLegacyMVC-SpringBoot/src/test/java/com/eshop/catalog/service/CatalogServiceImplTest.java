package com.eshop.catalog.service;

import java.math.BigDecimal;
import java.util.Collections;
import java.util.List;
import java.util.Optional;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageImpl;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Pageable;
import org.springframework.data.domain.Sort;

import com.eshop.catalog.config.CatalogItemHiLoGenerator;
import com.eshop.catalog.domain.entity.CatalogBrand;
import com.eshop.catalog.domain.entity.CatalogItem;
import com.eshop.catalog.domain.entity.CatalogType;
import com.eshop.catalog.dto.PaginatedItemsDto;
import com.eshop.catalog.repository.CatalogBrandRepository;
import com.eshop.catalog.repository.CatalogItemRepository;
import com.eshop.catalog.repository.CatalogTypeRepository;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

@ExtendWith(MockitoExtension.class)
class CatalogServiceImplTest {

    @Mock
    private CatalogItemRepository catalogItemRepository;

    @Mock
    private CatalogBrandRepository catalogBrandRepository;

    @Mock
    private CatalogTypeRepository catalogTypeRepository;

    @Mock
    private CatalogItemHiLoGenerator hiLoGenerator;

    @InjectMocks
    private CatalogServiceImpl catalogService;

    private CatalogItem sampleItem;
    private CatalogBrand sampleBrand;
    private CatalogType sampleType;

    @BeforeEach
    void setUp() {
        sampleBrand = new CatalogBrand("Azure");
        sampleBrand.setId(1);

        sampleType = new CatalogType("Mug");
        sampleType.setId(1);

        sampleItem = new CatalogItem();
        sampleItem.setId(1);
        sampleItem.setName(".NET Bot Black Hoodie");
        sampleItem.setDescription("A cool hoodie");
        sampleItem.setPrice(new BigDecimal("19.50"));
        sampleItem.setCatalogBrandId(1);
        sampleItem.setCatalogTypeId(1);
        sampleItem.setCatalogBrand(sampleBrand);
        sampleItem.setCatalogType(sampleType);
    }

    // --- findCatalogItem ---

    @Test
    void findCatalogItem_existingId_returnsItem() {
        when(catalogItemRepository.findByIdWithBrandAndType(1))
                .thenReturn(Optional.of(sampleItem));

        CatalogItem result = catalogService.findCatalogItem(1);

        assertThat(result).isNotNull();
        assertThat(result.getId()).isEqualTo(1);
        assertThat(result.getName()).isEqualTo(".NET Bot Black Hoodie");
        verify(catalogItemRepository).findByIdWithBrandAndType(1);
    }

    @Test
    void findCatalogItem_unknownId_returnsNull() {
        when(catalogItemRepository.findByIdWithBrandAndType(999))
                .thenReturn(Optional.empty());

        CatalogItem result = catalogService.findCatalogItem(999);

        assertThat(result).isNull();
        verify(catalogItemRepository).findByIdWithBrandAndType(999);
    }

    // --- getCatalogBrands ---

    @Test
    void getCatalogBrands_returnsBrandsList() {
        CatalogBrand brand2 = new CatalogBrand(".NET");
        brand2.setId(2);
        List<CatalogBrand> brands = List.of(sampleBrand, brand2);
        when(catalogBrandRepository.findAll()).thenReturn(brands);

        List<CatalogBrand> result = catalogService.getCatalogBrands();

        assertThat(result).hasSize(2);
        assertThat(result.get(0).getBrand()).isEqualTo("Azure");
        assertThat(result.get(1).getBrand()).isEqualTo(".NET");
        verify(catalogBrandRepository).findAll();
    }

    @Test
    void getCatalogBrands_empty_returnsEmptyList() {
        when(catalogBrandRepository.findAll()).thenReturn(Collections.emptyList());

        List<CatalogBrand> result = catalogService.getCatalogBrands();

        assertThat(result).isEmpty();
    }

    // --- getCatalogTypes ---

    @Test
    void getCatalogTypes_returnsTypesList() {
        CatalogType type2 = new CatalogType("T-Shirt");
        type2.setId(2);
        List<CatalogType> types = List.of(sampleType, type2);
        when(catalogTypeRepository.findAll()).thenReturn(types);

        List<CatalogType> result = catalogService.getCatalogTypes();

        assertThat(result).hasSize(2);
        assertThat(result.get(0).getType()).isEqualTo("Mug");
        assertThat(result.get(1).getType()).isEqualTo("T-Shirt");
        verify(catalogTypeRepository).findAll();
    }

    @Test
    void getCatalogTypes_empty_returnsEmptyList() {
        when(catalogTypeRepository.findAll()).thenReturn(Collections.emptyList());

        List<CatalogType> result = catalogService.getCatalogTypes();

        assertThat(result).isEmpty();
    }

    // --- getCatalogItemsPaginated ---

    @Test
    void getCatalogItemsPaginated_returnsCorrectPage() {
        int pageSize = 5;
        int pageIndex = 0;
        Pageable expectedPageable = PageRequest.of(pageIndex, pageSize, Sort.by("id"));
        List<CatalogItem> items = List.of(sampleItem);
        Page<CatalogItem> page = new PageImpl<>(items, expectedPageable, 1);
        when(catalogItemRepository.findAll(eq(expectedPageable))).thenReturn(page);

        PaginatedItemsDto<CatalogItem> result = catalogService.getCatalogItemsPaginated(pageSize, pageIndex);

        assertThat(result.pageIndex()).isEqualTo(0);
        assertThat(result.pageSize()).isEqualTo(5);
        assertThat(result.count()).isEqualTo(1);
        assertThat(result.data()).hasSize(1);
        assertThat(result.data().get(0).getName()).isEqualTo(".NET Bot Black Hoodie");
        verify(catalogItemRepository).findAll(eq(expectedPageable));
    }

    @Test
    void getCatalogItemsPaginated_emptyPage_returnsEmptyData() {
        int pageSize = 10;
        int pageIndex = 5;
        Pageable expectedPageable = PageRequest.of(pageIndex, pageSize, Sort.by("id"));
        Page<CatalogItem> emptyPage = new PageImpl<>(Collections.emptyList(), expectedPageable, 0);
        when(catalogItemRepository.findAll(eq(expectedPageable))).thenReturn(emptyPage);

        PaginatedItemsDto<CatalogItem> result = catalogService.getCatalogItemsPaginated(pageSize, pageIndex);

        assertThat(result.pageIndex()).isEqualTo(5);
        assertThat(result.pageSize()).isEqualTo(10);
        assertThat(result.count()).isZero();
        assertThat(result.data()).isEmpty();
    }

    @Test
    void getCatalogItemsPaginated_multiplePages_returnsCorrectTotalCount() {
        int pageSize = 2;
        int pageIndex = 0;
        Pageable expectedPageable = PageRequest.of(pageIndex, pageSize, Sort.by("id"));

        CatalogItem item2 = new CatalogItem();
        item2.setId(2);
        item2.setName("Second Item");
        item2.setPrice(new BigDecimal("10.00"));

        List<CatalogItem> items = List.of(sampleItem, item2);
        Page<CatalogItem> page = new PageImpl<>(items, expectedPageable, 5);
        when(catalogItemRepository.findAll(eq(expectedPageable))).thenReturn(page);

        PaginatedItemsDto<CatalogItem> result = catalogService.getCatalogItemsPaginated(pageSize, pageIndex);

        assertThat(result.count()).isEqualTo(5);
        assertThat(result.data()).hasSize(2);
    }

    // --- createCatalogItem ---

    @Test
    void createCatalogItem_setsIdFromGeneratorAndSaves() {
        when(hiLoGenerator.getNextSequenceValue()).thenReturn(42);

        CatalogItem newItem = new CatalogItem();
        newItem.setName("New Item");
        newItem.setPrice(new BigDecimal("15.00"));

        catalogService.createCatalogItem(newItem);

        assertThat(newItem.getId()).isEqualTo(42);
        verify(hiLoGenerator).getNextSequenceValue();
        verify(catalogItemRepository).save(newItem);
    }

    @Test
    void createCatalogItem_callsRepositorySaveWithItem() {
        when(hiLoGenerator.getNextSequenceValue()).thenReturn(100);

        CatalogItem newItem = new CatalogItem();
        newItem.setName("Another Item");
        newItem.setPrice(new BigDecimal("25.00"));

        catalogService.createCatalogItem(newItem);

        verify(catalogItemRepository).save(newItem);
    }

    // --- updateCatalogItem ---

    @Test
    void updateCatalogItem_delegatesToRepository() {
        catalogService.updateCatalogItem(sampleItem);

        verify(catalogItemRepository).save(sampleItem);
    }

    @Test
    void updateCatalogItem_savesModifiedItem() {
        sampleItem.setName("Updated Name");
        sampleItem.setPrice(new BigDecimal("29.99"));

        catalogService.updateCatalogItem(sampleItem);

        verify(catalogItemRepository).save(sampleItem);
    }

    // --- removeCatalogItem ---

    @Test
    void removeCatalogItem_delegatesToRepository() {
        catalogService.removeCatalogItem(sampleItem);

        verify(catalogItemRepository).delete(sampleItem);
    }

    @Test
    void removeCatalogItem_deletesCorrectItem() {
        CatalogItem itemToRemove = new CatalogItem();
        itemToRemove.setId(7);
        itemToRemove.setName("Item To Remove");

        catalogService.removeCatalogItem(itemToRemove);

        verify(catalogItemRepository).delete(itemToRemove);
    }
}
