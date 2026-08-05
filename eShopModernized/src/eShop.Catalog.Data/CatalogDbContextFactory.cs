using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace eShop.Catalog.Data;

/// <summary>
/// Design-time factory used by <c>dotnet ef</c>. The connection string is only needed to
/// pick the provider when scaffolding migrations; override it with the
/// <c>ConnectionStrings__Catalog</c> environment variable to target a real database.
/// </summary>
public class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    private const string DesignTimeConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=eShopCatalog;Trusted_Connection=True;MultipleActiveResultSets=true";

    public CatalogDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Catalog") ?? DesignTimeConnectionString;

        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new CatalogDbContext(options);
    }
}
