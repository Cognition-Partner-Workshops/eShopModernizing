using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace eShop.Catalog.Data.Seeding;

/// <summary>
/// Readiness check for the consolidated catalog database: unhealthy until the startup initializer
/// has migrated and seeded it, then a connectivity probe.
/// </summary>
internal sealed class CatalogDatabaseHealthCheck : IHealthCheck
{
    /// <summary>Name the check is registered under.</summary>
    public const string Name = "catalog-db";

    private readonly CatalogDbContext _dbContext;
    private readonly CatalogSeedingState _state;

    public CatalogDatabaseHealthCheck(CatalogDbContext dbContext, CatalogSeedingState state)
    {
        _dbContext = dbContext;
        _state = state;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_state.IsInitialized)
        {
            return HealthCheckResult.Unhealthy("The catalog database has not been migrated and seeded yet.");
        }

        return await _dbContext.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false)
            ? HealthCheckResult.Healthy("The catalog database is migrated, seeded and reachable.")
            : HealthCheckResult.Unhealthy("The catalog database is not reachable.");
    }
}
