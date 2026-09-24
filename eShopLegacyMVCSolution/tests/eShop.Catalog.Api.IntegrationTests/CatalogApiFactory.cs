using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace eShop.Catalog.Api.IntegrationTests;

/// <summary>Hosts the Catalog API in-process with UseMockData=true (EF Core InMemory provider, seeded with the 12 preconfigured items).</summary>
public class CatalogApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseMockData"] = "true",
                ["UseCustomizationData"] = "false",
            });
        });
    }
}
