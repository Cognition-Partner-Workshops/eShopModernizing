using Catalog.Domain;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Catalog.Mvc.Models;

/// <summary>
/// View model for the catalog listing action. Replaces the legacy
/// <c>ViewBag</c>/<c>SelectList</c> plumbing with an explicit model carrying the
/// paged items plus the brand/type filter dropdowns and current selections.
/// </summary>
public class CatalogIndexViewModel
{
    public required PaginatedItemsViewModel<CatalogItem> Items { get; init; }

    public required SelectList Brands { get; init; }

    public required SelectList Types { get; init; }

    public int? BrandFilter { get; init; }

    public int? TypeFilter { get; init; }
}
