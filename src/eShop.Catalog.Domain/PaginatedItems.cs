namespace eShop.Catalog.Domain;

/// <summary>
/// Page of results, carrying the same fields as the legacy PaginatedItemsViewModel&lt;T&gt;.
/// </summary>
public class PaginatedItems<TEntity>
    where TEntity : class
{
    public PaginatedItems(int pageIndex, int pageSize, long count, IEnumerable<TEntity> data)
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

    public int TotalPages { get; set; }

    public IEnumerable<TEntity> Data { get; }
}
