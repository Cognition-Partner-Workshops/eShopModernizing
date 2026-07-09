using Catalog.Shared.Configuration;
using Catalog.Shared.DependencyInjection;
using Catalog.Shared.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Catalog.Shared.Tests;

public class CatalogSharedTests
{
    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void AddCatalogShared_BindsCatalogSettingsFromConfiguration()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Catalog:UseMockData"] = "true",
            ["Catalog:UseCustomizationData"] = "true",
        });

        using var provider = new ServiceCollection()
            .AddCatalogShared(configuration)
            .BuildServiceProvider();

        var settings = provider.GetRequiredService<IOptions<CatalogSettings>>().Value;

        Assert.True(settings.UseMockData);
        Assert.True(settings.UseCustomizationData);
    }

    [Fact]
    public void AddCatalogShared_DefaultsAreFalse_WhenNotConfigured()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>());

        using var provider = new ServiceCollection()
            .AddCatalogShared(configuration)
            .BuildServiceProvider();

        var settings = provider.GetRequiredService<IOptions<CatalogSettings>>().Value;

        Assert.False(settings.UseMockData);
        Assert.False(settings.UseCustomizationData);
    }

    [Fact]
    public void AddCatalogShared_RegistersLogging()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>());

        using var provider = new ServiceCollection()
            .AddCatalogShared(configuration)
            .BuildServiceProvider();

        var loggerFactory = provider.GetService<ILoggerFactory>();
        Assert.NotNull(loggerFactory);

        var logger = provider.GetRequiredService<ILogger<CatalogSharedTests>>();
        Assert.NotNull(logger);
    }

    [Fact]
    public void CreateSerilogLogger_ReturnsLogger()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Serilog:MinimumLevel:Default"] = "Debug",
        });

        var logger = CatalogLoggingExtensions.CreateSerilogLogger(configuration);

        Assert.NotNull(logger);
    }

    [Fact]
    public void ConnectionString_IsReadFromConfiguration()
    {
        const string expected = "Server=(localdb)\\MSSQLLocalDB;Initial Catalog=TestDb;";
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{CatalogSharedServiceCollectionExtensions.CatalogDbConnectionName}"] = expected,
        });

        var actual = configuration.GetConnectionString(
            CatalogSharedServiceCollectionExtensions.CatalogDbConnectionName);

        Assert.Equal(expected, actual);
    }
}
