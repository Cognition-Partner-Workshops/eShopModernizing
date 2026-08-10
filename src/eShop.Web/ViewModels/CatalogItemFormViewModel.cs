using eShop.Catalog.Domain;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace eShop.Web.ViewModels;

/// <summary>
/// Typed replacement for the legacy <c>ViewBag.CatalogBrandId</c> / <c>ViewBag.CatalogTypeId</c>
/// <c>SelectList</c>s used by the Create and Edit views.
/// </summary>
public sealed class CatalogItemFormViewModel
{
    public CatalogItemFormViewModel(
        CatalogItem item,
        IEnumerable<CatalogBrand> brands,
        IEnumerable<CatalogType> types)
    {
        Item = item;
        Brands = new SelectList(brands, nameof(CatalogBrand.Id), nameof(CatalogBrand.Brand), item.CatalogBrandId);
        Types = new SelectList(types, nameof(CatalogType.Id), nameof(CatalogType.Type), item.CatalogTypeId);
    }

    public CatalogItem Item { get; }

    /// <summary>Brand options, selected value bound to <c>Item.CatalogBrandId</c>.</summary>
    public SelectList Brands { get; }

    /// <summary>Type options, selected value bound to <c>Item.CatalogTypeId</c>.</summary>
    public SelectList Types { get; }
}
