package com.eshop.catalog.dto;

import java.util.List;

/**
 * Generic paginated response container for catalog queries.
 *
 * @param pageIndex zero-based index of the current page
 * @param pageSize  maximum number of items per page
 * @param count     total number of items across all pages
 * @param data      items on the current page
 * @param <T>       the type of items in the page
 */
public record PaginatedItemsDto<T>(
        int pageIndex,
        int pageSize,
        long count,
        List<T> data
) {

    /**
     * Calculates the total number of pages needed to hold all items.
     *
     * @return total page count (at least 1 when items exist)
     */
    public int totalPages() {
        if (pageSize <= 0) {
            return 0;
        }
        return (int) Math.ceil((double) count / pageSize);
    }
}
