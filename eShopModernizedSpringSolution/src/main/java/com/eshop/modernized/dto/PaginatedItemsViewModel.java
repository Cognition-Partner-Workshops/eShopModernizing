package com.eshop.modernized.dto;

import java.util.List;

public record PaginatedItemsViewModel<T>(
        int pageIndex,
        int pageSize,
        long count,
        List<T> data
) {

    public int totalPages() {
        return (int) Math.ceil((double) count / pageSize);
    }
}
