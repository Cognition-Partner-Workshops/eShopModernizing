using eShop.Catalog.Data;
using eShop.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace eShop.Shared.Tests;

public class CatalogServiceRegistrationTests
{
    [Fact]
    public void ResolvesMockCatalogService_WhenUseMockDataIsTrue()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Catalog:UseMockData"] = "true",
        });

        Assert.IsType<MockCatalogService>(provider.GetRequiredService<ICatalogService>());
    }

    [Fact]
    public void MockCatalogService_IsASingleton()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Catalog:UseMockData"] = "true",
        });

        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        Assert.Same(
            firstScope.ServiceProvider.GetRequiredService<ICatalogService>(),
            secondScope.ServiceProvider.GetRequiredService<ICatalogService>());
    }

    [Fact]
    public void ResolvesDatabaseCatalogService_WhenUseMockDataIsFalse()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Catalog:UseMockData"] = "false",
            ["ConnectionStrings:Catalog"] = "Server=db;Database=Catalog;",
        });

        using var scope = provider.CreateScope();

        Assert.IsType<PendingDatabaseCatalogService>(scope.ServiceProvider.GetRequiredService<ICatalogService>());
    }

    [Fact]
    public void DatabaseCatalogService_IsScoped()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Catalog:UseMockData"] = "false",
            ["ConnectionStrings:Catalog"] = "Server=db;Database=Catalog;",
        });

        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var first = firstScope.ServiceProvider.GetRequiredService<ICatalogService>();

        Assert.Same(first, firstScope.ServiceProvider.GetRequiredService<ICatalogService>());
        Assert.NotSame(first, secondScope.ServiceProvider.GetRequiredService<ICatalogService>());
    }

    [Fact]
    public void DatabaseInitializer_IsOnlyRegistered_WhenNotUsingMockData()
    {
        using var mockProvider = BuildProvider(new Dictionary<string, string?>
        {
            ["Catalog:UseMockData"] = "true",
        });
        using var databaseProvider = BuildProvider(new Dictionary<string, string?>
        {
            ["Catalog:UseMockData"] = "false",
            ["ConnectionStrings:Catalog"] = "Server=db;Database=Catalog;",
        });

        using var mockScope = mockProvider.CreateScope();
        using var databaseScope = databaseProvider.CreateScope();

        Assert.Null(mockScope.ServiceProvider.GetService<ICatalogDatabaseInitializer>());
        Assert.NotNull(databaseScope.ServiceProvider.GetService<ICatalogDatabaseInitializer>());
    }

    private static ServiceProvider BuildProvider(IDictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        return new ServiceCollection()
            .AddEShopCatalogServices(configuration)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }
}
