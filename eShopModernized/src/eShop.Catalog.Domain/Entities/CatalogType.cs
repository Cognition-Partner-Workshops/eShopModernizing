using System.ComponentModel.DataAnnotations;

namespace eShop.Catalog.Domain.Entities;

public class CatalogType
{
    public int Id { get; set; }

    [StringLength(50)]
    public string Type { get; set; } = string.Empty;
}
