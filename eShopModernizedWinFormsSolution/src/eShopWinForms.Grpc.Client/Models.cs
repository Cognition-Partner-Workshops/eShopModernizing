namespace eShopWinForms.Grpc.Client.Models;

public sealed class CatalogBrand
{
    public int Id { get; set; }
    public string Brand { get; set; } = "";
}

public sealed class CatalogType
{
    public int Id { get; set; }
    public string Type { get; set; } = "";
}

public sealed class CatalogItem
{
    public int Id { get; set; }
    public string? Description { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string Picturefilename { get; set; } = "";
    public int CatalogBrandId { get; set; }
    public int CatalogTypeId { get; set; }
    public CatalogBrand? CatalogBrand { get; set; }
    public CatalogType? CatalogType { get; set; }
}

public sealed class CatalogItemsStock
{
    public int StockId { get; set; }
    public DateTime Date { get; set; }
    public int CatalogItemId { get; set; }
    public int AvailableStock { get; set; }
}

public sealed class DiscountItem
{
    public int Id { get; set; }
    public double Size { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}
