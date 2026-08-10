using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace eShop.Catalog.Data;

/// <summary>
/// Used only by <c>dotnet ef</c>. The connection string comes from the environment
/// (<c>ConnectionStrings__CatalogDbContext</c>) and falls back to a placeholder, because migration
/// scaffolding never opens a connection.
/// </summary>
public class CatalogDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable($"ConnectionStrings__{CatalogDataServiceCollectionExtensions.ConnectionStringName}")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=Microsoft.eShopOnContainers.Services.CatalogDb;Trusted_Connection=True;";

        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new CatalogDbContext(options);
    }
}
