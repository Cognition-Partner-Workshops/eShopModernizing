using eShop.Catalog.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace eShop.Catalog.Data.Tests;

public class AddCatalogDataTests
{
    [Fact]
    public void UseMockData_RegistersTheInMemoryService()
    {
        var provider = Build(new Dictionary<string, string?> { ["UseMockData"] = "true" });

        Assert.IsType<MockCatalogService>(provider.GetRequiredService<ICatalogService>());
    }

    [Fact]
    public void ConnectionString_RegistersTheEfCoreServiceAndContext()
    {
        var provider = Build(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Catalog"] = "Server=none;Database=none;Trusted_Connection=True;",
        });

        using var scope = provider.CreateScope();

        Assert.IsType<CatalogService>(scope.ServiceProvider.GetRequiredService<ICatalogService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<CatalogDbContext>());
    }

    [Fact]
    public void NoConnectionStringAndNoMockData_FailsFast()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        Assert.Throws<InvalidOperationException>(() => services.AddCatalogData(configuration));
    }

    private static ServiceProvider Build(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return new ServiceCollection().AddCatalogData(configuration).BuildServiceProvider();
    }
}
