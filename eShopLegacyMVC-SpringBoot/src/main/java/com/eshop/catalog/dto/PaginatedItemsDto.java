package com.eshop.catalog.dto;

import java.util.List;

public record PaginatedItemsDto<T>(
        int actualPage,
        int itemsPerPage,
        long totalItems,
        int totalPages,
        List<T> data) {

    public PaginatedItemsDto(int pageIndex, int pageSize, long count, List<T> data) {
        this(pageIndex, pageSize, count,
                (int) Math.ceil((double) count / pageSize), data);
    }
}
