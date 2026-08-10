using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace eShop.Catalog.Data;

/// <summary>
/// Single entry point every host uses to get the catalog data layer. Hosts call
/// <c>builder.Services.AddCatalogData(builder.Configuration)</c>; nothing else in Program.cs has to
/// know about EF Core.
/// </summary>
public static class CatalogDataServiceCollectionExtensions
{
    /// <summary>Configuration key that selects the in-memory mock data set (legacy UseMockData).</summary>
    public const string UseMockDataKey = "UseMockData";

    /// <summary>Name of the catalog connection string (legacy CatalogDBContext connection).</summary>
    public const string ConnectionStringName = "Catalog";

    public static IServiceCollection AddCatalogData(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (configuration.GetValue<bool>(UseMockDataKey))
        {
            services.AddSingleton<ICatalogService, MockCatalogService>();
            return services;
        }

        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured. Set ConnectionStrings__{ConnectionStringName} " +
                $"or enable {UseMockDataKey} to run against the mock data set.");

        services.AddDbContext<CatalogDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<ICatalogItemIdGenerator, MaxCatalogItemIdGenerator>();
        services.AddScoped<ICatalogService, CatalogService>();

        return services;
    }
}
