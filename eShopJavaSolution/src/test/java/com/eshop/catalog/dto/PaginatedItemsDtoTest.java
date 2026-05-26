package com.eshop.catalog.dto;

import static org.assertj.core.api.Assertions.assertThat;

import java.util.List;
import org.junit.jupiter.api.Test;

class PaginatedItemsDtoTest {

  @Test
  void computesTotalPagesFromCeil() {
    var dto = new PaginatedItemsDto<>(0, 10, 25, List.of("a", "b"));

    assertThat(dto.actualPage()).isZero();
    assertThat(dto.itemsPerPage()).isEqualTo(10);
    assertThat(dto.totalItems()).isEqualTo(25);
    assertThat(dto.totalPages()).isEqualTo(3);
    assertThat(dto.data()).containsExactly("a", "b");
  }

  @Test
  void exactDivisionYieldsNoExtraPage() {
    var dto = new PaginatedItemsDto<>(1, 5, 20, List.of(1, 2, 3, 4, 5));

    assertThat(dto.totalPages()).isEqualTo(4);
  }

  @Test
  void zeroItemsYieldsZeroPages() {
    var dto = new PaginatedItemsDto<>(0, 10, 0, List.of());

    assertThat(dto.totalPages()).isZero();
    assertThat(dto.data()).isEmpty();
  }

  @Test
  void zeroPageSizeYieldsZeroPages() {
    var dto = new PaginatedItemsDto<>(0, 0, 10, List.of());

    assertThat(dto.totalPages()).isZero();
  }

  @Test
  void singleItemSinglePage() {
    var dto = new PaginatedItemsDto<>(0, 10, 1, List.of("only"));

    assertThat(dto.totalPages()).isEqualTo(1);
    assertThat(dto.data()).containsExactly("only");
  }
}
