using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure;

public static class DependencyInjection
{
    public const string CatalogDbConnectionName = "CatalogDb";

    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(CatalogDbConnectionName);

        services.AddDbContext<CatalogDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }
}
