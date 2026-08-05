using eShop.Catalog.Data.Services;
using eShop.Catalog.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace eShop.Catalog.Data.DependencyInjection;

public static class CatalogDataServiceCollectionExtensions
{
    /// <summary>
    /// Configuration key holding the catalog connection string.
    /// </summary>
    public const string ConnectionStringName = "Catalog";

    /// <summary>
    /// Configuration key selecting the in-memory catalog data (legacy <c>UseMockData</c> app setting).
    /// </summary>
    public const string UseMockDataKey = "Catalog:UseMockData";

    /// <summary>
    /// Registers the catalog data layer: the EF Core <see cref="CatalogDbContext" /> on the
    /// <c>ConnectionStrings:Catalog</c> connection string and an <see cref="ICatalogService" />
    /// implementation chosen by <c>Catalog:UseMockData</c>.
    /// </summary>
    public static IServiceCollection AddCatalogData(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (bool.TryParse(configuration[UseMockDataKey], out var useMockData) && useMockData)
        {
            services.AddSingleton<ICatalogService, CatalogServiceMock>();
            return services;
        }

        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string 'ConnectionStrings:{ConnectionStringName}' is not configured and '{UseMockDataKey}' is not true.");
        }

        services.AddDbContext<CatalogDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<ICatalogService, CatalogService>();

        return services;
    }
}
