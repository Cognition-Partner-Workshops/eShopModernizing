using System;
using System.Collections.Generic;

namespace eShopLegacyMVC.ViewModel
{
    public class PaginatedItemsViewModel<TEntity> where TEntity : class
    {
        public int ActualPage { get; private set; }

        public int ItemsPerPage { get; private set; }

        public long TotalItems { get; private set; }

        public int TotalPages { get; set; }

        public IEnumerable<TEntity> Data { get; private set; }

        public string SearchText { get; set; }

        public int? BrandId { get; set; }

        public int? TypeId { get; set; }

        public System.Web.Mvc.SelectList Brands { get; set; }

        public System.Web.Mvc.SelectList Types { get; set; }

        public PaginatedItemsViewModel(int pageIndex, int pageSize, long count, IEnumerable<TEntity> data)
        {
            ActualPage = pageIndex;
            ItemsPerPage = pageSize;
            TotalItems = count;
            TotalPages = (int)Math.Ceiling(((decimal)count / pageSize));
            Data = data;
        }
    }
}
