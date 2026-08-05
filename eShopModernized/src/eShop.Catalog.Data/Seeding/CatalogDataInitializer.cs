using eShop.Shared.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eShop.Catalog.Data.Seeding;

/// <summary>
/// Replacement for the legacy <c>Database.SetInitializer(new CatalogDBInitializer(...))</c> call:
/// applies the EF Core migrations and then seeds the catalog. Runs through NET-61's
/// <see cref="IDataInitializer"/> seam, i.e. once at startup and only when mock data is disabled.
/// </summary>
internal sealed class CatalogDataInitializer : IDataInitializer
{
    private readonly CatalogDbContext _context;
    private readonly CatalogSeeder _seeder;
    private readonly CatalogSeedingState _state;
    private readonly ILogger<CatalogDataInitializer> _logger;

    public CatalogDataInitializer(
        CatalogDbContext context,
        CatalogSeeder seeder,
        CatalogSeedingState state,
        ILogger<CatalogDataInitializer> logger)
    {
        _context = context;
        _seeder = seeder;
        _state = state;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Applying catalog database migrations.");
        await _context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Seeding the catalog database.");
        await _seeder.SeedAsync(cancellationToken).ConfigureAwait(false);

        _state.MarkInitialized();
        _logger.LogInformation("Catalog database is migrated and seeded.");
    }
}
