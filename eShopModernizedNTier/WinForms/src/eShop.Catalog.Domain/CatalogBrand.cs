using System.ComponentModel.DataAnnotations;

namespace eShop.Catalog.Domain;

public class CatalogBrand
{
    public int Id { get; set; }

    [StringLength(50)]
    public string Brand { get; set; } = string.Empty;
}
