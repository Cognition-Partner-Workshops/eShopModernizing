package com.eshop.catalog.dto;

import static org.assertj.core.api.Assertions.assertThat;

import java.util.List;
import org.junit.jupiter.api.Test;

class PaginatedItemsResponseTest {

  @Test
  void shouldCalculateTotalPages() {
    var response = new PaginatedItemsResponse<>(0, 10, 25, List.of("a", "b"));

    assertThat(response.actualPage()).isEqualTo(0);
    assertThat(response.itemsPerPage()).isEqualTo(10);
    assertThat(response.totalItems()).isEqualTo(25);
    assertThat(response.totalPages()).isEqualTo(3);
    assertThat(response.data()).containsExactly("a", "b");
  }

  @Test
  void shouldReturnZeroPagesWhenPageSizeIsZero() {
    var response = new PaginatedItemsResponse<>(0, 0, 10, List.of());

    assertThat(response.totalPages()).isZero();
  }

  @Test
  void shouldReturnOnePageWhenItemsFitExactly() {
    var response = new PaginatedItemsResponse<>(0, 5, 5, List.of(1, 2, 3, 4, 5));

    assertThat(response.totalPages()).isEqualTo(1);
    assertThat(response.data()).hasSize(5);
  }

  @Test
  void shouldHandleEmptyData() {
    var response = new PaginatedItemsResponse<>(0, 10, 0, List.of());

    assertThat(response.totalPages()).isZero();
    assertThat(response.data()).isEmpty();
  }

  @Test
  void shouldSupportGenericTypes() {
    record Item(int id, String name) {}
    var items = List.of(new Item(1, "Widget"), new Item(2, "Gadget"));
    var response = new PaginatedItemsResponse<>(2, 2, 10, items);

    assertThat(response.actualPage()).isEqualTo(2);
    assertThat(response.totalPages()).isEqualTo(5);
    assertThat(response.data()).hasSize(2);
    assertThat(response.data().get(0).name()).isEqualTo("Widget");
  }
}
