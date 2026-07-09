using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Catalog.Infrastructure;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can build the model and generate
/// migrations without a running application host or a live database.
/// </summary>
public class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=Microsoft.eShopOnContainers.Services.CatalogDb;Trusted_Connection=True;")
            .Options;

        return new CatalogDbContext(options);
    }
}
