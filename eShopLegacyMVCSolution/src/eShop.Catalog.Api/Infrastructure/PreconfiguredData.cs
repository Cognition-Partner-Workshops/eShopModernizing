using eShop.Catalog.Api.Model;

namespace eShop.Catalog.Api.Infrastructure;

public static class PreconfiguredData
{
    public static List<CatalogBrand> GetPreconfiguredCatalogBrands() => new()
    {
        new CatalogBrand { Brand = "Azure" },
        new CatalogBrand { Brand = ".NET" },
        new CatalogBrand { Brand = "Visual Studio" },
        new CatalogBrand { Brand = "SQL Server" },
        new CatalogBrand { Brand = "Other" },
    };

    public static List<CatalogType> GetPreconfiguredCatalogTypes() => new()
    {
        new CatalogType { Type = "Mug" },
        new CatalogType { Type = "T-Shirt" },
        new CatalogType { Type = "Sheet" },
        new CatalogType { Type = "USB Memory Stick" },
    };

    public static List<CatalogItem> GetPreconfiguredCatalogItems(IReadOnlyDictionary<string, int> typeIds, IReadOnlyDictionary<string, int> brandIds)
    {
        CatalogItem Item(string type, string brand, string desc, string name, decimal price, string pic, int stock = 100) => new()
        {
            CatalogTypeId = typeIds[type],
            CatalogBrandId = brandIds[brand],
            AvailableStock = stock,
            Description = desc,
            Name = name,
            Price = price,
            PictureFileName = pic,
        };

        return new List<CatalogItem>
        {
            Item("T-Shirt", ".NET", ".NET Bot Black Hoodie, and more", ".NET Bot Black Hoodie", 19.5M, "1.png"),
            Item("Mug", ".NET", ".NET Black & White Mug", ".NET Black & White Mug", 8.50M, "2.png"),
            Item("T-Shirt", "Other", "Prism White T-Shirt", "Prism White T-Shirt", 12, "3.png"),
            Item("T-Shirt", ".NET", ".NET Foundation T-shirt", ".NET Foundation T-shirt", 12, "4.png"),
            Item("Sheet", "Other", "Roslyn Red Sheet", "Roslyn Red Sheet", 8.5M, "5.png"),
            Item("T-Shirt", ".NET", ".NET Blue Hoodie", ".NET Blue Hoodie", 12, "6.png"),
            Item("T-Shirt", "Other", "Roslyn Red T-Shirt", "Roslyn Red T-Shirt", 12, "7.png"),
            Item("T-Shirt", "Other", "Kudu Purple Hoodie", "Kudu Purple Hoodie", 8.5M, "8.png"),
            Item("Mug", "Other", "Cup<T> White Mug", "Cup<T> White Mug", 12, "9.png"),
            Item("Sheet", ".NET", ".NET Foundation Sheet", ".NET Foundation Sheet", 12, "10.png"),
            Item("Sheet", ".NET", "Cup<T> Sheet", "Cup<T> Sheet", 8.5M, "11.png"),
            Item("T-Shirt", "Other", "Prism White TShirt", "Prism White TShirt", 12, "12.png"),
        };
    }
}
