using eShop.Catalog.Api.Model;
using Microsoft.EntityFrameworkCore;

namespace eShop.Catalog.Api.Infrastructure;

public static class CatalogContextSeed
{
    public static void Seed(CatalogContext context, IHostEnvironment env, bool useCustomizationData, ILogger logger)
    {
        if (context.CatalogBrands.Any() || context.CatalogTypes.Any() || context.CatalogItems.Any())
        {
            return;
        }

        var setupDir = Path.Combine(env.ContentRootPath, "Setup");

        var types = useCustomizationData
            ? CsvSetupReader.ReadTypes(Path.Combine(setupDir, "CatalogTypes.csv")) ?? PreconfiguredData.GetPreconfiguredCatalogTypes()
            : PreconfiguredData.GetPreconfiguredCatalogTypes();
        context.CatalogTypes.AddRange(types);
        context.SaveChanges();

        var brands = useCustomizationData
            ? CsvSetupReader.ReadBrands(Path.Combine(setupDir, "CatalogBrands.csv")) ?? PreconfiguredData.GetPreconfiguredCatalogBrands()
            : PreconfiguredData.GetPreconfiguredCatalogBrands();
        context.CatalogBrands.AddRange(brands);
        context.SaveChanges();

        var typeIds = context.CatalogTypes.ToDictionary(t => t.Type, t => t.Id);
        var brandIds = context.CatalogBrands.ToDictionary(b => b.Brand, b => b.Id);
        var items = useCustomizationData
            ? CsvSetupReader.ReadItems(Path.Combine(setupDir, "CatalogItems.csv"), typeIds, brandIds) ?? PreconfiguredData.GetPreconfiguredCatalogItems(typeIds, brandIds)
            : PreconfiguredData.GetPreconfiguredCatalogItems(typeIds, brandIds);
        context.CatalogItems.AddRange(items);
        context.SaveChanges();

        logger.LogInformation("Seeded catalog with {Types} types, {Brands} brands, {Items} items",
            context.CatalogTypes.Count(), context.CatalogBrands.Count(), context.CatalogItems.Count());
    }
}
