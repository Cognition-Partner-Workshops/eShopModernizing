using Catalog.Domain;
using Catalog.Infrastructure;
using Catalog.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Web.Pages.Catalog;

/// <summary>
/// Catalog listing page, ported from the legacy Web Forms <c>Default.aspx</c>.
/// Preserves the baseline paging (default size 10, index 0, <c>Skip</c>/<c>Take</c>
/// ordered by <c>Id</c>) and adds brand/type filter dropdowns.
/// </summary>
public class IndexModel : PageModel
{
    public const int DefaultPageIndex = 0;
    public const int DefaultPageSize = 10;

    private readonly CatalogDbContext _db;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(CatalogDbContext db, ILogger<IndexModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true, Name = "size")]
    public int PageSize { get; set; } = DefaultPageSize;

    [BindProperty(SupportsGet = true, Name = "index")]
    public int PageIndex { get; set; } = DefaultPageIndex;

    [BindProperty(SupportsGet = true)]
    public int? BrandFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? TypeFilter { get; set; }

    public PaginatedItemsViewModel<CatalogItem> Model { get; private set; } = null!;

    public SelectList Brands { get; private set; } = null!;

    public SelectList Types { get; private set; } = null!;

    public async Task OnGetAsync()
    {
        var pageSize = PageSize > 0 ? PageSize : DefaultPageSize;
        var pageIndex = PageIndex >= 0 ? PageIndex : DefaultPageIndex;
        PageSize = pageSize;
        PageIndex = pageIndex;

        _logger.LogInformation(
            "Now loading... /Catalog/Index?size={PageSize}&index={PageIndex}&brand={BrandFilter}&type={TypeFilter}",
            pageSize, pageIndex, BrandFilter, TypeFilter);

        IQueryable<CatalogItem> query = _db.CatalogItems
            .Include(c => c.CatalogBrand)
            .Include(c => c.CatalogType);

        if (BrandFilter is > 0)
        {
            query = query.Where(c => c.CatalogBrandId == BrandFilter);
        }

        if (TypeFilter is > 0)
        {
            query = query.Where(c => c.CatalogTypeId == TypeFilter);
        }

        var totalItems = await query.LongCountAsync();

        var itemsOnPage = await query
            .OrderBy(c => c.Id)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync();

        Model = new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage);

        await PopulateFiltersAsync();
    }

    private async Task PopulateFiltersAsync()
    {
        var brands = await _db.CatalogBrands.OrderBy(b => b.Id).ToListAsync();
        var types = await _db.CatalogTypes.OrderBy(t => t.Id).ToListAsync();

        Brands = new SelectList(brands, nameof(CatalogBrand.Id), nameof(CatalogBrand.Brand), BrandFilter);
        Types = new SelectList(types, nameof(CatalogType.Id), nameof(CatalogType.Type), TypeFilter);
    }
}
