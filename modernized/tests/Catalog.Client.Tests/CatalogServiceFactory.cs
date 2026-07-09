using Catalog.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Client.Tests;

/// <summary>
/// Boots the real Catalog.Service gRPC host in-process but swaps its SQL Server
/// <see cref="CatalogDbContext"/> for an in-memory Sqlite connection seeded with
/// the behavioral baseline data, so <see cref="CatalogGrpcClient"/> can be
/// exercised over the full gRPC pipeline on CI with no external database.
/// </summary>
public class CatalogServiceFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public CatalogServiceFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // AddCatalogInfrastructure reads the connection string eagerly while the
        // host is being built (before ConfigureWebHost callbacks apply), so supply
        // a non-empty placeholder via the environment. The SQL Server provider it
        // registers is replaced with Sqlite below.
        Environment.SetEnvironmentVariable("ConnectionStrings__CatalogDb", "PlaceholderForTests");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            RemoveCatalogDbContextRegistrations(services);

            services.AddDbContext<CatalogDbContext>(options =>
                options.UseSqlite(_connection));

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            dbContext.Database.EnsureCreated();
        });
    }

    private static void RemoveCatalogDbContextRegistrations(IServiceCollection services)
    {
        var descriptors = services
            .Where(descriptor =>
                descriptor.ServiceType == typeof(DbContextOptions<CatalogDbContext>)
                || descriptor.ServiceType == typeof(DbContextOptions)
                || descriptor.ServiceType == typeof(CatalogDbContext)
                || (descriptor.ServiceType.IsGenericType
                    && descriptor.ServiceType.GetGenericArguments().Contains(typeof(CatalogDbContext))))
            .ToList();

        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
