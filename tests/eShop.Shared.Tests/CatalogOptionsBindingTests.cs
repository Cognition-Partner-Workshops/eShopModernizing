using eShop.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace eShop.Shared.Tests;

public class CatalogOptionsBindingTests
{
    [Fact]
    public void Options_BindFromJsonConfiguration()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Catalog:UseMockData"] = "true",
            ["Catalog:UseCustomizationData"] = "true",
            ["ConnectionStrings:Catalog"] = "Server=json;Database=Catalog;",
        });

        using var provider = BuildProvider(configuration);

        var catalog = provider.GetRequiredService<IOptions<CatalogOptions>>().Value;
        var connection = provider.GetRequiredService<IOptions<CatalogConnectionOptions>>().Value;

        Assert.True(catalog.UseMockData);
        Assert.True(catalog.UseCustomizationData);
        Assert.Equal("Server=json;Database=Catalog;", connection.Catalog);
    }

    [Fact]
    public void Options_DefaultToFalse_WhenAbsent()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>());

        using var provider = BuildProvider(configuration);

        var catalog = provider.GetRequiredService<IOptions<CatalogOptions>>().Value;

        Assert.False(catalog.UseMockData);
        Assert.False(catalog.UseCustomizationData);
    }

    [Fact]
    public void Options_BindFromEnvironmentVariables()
    {
        using var environment = new ScopedEnvironmentVariables(
            ("Catalog__UseMockData", "true"),
            ("Catalog__UseCustomizationData", "true"),
            ("ConnectionStrings__Catalog", "Server=env;Database=Catalog;"));

        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();

        using var provider = BuildProvider(configuration);

        var catalog = provider.GetRequiredService<IOptions<CatalogOptions>>().Value;
        var connection = provider.GetRequiredService<IOptions<CatalogConnectionOptions>>().Value;

        Assert.True(catalog.UseMockData);
        Assert.True(catalog.UseCustomizationData);
        Assert.Equal("Server=env;Database=Catalog;", connection.Catalog);
    }

    [Fact]
    public void ConnectionString_IsRequired_WhenNotUsingMockData()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Catalog:UseMockData"] = "false",
        });

        using var provider = BuildProvider(configuration);

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<CatalogConnectionOptions>>().Value);

        Assert.Contains("ConnectionStrings__Catalog", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConnectionString_IsOptional_WhenUsingMockData()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Catalog:UseMockData"] = "true",
        });

        using var provider = BuildProvider(configuration);

        Assert.Null(provider.GetRequiredService<IOptions<CatalogConnectionOptions>>().Value.Catalog);
    }

    private static IConfiguration BuildConfiguration(IDictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static ServiceProvider BuildProvider(IConfiguration configuration)
        => new ServiceCollection().AddEShopServices(configuration).BuildServiceProvider();
}
