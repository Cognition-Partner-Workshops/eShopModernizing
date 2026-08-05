using eShop.Shared.Configuration;
using eShop.Shared.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace eShop.Catalog.Api.Tests;

public class ConfigurationBindingTests
{
    private const string AppSettingsJson = """
        {
          "ConnectionStrings": { "Catalog": "Server=sql;Database=Catalog;" },
          "Catalog": {
            "UseMockData": true,
            "UseCustomizationData": true,
            "PicsFolder": "Pictures",
            "SetupFolder": "SeedData"
          }
        }
        """;

    [Fact]
    public void CatalogOptions_BindFromJson()
    {
        var provider = BuildProvider(JsonConfiguration(AppSettingsJson));

        var options = provider.GetRequiredService<IOptions<CatalogOptions>>().Value;

        Assert.True(options.UseMockData);
        Assert.True(options.UseCustomizationData);
        Assert.Equal("Pictures", options.PicsFolder);
        Assert.Equal("SeedData", options.SetupFolder);
    }

    [Fact]
    public void ConnectionString_BindsFromJson()
    {
        var provider = BuildProvider(JsonConfiguration(AppSettingsJson));

        var options = provider.GetRequiredService<IOptions<CatalogConnectionOptions>>().Value;

        Assert.Equal("Server=sql;Database=Catalog;", options.Catalog);
    }

    [Fact]
    public void MissingConnectionString_FailsValidation_WhenMockDataIsDisabled()
    {
        var provider = BuildProvider(InMemoryConfiguration(new()
        {
            ["Catalog:UseMockData"] = "false",
        }));

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<CatalogConnectionOptions>>().Value);

        Assert.Contains("ConnectionStrings__Catalog", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingConnectionString_IsAllowed_WhenMockDataIsEnabled()
    {
        var provider = BuildProvider(InMemoryConfiguration(new()
        {
            ["Catalog:UseMockData"] = "true",
        }));

        Assert.Null(provider.GetRequiredService<IOptions<CatalogConnectionOptions>>().Value.Catalog);
    }

    [Fact]
    public void BlankPicsFolder_FailsDataAnnotationsValidation()
    {
        var provider = BuildProvider(InMemoryConfiguration(new()
        {
            ["Catalog:UseMockData"] = "true",
            ["Catalog:PicsFolder"] = "",
        }));

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<CatalogOptions>>().Value);

        Assert.Contains(nameof(CatalogOptions.PicsFolder), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Defaults_MatchLegacyWebConfigValues()
    {
        var provider = BuildProvider(InMemoryConfiguration([]));

        var options = provider.GetRequiredService<IOptions<CatalogOptions>>().Value;

        Assert.False(options.UseMockData);
        Assert.False(options.UseCustomizationData);
        Assert.Equal("Pics", options.PicsFolder);
        Assert.Equal("Setup", options.SetupFolder);
    }

    private static ServiceProvider BuildProvider(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEShopConfiguration(configuration);
        return services.BuildServiceProvider();
    }

    private static IConfiguration JsonConfiguration(string json) =>
        new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
            .Build();

    private static IConfiguration InMemoryConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
