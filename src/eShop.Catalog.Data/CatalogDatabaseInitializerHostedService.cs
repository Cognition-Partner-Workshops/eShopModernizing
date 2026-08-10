using eShop.Catalog.Data.Seeding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace eShop.Catalog.Data;

/// <summary>
/// Runs <see cref="ICatalogDatabaseInitializer"/> once at startup, replacing the
/// <c>ConfigDataBase()</c> call the legacy <c>Application_Start</c> handlers made. It is only
/// registered when the host is not running on mock data.
/// </summary>
public sealed class CatalogDatabaseInitializerHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly IOptions<CatalogSeedOptions> _options;

    public CatalogDatabaseInitializerHostedService(IServiceProvider services, IOptions<CatalogSeedOptions> options)
    {
        _services = services;
        _options = options;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Value.InitializeDatabaseOnStartup)
        {
            return;
        }

        using var scope = _services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<ICatalogDatabaseInitializer>();
        await initializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
