using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace eShop.Catalog.Api.Tests;

/// <summary>
/// Hosts the API in memory against the mock catalog data provider so the parity tests need no
/// database. <see cref="PicsFolder"/> overrides <c>Catalog:PicsFolder</c> when set.
/// </summary>
public class CatalogApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Absolute picture folder to serve <c>/items/{id}/pic</c> from; null keeps the app default.</summary>
    public string? PicsFolder { get; init; }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration(configuration =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["Catalog:UseMockData"] = "true",
            };

            if (PicsFolder is not null)
            {
                settings["Catalog:PicsFolder"] = PicsFolder;
            }

            configuration.AddInMemoryCollection(settings);
        });
    }
}
