package com.eshop.catalog.viewmodel;

import java.util.List;

public class PaginatedItemsViewModel<T> {

    private int actualPage;
    private int itemsPerPage;
    private long totalItems;
    private int totalPages;
    private List<T> data;

    public PaginatedItemsViewModel(int pageIndex, int pageSize, long count, List<T> data) {
        this.actualPage = pageIndex;
        this.itemsPerPage = pageSize;
        this.totalItems = count;
        this.totalPages = (int) Math.ceil((double) count / pageSize);
        this.data = data;
    }

    public int getActualPage() {
        return actualPage;
    }

    public void setActualPage(int actualPage) {
        this.actualPage = actualPage;
    }

    public int getItemsPerPage() {
        return itemsPerPage;
    }

    public void setItemsPerPage(int itemsPerPage) {
        this.itemsPerPage = itemsPerPage;
    }

    public long getTotalItems() {
        return totalItems;
    }

    public void setTotalItems(long totalItems) {
        this.totalItems = totalItems;
    }

    public int getTotalPages() {
        return totalPages;
    }

    public void setTotalPages(int totalPages) {
        this.totalPages = totalPages;
    }

    public List<T> getData() {
        return data;
    }

    public void setData(List<T> data) {
        this.data = data;
    }
}
