namespace Catalog.Mvc.Models;

/// <summary>
/// Server-side paging view model ported from the legacy MVC
/// <c>eShopLegacyMVC.ViewModel.PaginatedItemsViewModel&lt;TEntity&gt;</c>.
/// Preserves the same fields and the <c>TotalPages = ceil(count / pageSize)</c>
/// calculation so paging behavior matches the behavioral baseline.
/// </summary>
public class PaginatedItemsViewModel<TEntity>
    where TEntity : class
{
    public PaginatedItemsViewModel(int pageIndex, int pageSize, long count, IEnumerable<TEntity> data)
    {
        ActualPage = pageIndex;
        ItemsPerPage = pageSize;
        TotalItems = count;
        TotalPages = (int)Math.Ceiling((decimal)count / pageSize);
        Data = data;
    }

    public int ActualPage { get; }

    public int ItemsPerPage { get; }

    public long TotalItems { get; }

    public int TotalPages { get; }

    public IEnumerable<TEntity> Data { get; }
}
