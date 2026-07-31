package com.eshop.catalog.service;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

import com.eshop.catalog.dto.PaginatedItemsDto;
import com.eshop.catalog.model.CatalogBrand;
import com.eshop.catalog.model.CatalogItem;
import com.eshop.catalog.model.CatalogType;
import com.eshop.catalog.repository.CatalogBrandRepository;
import com.eshop.catalog.repository.CatalogItemRepository;
import com.eshop.catalog.repository.CatalogTypeRepository;
import com.eshop.catalog.service.impl.CatalogServiceImpl;
import com.eshop.catalog.config.CatalogItemHiLoGenerator;
import java.math.BigDecimal;
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

@ExtendWith(MockitoExtension.class)
class CatalogServiceImplTest {

  @Mock private CatalogItemRepository catalogItemRepository;
  @Mock private CatalogBrandRepository catalogBrandRepository;
  @Mock private CatalogTypeRepository catalogTypeRepository;
  @Mock private CatalogItemHiLoGenerator indexGenerator;

  @InjectMocks private CatalogServiceImpl catalogService;

  private CatalogItem sampleItem;
  private CatalogBrand sampleBrand;
  private CatalogType sampleType;

  @BeforeEach
  void setUp() {
    sampleBrand = new CatalogBrand();
    sampleBrand.setId(1);
    sampleBrand.setBrand("TestBrand");

    sampleType = new CatalogType();
    sampleType.setId(1);
    sampleType.setType("TestType");

    sampleItem = new CatalogItem();
    sampleItem.setId(1);
    sampleItem.setName("Test Item");
    sampleItem.setDescription("Test Description");
    sampleItem.setPrice(new BigDecimal("19.99"));
    sampleItem.setCatalogBrandId(1);
    sampleItem.setCatalogTypeId(1);
    sampleItem.setCatalogBrand(sampleBrand);
    sampleItem.setCatalogType(sampleType);
    sampleItem.setAvailableStock(100);
    sampleItem.setRestockThreshold(10);
    sampleItem.setMaxStockThreshold(200);
    sampleItem.setOnReorder(false);
  }

  @Test
  void findCatalogItem_existingId_returnsItem() {
    when(catalogItemRepository.findByIdWithBrandAndType(1)).thenReturn(Optional.of(sampleItem));

    CatalogItem result = catalogService.findCatalogItem(1);

    assertThat(result).isNotNull();
    assertThat(result.getId()).isEqualTo(1);
    assertThat(result.getName()).isEqualTo("Test Item");
    verify(catalogItemRepository).findByIdWithBrandAndType(1);
  }

  @Test
  void findCatalogItem_nonExistingId_returnsNull() {
    when(catalogItemRepository.findByIdWithBrandAndType(999)).thenReturn(Optional.empty());

    CatalogItem result = catalogService.findCatalogItem(999);

    assertThat(result).isNull();
    verify(catalogItemRepository).findByIdWithBrandAndType(999);
  }

  @Test
  void getCatalogBrands_returnsBrandList() {
    List<CatalogBrand> brands = List.of(sampleBrand);
    when(catalogBrandRepository.findAll()).thenReturn(brands);

    List<CatalogBrand> result = catalogService.getCatalogBrands();

    assertThat(result).hasSize(1);
    assertThat(result.get(0).getBrand()).isEqualTo("TestBrand");
    verify(catalogBrandRepository).findAll();
  }

  @Test
  void getCatalogBrands_emptyList_returnsEmpty() {
    when(catalogBrandRepository.findAll()).thenReturn(List.of());

    List<CatalogBrand> result = catalogService.getCatalogBrands();

    assertThat(result).isEmpty();
  }

  @Test
  void getCatalogTypes_returnsTypeList() {
    List<CatalogType> types = List.of(sampleType);
    when(catalogTypeRepository.findAll()).thenReturn(types);

    List<CatalogType> result = catalogService.getCatalogTypes();

    assertThat(result).hasSize(1);
    assertThat(result.get(0).getType()).isEqualTo("TestType");
    verify(catalogTypeRepository).findAll();
  }

  @Test
  void getCatalogTypes_emptyList_returnsEmpty() {
    when(catalogTypeRepository.findAll()).thenReturn(List.of());

    List<CatalogType> result = catalogService.getCatalogTypes();

    assertThat(result).isEmpty();
  }

  @Test
  void getCatalogItemsPaginated_returnsPage() {
    List<CatalogItem> items = List.of(sampleItem);
    PageRequest expectedPageRequest = PageRequest.of(0, 10, Sort.by("id"));
    Page<CatalogItem> page = new PageImpl<>(items, expectedPageRequest, 1);
    when(catalogItemRepository.findAllWithBrandAndType(any(Pageable.class))).thenReturn(page);

    PaginatedItemsDto<CatalogItem> result = catalogService.getCatalogItemsPaginated(10, 0);

    assertThat(result).isNotNull();
    assertThat(result.actualPage()).isZero();
    assertThat(result.itemsPerPage()).isEqualTo(10);
    assertThat(result.totalItems()).isEqualTo(1);
    assertThat(result.data()).hasSize(1);
    assertThat(result.data().get(0).getName()).isEqualTo("Test Item");
  }

