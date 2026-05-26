package com.eshop.catalog.dto;

import java.util.List;

/**
 * Generic DTO for paginated API responses and MVC model attributes.
 *
 * @param <T> the element type
 */
public record PaginatedItemsDto<T>(
    int actualPage, int itemsPerPage, long totalItems, int totalPages, List<T> data) {

  public PaginatedItemsDto(int pageIndex, int pageSize, long count, List<T> data) {
    this(
        pageIndex,
        pageSize,
        count,
        pageSize > 0 ? (int) Math.ceil((double) count / pageSize) : 0,
        data);
  }
}
