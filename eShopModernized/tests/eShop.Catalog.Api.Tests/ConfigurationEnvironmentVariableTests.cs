using eShop.Shared.Configuration;
using eShop.Shared.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace eShop.Catalog.Api.Tests;

/// <summary>
/// Verifies the deployment override convention: every option is settable through a process
/// environment variable using '__' as the section separator, so no secret needs to be committed.
/// Mutates the process environment, hence the dedicated non-parallel collection.
/// </summary>
[Collection(nameof(EnvironmentVariableCollection))]
public class ConfigurationEnvironmentVariableTests
{
    private const string ConnectionStringVariable = "ConnectionStrings__Catalog";
    private const string UseMockDataVariable = "Catalog__UseMockData";
    private const string PicsFolderVariable = "Catalog__PicsFolder";

    [Fact]
    public void OptionsBindFromEnvironmentVariables()
    {
        var json = """
            {
              "ConnectionStrings": { "Catalog": "Server=from-json;" },
              "Catalog": { "UseMockData": true, "PicsFolder": "Pics" }
            }
            """;

        using var _ = new EnvironmentVariableScope(new Dictionary<string, string?>
        {
            [ConnectionStringVariable] = "Server=from-env;Database=Catalog;",
            [UseMockDataVariable] = "false",
            [PicsFolderVariable] = "PicsFromEnv",
        });

        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEShopConfiguration(configuration);
        using var provider = services.BuildServiceProvider();

        var catalogOptions = provider.GetRequiredService<IOptions<CatalogOptions>>().Value;
        var connectionOptions = provider.GetRequiredService<IOptions<CatalogConnectionOptions>>().Value;

        Assert.False(catalogOptions.UseMockData);
        Assert.Equal("PicsFromEnv", catalogOptions.PicsFolder);
        Assert.Equal("Server=from-env;Database=Catalog;", connectionOptions.Catalog);
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly Dictionary<string, string?> _previous = [];

        public EnvironmentVariableScope(Dictionary<string, string?> values)
        {
            foreach (var (key, value) in values)
            {
                _previous[key] = Environment.GetEnvironmentVariable(key);
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        public void Dispose()
        {
            foreach (var (key, value) in _previous)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}

[CollectionDefinition(nameof(EnvironmentVariableCollection), DisableParallelization = true)]
public class EnvironmentVariableCollection
{
}
