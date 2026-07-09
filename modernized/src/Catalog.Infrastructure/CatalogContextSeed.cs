using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure;

/// <summary>
/// Ported from the legacy <c>PreconfiguredData</c> seed (eShopLegacyMVC):
/// 4 catalog types, 5 catalog brands and 12 catalog items. The dataset is
/// reproduced exactly to preserve the behavioral baseline.
/// </summary>
public static class CatalogContextSeed
{
    public static void SeedCatalog(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CatalogType>().HasData(GetPreconfiguredCatalogTypes());
        modelBuilder.Entity<CatalogBrand>().HasData(GetPreconfiguredCatalogBrands());
        modelBuilder.Entity<CatalogItem>().HasData(GetPreconfiguredCatalogItems());
        modelBuilder.Entity<CatalogItemsStock>().HasData(GetPreconfiguredCatalogItemsStock());
        modelBuilder.Entity<DiscountItem>().HasData(GetPreconfiguredDiscountItems());
    }

    public static IEnumerable<CatalogType> GetPreconfiguredCatalogTypes() =>
        new[]
        {
            new CatalogType { Id = 1, Type = "Mug" },
            new CatalogType { Id = 2, Type = "T-Shirt" },
            new CatalogType { Id = 3, Type = "Sheet" },
            new CatalogType { Id = 4, Type = "USB Memory Stick" },
        };

    public static IEnumerable<CatalogBrand> GetPreconfiguredCatalogBrands() =>
        new[]
        {
            new CatalogBrand { Id = 1, Brand = "Azure" },
            new CatalogBrand { Id = 2, Brand = ".NET" },
            new CatalogBrand { Id = 3, Brand = "Visual Studio" },
            new CatalogBrand { Id = 4, Brand = "SQL Server" },
            new CatalogBrand { Id = 5, Brand = "Other" },
        };

    public static IEnumerable<CatalogItem> GetPreconfiguredCatalogItems() =>
        new[]
        {
            new CatalogItem { Id = 1, CatalogTypeId = 2, CatalogBrandId = 2, AvailableStock = 100, Description = ".NET Bot Black Hoodie", Name = ".NET Bot Black Hoodie", Price = 19.5M, PictureFileName = "1.png" },
            new CatalogItem { Id = 2, CatalogTypeId = 1, CatalogBrandId = 2, AvailableStock = 100, Description = ".NET Black & White Mug", Name = ".NET Black & White Mug", Price = 8.50M, PictureFileName = "2.png" },
            new CatalogItem { Id = 3, CatalogTypeId = 2, CatalogBrandId = 5, AvailableStock = 100, Description = "Prism White T-Shirt", Name = "Prism White T-Shirt", Price = 12, PictureFileName = "3.png" },
            new CatalogItem { Id = 4, CatalogTypeId = 2, CatalogBrandId = 2, AvailableStock = 100, Description = ".NET Foundation T-shirt", Name = ".NET Foundation T-shirt", Price = 12, PictureFileName = "4.png" },
            new CatalogItem { Id = 5, CatalogTypeId = 3, CatalogBrandId = 5, AvailableStock = 100, Description = "Roslyn Red Sheet", Name = "Roslyn Red Sheet", Price = 8.5M, PictureFileName = "5.png" },
            new CatalogItem { Id = 6, CatalogTypeId = 2, CatalogBrandId = 2, AvailableStock = 100, Description = ".NET Blue Hoodie", Name = ".NET Blue Hoodie", Price = 12, PictureFileName = "6.png" },
            new CatalogItem { Id = 7, CatalogTypeId = 2, CatalogBrandId = 5, AvailableStock = 100, Description = "Roslyn Red T-Shirt", Name = "Roslyn Red T-Shirt", Price = 12, PictureFileName = "7.png" },
            new CatalogItem { Id = 8, CatalogTypeId = 2, CatalogBrandId = 5, AvailableStock = 100, Description = "Kudu Purple Hoodie", Name = "Kudu Purple Hoodie", Price = 8.5M, PictureFileName = "8.png" },
            new CatalogItem { Id = 9, CatalogTypeId = 1, CatalogBrandId = 5, AvailableStock = 100, Description = "Cup<T> White Mug", Name = "Cup<T> White Mug", Price = 12, PictureFileName = "9.png" },
            new CatalogItem { Id = 10, CatalogTypeId = 3, CatalogBrandId = 2, AvailableStock = 100, Description = ".NET Foundation Sheet", Name = ".NET Foundation Sheet", Price = 12, PictureFileName = "10.png" },
            new CatalogItem { Id = 11, CatalogTypeId = 3, CatalogBrandId = 2, AvailableStock = 100, Description = "Cup<T> Sheet", Name = "Cup<T> Sheet", Price = 8.5M, PictureFileName = "11.png" },
            new CatalogItem { Id = 12, CatalogTypeId = 2, CatalogBrandId = 5, AvailableStock = 100, Description = "Prism White TShirt", Name = "Prism White TShirt", Price = 12, PictureFileName = "12.png" },
        };

    /// <summary>
    /// Ported from the legacy WCF N-Tier <c>PreconfiguredData.GetPreconfiguredCatalogItemsStock</c>:
    /// 6 date-stamped stock rows across items 1, 2, 4 and 5.
    /// </summary>
    public static IEnumerable<CatalogItemsStock> GetPreconfiguredCatalogItemsStock() =>
        new[]
        {
            new CatalogItemsStock { StockId = 1, CatalogItemId = 1, Date = new DateTime(2017, 9, 20), AvailableStock = 100 },
            new CatalogItemsStock { StockId = 2, CatalogItemId = 1, Date = new DateTime(2017, 9, 21), AvailableStock = 120 },
            new CatalogItemsStock { StockId = 3, CatalogItemId = 1, Date = new DateTime(2017, 9, 22), AvailableStock = 80 },
            new CatalogItemsStock { StockId = 4, CatalogItemId = 2, Date = new DateTime(2017, 9, 20), AvailableStock = 45 },
            new CatalogItemsStock { StockId = 5, CatalogItemId = 4, Date = new DateTime(2017, 9, 25), AvailableStock = 65 },
            new CatalogItemsStock { StockId = 6, CatalogItemId = 5, Date = new DateTime(2017, 9, 28), AvailableStock = 22 },
        };

    /// <summary>
    /// Ported from the legacy WCF N-Tier <c>PreconfiguredData.GetPreconfiguredDiscountItems</c>:
    /// 6 time-bounded discounts. Ids are assigned explicitly for deterministic seeding.
    /// </summary>
    public static IEnumerable<DiscountItem> GetPreconfiguredDiscountItems() =>
        new[]
        {
            new DiscountItem { Id = 1, Start = new DateTime(2017, 9, 18), End = new DateTime(2017, 9, 21), Size = 0.3 },
            new DiscountItem { Id = 2, Start = new DateTime(2017, 9, 22), End = new DateTime(2017, 9, 26), Size = 0.25 },
            new DiscountItem { Id = 3, Start = new DateTime(2017, 9, 27), End = new DateTime(2017, 9, 30), Size = 0.1 },
            new DiscountItem { Id = 4, Start = new DateTime(2017, 10, 5), End = new DateTime(2017, 10, 20), Size = 0.5 },
            new DiscountItem { Id = 5, Start = new DateTime(2017, 11, 13), End = new DateTime(2017, 11, 25), Size = 0.3 },
            new DiscountItem { Id = 6, Start = new DateTime(2017, 12, 20), End = new DateTime(2017, 12, 25), Size = 0.25 },
        };
}
