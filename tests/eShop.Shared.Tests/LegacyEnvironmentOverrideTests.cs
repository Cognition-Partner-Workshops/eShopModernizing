using eShop.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace eShop.Shared.Tests;

public class LegacyEnvironmentOverrideTests
{
    [Fact]
    public void LegacyConnectionStringVariable_IsMappedToCatalogConnectionString()
    {
        using var environment = new ScopedEnvironmentVariables(
            ("ConnectionStrings__Catalog", null),
            (EShopConfigurationExtensions.LegacyConnectionStringVariable, "Server=legacy;Database=Catalog;"));

        var builder = Host.CreateApplicationBuilder();
        builder.AddEShopConfiguration();

        Assert.Equal(
            "Server=legacy;Database=Catalog;",
            builder.Configuration.GetConnectionString("Catalog"));
    }

    [Fact]
    public void StandardVariable_WinsOverLegacyVariable()
    {
        using var environment = new ScopedEnvironmentVariables(
            ("ConnectionStrings__Catalog", "Server=standard;Database=Catalog;"),
            (EShopConfigurationExtensions.LegacyConnectionStringVariable, "Server=legacy;Database=Catalog;"));

        var builder = Host.CreateApplicationBuilder();
        builder.AddEShopConfiguration();

        Assert.Equal(
            "Server=standard;Database=Catalog;",
            builder.Configuration.GetConnectionString("Catalog"));
    }
}
