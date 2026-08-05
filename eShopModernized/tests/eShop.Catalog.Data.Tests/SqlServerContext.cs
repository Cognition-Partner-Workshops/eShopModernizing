using Microsoft.EntityFrameworkCore;

namespace eShop.Catalog.Data.Tests;

/// <summary>
/// SQL Server-configured context used for model/migration assertions. No server is contacted:
/// the model, the migrations assembly and the migration SQL are all produced offline.
/// </summary>
internal static class SqlServerContext
{
    public static CatalogDbContext Create()
        => new(new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlServer("Server=localhost;Database=eShopCatalog;Trusted_Connection=True")
            .Options);
}
