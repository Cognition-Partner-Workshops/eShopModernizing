using System.ComponentModel.DataAnnotations;

namespace Catalog.Mvc.Models;

/// <summary>
/// Create/edit form model for a catalog item. Replaces the legacy
/// <c>[Bind(Include = ...)]</c> over the EF entity with an explicit view model,
/// carrying the same fields, bounds and validation messages while preventing
/// overposting of navigation/identity fields.
/// </summary>
public class CatalogItemInputModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "The Name field is required.")]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Display(Name = "Brand")]
    public int CatalogBrandId { get; set; }

    [Display(Name = "Type")]
    public int CatalogTypeId { get; set; }

    [Range(0, 1_000_000, ErrorMessage = "The Price must be a positive number with maximum two decimals between 0 and 1 million.")]
    public decimal Price { get; set; }

    [Display(Name = "Picture name")]
    public string PictureFileName { get; set; } = CatalogItemDefaults.PictureFileName;

    [Display(Name = "Stock")]
    [Range(0, 10_000_000, ErrorMessage = "The field Stock must be between 0 and 10 million.")]
    public int AvailableStock { get; set; }

    [Display(Name = "Restock")]
    [Range(0, 10_000_000, ErrorMessage = "The field Restock must be between 0 and 10 million.")]
    public int RestockThreshold { get; set; }

    [Display(Name = "Max stock")]
    [Range(0, 10_000_000, ErrorMessage = "The field Max stock must be between 0 and 10 million.")]
    public int MaxStockThreshold { get; set; }
}

/// <summary>Shared defaults for a new catalog item form.</summary>
public static class CatalogItemDefaults
{
    public const string PictureFileName = Catalog.Domain.CatalogItem.DefaultPictureName;
}
