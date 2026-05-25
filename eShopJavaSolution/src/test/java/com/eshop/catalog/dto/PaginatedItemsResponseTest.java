package com.eshop.catalog.dto;

import static org.junit.jupiter.api.Assertions.assertEquals;

import java.util.List;
import org.junit.jupiter.api.Test;

class PaginatedItemsResponseTest {

  @Test
  void totalPagesCalculatedCorrectly() {
    PaginatedItemsResponse<String> response =
        PaginatedItemsResponse.of(0, 4, 10, List.of("a", "b", "c", "d"));

    assertEquals(0, response.actualPage());
    assertEquals(4, response.itemsPerPage());
    assertEquals(10, response.totalItems());
    assertEquals(3, response.totalPages());
    assertEquals(4, response.data().size());
  }

  @Test
  void totalPagesRoundsUp() {
    PaginatedItemsResponse<String> response =
        PaginatedItemsResponse.of(0, 3, 7, List.of("a", "b", "c"));

    assertEquals(3, response.totalPages());
  }

  @Test
  void singlePageWhenItemsEqualPageSize() {
    PaginatedItemsResponse<String> response =
        PaginatedItemsResponse.of(0, 5, 5, List.of("a", "b", "c", "d", "e"));

    assertEquals(1, response.totalPages());
  }
}
