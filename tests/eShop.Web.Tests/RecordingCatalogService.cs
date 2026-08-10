using eShop.Catalog.Data;
using eShop.Catalog.Domain;

namespace eShop.Web.Tests;

/// <summary>
/// Hand-written <see cref="ICatalogService"/> test double replacing the legacy Moq mock: it returns
/// canned data and records the calls the controller tests assert on.
/// </summary>
public sealed class RecordingCatalogService : ICatalogService
{
    public List<CatalogBrand> Brands { get; } =
    [
        new() { Id = 1, Brand = "Azure" },
        new() { Id = 2, Brand = ".NET" },
    ];

    public List<CatalogType> Types { get; } =
    [
        new() { Id = 1, Type = "Mug" },
        new() { Id = 2, Type = "T-Shirt" },
    ];

    public List<CatalogItem> Items { get; } = [];

    /// <summary>Page returned by <see cref="GetCatalogItemsPaginated"/>, when set.</summary>
    public PaginatedItems<CatalogItem>? Page { get; set; }

    public List<(int PageSize, int PageIndex)> PaginatedCalls { get; } = [];

    public List<CatalogItem> Created { get; } = [];

    public List<CatalogItem> Updated { get; } = [];

    public List<CatalogItem> Removed { get; } = [];

    public CatalogItem? FindCatalogItem(int id) => Items.FirstOrDefault(i => i.Id == id);

    public IEnumerable<CatalogBrand> GetCatalogBrands() => Brands;

    public IEnumerable<CatalogType> GetCatalogTypes() => Types;

    public IReadOnlyList<CatalogItem> GetCatalogItems(int brandIdFilter, int typeIdFilter) => Items;

    public PaginatedItems<CatalogItem> GetCatalogItemsPaginated(int pageSize, int pageIndex)
    {
        PaginatedCalls.Add((pageSize, pageIndex));

        return Page ?? new PaginatedItems<CatalogItem>(pageIndex, pageSize, 0, []);
    }

    public void CreateCatalogItem(CatalogItem catalogItem) => Created.Add(catalogItem);

    public void UpdateCatalogItem(CatalogItem catalogItem) => Updated.Add(catalogItem);

    public void RemoveCatalogItem(CatalogItem catalogItem) => Removed.Add(catalogItem);
}
