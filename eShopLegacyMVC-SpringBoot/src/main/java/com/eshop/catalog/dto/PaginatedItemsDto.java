package com.eshop.catalog.dto;

import java.util.List;

public class PaginatedItemsDto<T> {

  private final int actualPage;
  private final int itemsPerPage;
  private final long totalItems;
  private final int totalPages;
  private final List<T> data;

  public PaginatedItemsDto(int pageIndex, int pageSize, long count, List<T> data) {
    this.actualPage = pageIndex;
    this.itemsPerPage = pageSize;
    this.totalItems = count;
    this.totalPages = (int) Math.ceil((double) count / pageSize);
    this.data = data;
  }

  public int getActualPage() {
    return actualPage;
  }

  public int getItemsPerPage() {
    return itemsPerPage;
  }

  public long getTotalItems() {
    return totalItems;
  }

  public int getTotalPages() {
    return totalPages;
  }

  public List<T> getData() {
    return data;
  }
}
