using System.ComponentModel.DataAnnotations;

namespace eShop.Catalog.Domain;

public class CatalogType
{
    public int Id { get; set; }

    [StringLength(50)]
    public string Type { get; set; } = string.Empty;
}
