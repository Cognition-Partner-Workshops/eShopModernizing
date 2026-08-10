using System.ComponentModel.DataAnnotations;

namespace eShop.Catalog.Domain;

/// <summary>
/// Canonical catalog item, reconciled from the MVC, Web Forms and WCF copies of the model.
/// </summary>
public class CatalogItem
{
    public const string DefaultPictureName = "dummy.png";

    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    // decimal(18,2)
    [RegularExpression(@"^\d+(\.\d{0,2})*$", ErrorMessage = "The field Price must be a positive number with maximum two decimals.")]
    [Range(0, 1000000)]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    [Display(Name = "Picture name")]
    public string PictureFileName { get; set; } = DefaultPictureName;

    public string? PictureUri { get; set; }

    [Display(Name = "Type")]
    public int CatalogTypeId { get; set; }

    [Display(Name = "Type")]
    public CatalogType? CatalogType { get; set; }

    [Display(Name = "Brand")]
    public int CatalogBrandId { get; set; }

    [Display(Name = "Brand")]
    public CatalogBrand? CatalogBrand { get; set; }

    /// <summary>Quantity in stock.</summary>
    [Range(0, 10000000, ErrorMessage = "The field Stock must be between 0 and 10 million.")]
    [Display(Name = "Stock")]
    public int AvailableStock { get; set; }

    /// <summary>Available stock at which we should reorder.</summary>
    [Range(0, 10000000, ErrorMessage = "The field Restock must be between 0 and 10 million.")]
    [Display(Name = "Restock")]
    public int RestockThreshold { get; set; }

    /// <summary>Maximum number of units that can be in stock at any time.</summary>
    [Range(0, 10000000, ErrorMessage = "The field Max stock must be between 0 and 10 million.")]
    [Display(Name = "Max stock")]
    public int MaxStockThreshold { get; set; }

    /// <summary>True if the item is on reorder.</summary>
    public bool OnReorder { get; set; }
}