  @Test
  void getCatalogItemsPaginated_emptyPage_returnsEmptyResult() {
    PageRequest expectedPageRequest = PageRequest.of(0, 10, Sort.by("id"));
    Page<CatalogItem> emptyPage = new PageImpl<>(List.of(), expectedPageRequest, 0);
    when(catalogItemRepository.findAllWithBrandAndType(any(Pageable.class))).thenReturn(emptyPage);

    PaginatedItemsDto<CatalogItem> result = catalogService.getCatalogItemsPaginated(10, 0);

    assertThat(result.data()).isEmpty();
    assertThat(result.totalItems()).isZero();
  }

  @Test
  void getCatalogItemsPaginated_secondPage() {
    PageRequest expectedPageRequest = PageRequest.of(1, 5, Sort.by("id"));
    Page<CatalogItem> page = new PageImpl<>(List.of(sampleItem), expectedPageRequest, 10);
    when(catalogItemRepository.findAllWithBrandAndType(any(Pageable.class))).thenReturn(page);

    PaginatedItemsDto<CatalogItem> result = catalogService.getCatalogItemsPaginated(5, 1);

    assertThat(result.actualPage()).isEqualTo(1);
    assertThat(result.itemsPerPage()).isEqualTo(5);
    assertThat(result.totalItems()).isEqualTo(10);
  }

  @Test
  void createCatalogItem_setsIdAndSaves() {
    when(indexGenerator.getNextSequenceValue()).thenReturn(42);

    catalogService.createCatalogItem(sampleItem);

    assertThat(sampleItem.getId()).isEqualTo(42);
    verify(indexGenerator).getNextSequenceValue();
    verify(catalogItemRepository).save(sampleItem);
  }

  @Test
  void updateCatalogItem_savesItem() {
    catalogService.updateCatalogItem(sampleItem);

    verify(catalogItemRepository).save(sampleItem);
  }

  @Test
  void removeCatalogItem_deletesItem() {
    catalogService.removeCatalogItem(sampleItem);

    verify(catalogItemRepository).delete(sampleItem);
  }

  @Test
  void findCatalogItem_verifiesBrandAndTypeAssociation() {
    when(catalogItemRepository.findByIdWithBrandAndType(1)).thenReturn(Optional.of(sampleItem));

    CatalogItem result = catalogService.findCatalogItem(1);

    assertThat(result.getCatalogBrand()).isNotNull();
    assertThat(result.getCatalogBrand().getBrand()).isEqualTo("TestBrand");
    assertThat(result.getCatalogType()).isNotNull();
    assertThat(result.getCatalogType().getType()).isEqualTo("TestType");
  }

  @Test
  void createCatalogItem_preservesItemData() {
    when(indexGenerator.getNextSequenceValue()).thenReturn(100);
    CatalogItem newItem = new CatalogItem();
    newItem.setName("New Item");
    newItem.setPrice(new BigDecimal("29.99"));
    newItem.setCatalogBrandId(1);
    newItem.setCatalogTypeId(1);

    catalogService.createCatalogItem(newItem);

    assertThat(newItem.getId()).isEqualTo(100);
    assertThat(newItem.getName()).isEqualTo("New Item");
    assertThat(newItem.getPrice()).isEqualByComparingTo(new BigDecimal("29.99"));
    verify(catalogItemRepository).save(newItem);
  }

  @Test
  void getCatalogItemsPaginated_calculatesCorrectTotalPages() {
    PageRequest pageRequest = PageRequest.of(0, 3, Sort.by("id"));
    Page<CatalogItem> page = new PageImpl<>(List.of(sampleItem), pageRequest, 10);
    when(catalogItemRepository.findAllWithBrandAndType(any(Pageable.class))).thenReturn(page);

    PaginatedItemsDto<CatalogItem> result = catalogService.getCatalogItemsPaginated(3, 0);

    assertThat(result.totalPages()).isEqualTo(4);
  }

  @Test
  void getCatalogItemsPaginated_usesCorrectSortOrder() {
    PageRequest expectedPageRequest = PageRequest.of(0, 10, Sort.by("id"));
    Page<CatalogItem> page = new PageImpl<>(List.of(sampleItem), expectedPageRequest, 1);
    when(catalogItemRepository.findAllWithBrandAndType(eq(expectedPageRequest))).thenReturn(page);

    catalogService.getCatalogItemsPaginated(10, 0);

    verify(catalogItemRepository).findAllWithBrandAndType(eq(expectedPageRequest));
  }

  @Test
  void createCatalogItem_withMultipleItems_assignsUniqueIds() {
    when(indexGenerator.getNextSequenceValue()).thenReturn(10, 11);

    CatalogItem item1 = new CatalogItem();
    item1.setName("Item 1");
    CatalogItem item2 = new CatalogItem();
    item2.setName("Item 2");

    catalogService.createCatalogItem(item1);
    catalogService.createCatalogItem(item2);

    assertThat(item1.getId()).isEqualTo(10);
    assertThat(item2.getId()).isEqualTo(11);
  }

  @Test
  void removeCatalogItem_callsDeleteOnRepository() {
    CatalogItem itemToRemove = new CatalogItem();
    itemToRemove.setId(5);
    itemToRemove.setName("To Remove");

    catalogService.removeCatalogItem(itemToRemove);

    verify(catalogItemRepository).delete(itemToRemove);
  }
}
