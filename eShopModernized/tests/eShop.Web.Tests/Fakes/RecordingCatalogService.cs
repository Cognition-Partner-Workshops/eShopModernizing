using eShop.Catalog.Domain;
using eShop.Catalog.Domain.Abstractions;
using eShop.Catalog.Domain.Entities;

namespace eShop.Web.Tests.Fakes;

/// <summary>
/// Hand-rolled <see cref="ICatalogService" /> double replacing the Moq mock the legacy MSTest
/// controller tests used: it returns the configured catalog and records every write call so the
/// ported tests can make the same assertions.
/// </summary>
public sealed class RecordingCatalogService : ICatalogService
{
    public List<CatalogBrand> Brands { get; init; } = [];

    public List<CatalogType> Types { get; init; } = [];

    public List<CatalogItem> Items { get; init; } = [];

    public List<CatalogItemsStock> Stocks { get; init; } = [];

    public List<DiscountItem> Discounts { get; init; } = [];

    /// <summary>Result handed back by <see cref="GetCatalogItemsPaginated" />.</summary>
    public PaginatedItemsViewModel<CatalogItem>? PaginatedResult { get; set; }

    /// <summary>Every <c>(pageSize, pageIndex)</c> pair the controller asked for.</summary>
    public List<(int PageSize, int PageIndex)> PaginationCalls { get; } = [];

    public List<CatalogItem> Created { get; } = [];

    public List<CatalogItem> Updated { get; } = [];

    public List<CatalogItem> Removed { get; } = [];

    public CatalogItem? FindCatalogItem(int id) => Items.Find(i => i.Id == id);

    public Task<CatalogItem?> FindCatalogItemAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(FindCatalogItem(id));

    public IEnumerable<CatalogBrand> GetCatalogBrands() => Brands;

    public Task<IEnumerable<CatalogBrand>> GetCatalogBrandsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(GetCatalogBrands());

    public IEnumerable<CatalogType> GetCatalogTypes() => Types;

    public Task<IEnumerable<CatalogType>> GetCatalogTypesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(GetCatalogTypes());

    public PaginatedItemsViewModel<CatalogItem> GetCatalogItemsPaginated(int pageSize = 10, int pageIndex = 0)
    {
        PaginationCalls.Add((pageSize, pageIndex));

        return PaginatedResult
            ?? new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, Items.Count, Items);
    }

    public Task<PaginatedItemsViewModel<CatalogItem>> GetCatalogItemsPaginatedAsync(int pageSize = 10, int pageIndex = 0, CancellationToken cancellationToken = default)
        => Task.FromResult(GetCatalogItemsPaginated(pageSize, pageIndex));

    public void CreateCatalogItem(CatalogItem catalogItem) => Created.Add(catalogItem);

    public Task CreateCatalogItemAsync(CatalogItem catalogItem, CancellationToken cancellationToken = default)
    {
        CreateCatalogItem(catalogItem);
        return Task.CompletedTask;
    }

    public void UpdateCatalogItem(CatalogItem catalogItem) => Updated.Add(catalogItem);

    public Task UpdateCatalogItemAsync(CatalogItem catalogItem, CancellationToken cancellationToken = default)
    {
        UpdateCatalogItem(catalogItem);
        return Task.CompletedTask;
    }

    public void RemoveCatalogItem(CatalogItem catalogItem) => Removed.Add(catalogItem);

    public Task RemoveCatalogItemAsync(CatalogItem catalogItem, CancellationToken cancellationToken = default)
    {
        RemoveCatalogItem(catalogItem);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<CatalogItem>> GetCatalogItemsAsync(int brandIdFilter, int typeIdFilter, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.Where(i =>
            (brandIdFilter == 0 || i.CatalogBrandId == brandIdFilter) &&
            (typeIdFilter == 0 || i.CatalogTypeId == typeIdFilter)));

    public Task<int> GetAvailableStockAsync(DateTime date, int catalogItemId, CancellationToken cancellationToken = default)
        => Task.FromResult(Stocks.Find(s => s.CatalogItemId == catalogItemId && s.Date.Date == date.Date)?.AvailableStock ?? 0);

    public Task CreateAvailableStockAsync(CatalogItemsStock catalogItemsStock, CancellationToken cancellationToken = default)
    {
        Stocks.Add(catalogItemsStock);
        return Task.CompletedTask;
    }

    public Task<DiscountItem?> GetDiscountAsync(DateTime day, CancellationToken cancellationToken = default)
        => Task.FromResult(Discounts.Find(d => d.Start.Date <= day.Date && d.End.Date >= day.Date));
}
