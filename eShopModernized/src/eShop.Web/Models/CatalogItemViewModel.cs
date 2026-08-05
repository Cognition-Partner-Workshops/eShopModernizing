using System.ComponentModel.DataAnnotations;
using eShop.Catalog.Domain.Entities;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace eShop.Web.Models;

/// <summary>
/// Editable catalog item plus the brand and type option lists, replacing the legacy MVC 5
/// <c>ViewBag.CatalogBrandId</c>/<c>ViewBag.CatalogTypeId</c> <c>SelectList</c>s. The scalar
/// properties are flat so the posted field names stay identical to the legacy forms.
/// </summary>
public class CatalogItemViewModel
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [RegularExpression(@"^\d+(\.\d{0,2})*$", ErrorMessage = "The field Price must be a positive number with maximum two decimals.")]
    [Range(0, 1000000)]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    [Display(Name = "Picture name")]
    public string PictureFileName { get; set; } = CatalogItem.DefaultPictureName;

    [BindNever]
    public string? PictureUri { get; set; }

    [Display(Name = "Type")]
    public int CatalogTypeId { get; set; }

    [Display(Name = "Brand")]
    public int CatalogBrandId { get; set; }

    [Range(0, 10000000, ErrorMessage = "The field Stock must be between 0 and 10 million.")]
    [Display(Name = "Stock")]
    public int AvailableStock { get; set; }

    [Range(0, 10000000, ErrorMessage = "The field Restock must be between 0 and 10 million.")]
    [Display(Name = "Restock")]
    public int RestockThreshold { get; set; }

    [Range(0, 10000000, ErrorMessage = "The field Max stock must be between 0 and 10 million.")]
    [Display(Name = "Max stock")]
    public int MaxStockThreshold { get; set; }

    public bool OnReorder { get; set; }

    /// <summary>Brand options rendered by the <c>CatalogBrandId</c> select.</summary>
    [BindNever]
    public IReadOnlyList<SelectListItem> Brands { get; set; } = [];

    /// <summary>Type options rendered by the <c>CatalogTypeId</c> select.</summary>
    [BindNever]
    public IReadOnlyList<SelectListItem> Types { get; set; } = [];

    /// <summary>Projects a catalog item onto the form model.</summary>
    public static CatalogItemViewModel FromCatalogItem(CatalogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return new CatalogItemViewModel
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            PictureFileName = item.PictureFileName,
            PictureUri = item.PictureUri,
            CatalogTypeId = item.CatalogTypeId,
            CatalogBrandId = item.CatalogBrandId,
            AvailableStock = item.AvailableStock,
            RestockThreshold = item.RestockThreshold,
            MaxStockThreshold = item.MaxStockThreshold,
            OnReorder = item.OnReorder,
        };
    }

    /// <summary>Materialises the catalog item the service layer persists.</summary>
    public CatalogItem ToCatalogItem() => new()
    {
        Id = Id,
        Name = Name,
        Description = Description,
        Price = Price,
        PictureFileName = PictureFileName,
        PictureUri = PictureUri,
        CatalogTypeId = CatalogTypeId,
        CatalogBrandId = CatalogBrandId,
        AvailableStock = AvailableStock,
        RestockThreshold = RestockThreshold,
        MaxStockThreshold = MaxStockThreshold,
        OnReorder = OnReorder,
    };

    /// <summary>Fills <see cref="Brands"/> and <see cref="Types"/>, selecting the current values.</summary>
    public CatalogItemViewModel WithOptions(IEnumerable<CatalogBrand> brands, IEnumerable<CatalogType> types)
    {
        ArgumentNullException.ThrowIfNull(brands);
        ArgumentNullException.ThrowIfNull(types);

        Brands = brands
            .Select(b => new SelectListItem(b.Brand, b.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), b.Id == CatalogBrandId))
            .ToList();

        Types = types
            .Select(t => new SelectListItem(t.Type, t.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), t.Id == CatalogTypeId))
            .ToList();

        return this;
    }
}
