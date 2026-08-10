using System.Net;
using eShop.Catalog.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace eShop.Catalog.Api.Tests;

/// <summary>Acceptance check for NET-61: the API boots with mock data on and off.</summary>
public class HostBootTests
{
    [Fact]
    public async Task Boots_WithMockData()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Catalog:UseMockData"] = "true",
        });

        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        Assert.IsType<MockCatalogService>(scope.ServiceProvider.GetRequiredService<ICatalogService>());
    }

    [Fact]
    public async Task Boots_WithoutMockData_WhenAConnectionStringIsConfigured()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Catalog:UseMockData"] = "false",
            ["Catalog:InitializeDatabaseOnStartup"] = "false",
            ["ConnectionStrings:Catalog"] = "Server=db;Database=Catalog;",
        });

        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        Assert.IsType<CatalogService>(scope.ServiceProvider.GetRequiredService<ICatalogService>());
    }

    private static WebApplicationFactory<Program> CreateFactory(IDictionary<string, string?> settings)
        => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            foreach (var (key, value) in settings)
            {
                builder.UseSetting(key, value);
            }
        });
}
