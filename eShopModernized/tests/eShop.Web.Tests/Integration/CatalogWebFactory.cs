using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace eShop.Web.Tests.Integration;

/// <summary>
/// Hosts the catalog UI in memory against the mock catalog data provider, which is the mode the
/// golden runtime baseline was captured in. The mock service is a singleton, so tests that write
/// take their own factory instance.
/// </summary>
public class CatalogWebFactory : WebApplicationFactory<Program>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?> { ["Catalog:UseMockData"] = "true" }));
    }

    /// <summary>Creates a client that does not follow the redirects the write actions return.</summary>
    public HttpClient CreateNonRedirectingClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
}
