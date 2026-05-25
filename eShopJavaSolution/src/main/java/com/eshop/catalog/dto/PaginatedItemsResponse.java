package com.eshop.catalog.dto;

import java.util.List;

/**
 * Generic paginated response DTO.
 *
 * @param <T> the type of items in the response
 */
public record PaginatedItemsResponse<T>(
    int actualPage, int itemsPerPage, long totalItems, int totalPages, List<T> data) {

  /**
   * Creates a paginated response, computing {@code totalPages} from {@code totalItems} and {@code
   * itemsPerPage}.
   */
  public PaginatedItemsResponse(int pageIndex, int pageSize, long count, List<T> data) {
    this(
        pageIndex,
        pageSize,
        count,
        pageSize > 0 ? (int) Math.ceil((double) count / pageSize) : 0,
        data);
  }
}
