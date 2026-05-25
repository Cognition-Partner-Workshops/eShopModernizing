package com.eshop.catalog.dto;

import java.util.List;

public record PaginatedItemsResponse<T>(
    int actualPage, int itemsPerPage, long totalItems, int totalPages, List<T> data) {

  public static <T> PaginatedItemsResponse<T> of(
      int pageIndex, int pageSize, long count, List<T> data) {
    int totalPages = (int) Math.ceil((double) count / pageSize);
    return new PaginatedItemsResponse<>(pageIndex, pageSize, count, totalPages, data);
  }
}
