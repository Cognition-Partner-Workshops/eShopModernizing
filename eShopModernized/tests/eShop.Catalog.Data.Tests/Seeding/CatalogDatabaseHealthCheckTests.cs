using eShop.Catalog.Data.Seeding;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace eShop.Catalog.Data.Tests.Seeding;

/// <summary>
/// <c>/ready</c> must only report the catalog database once the startup initializer has migrated
/// and seeded it.
/// </summary>
public class CatalogDatabaseHealthCheckTests : IDisposable
{
    private readonly SeedingTestHarness _harness = new();

    public void Dispose()
    {
        _harness.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Reports_unhealthy_until_the_database_has_been_seeded()
    {
        var state = new CatalogSeedingState();
        using var context = _harness.CreateContext();
        var check = new CatalogDatabaseHealthCheck(context, state);

        var beforeSeeding = await check.CheckHealthAsync(new HealthCheckContext());
        Assert.Equal(HealthStatus.Unhealthy, beforeSeeding.Status);

        await _harness.CreateSeeder(context).SeedAsync();
        state.MarkInitialized();

        var afterSeeding = await check.CheckHealthAsync(new HealthCheckContext());
        Assert.Equal(HealthStatus.Healthy, afterSeeding.Status);
    }
}
