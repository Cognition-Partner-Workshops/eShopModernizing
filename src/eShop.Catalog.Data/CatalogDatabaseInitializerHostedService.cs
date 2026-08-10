using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace eShop.Catalog.Data;

/// <summary>
/// Runs <see cref="ICatalogDatabaseInitializer"/> once at startup, replacing the
/// <c>ConfigDataBase()</c> call the legacy <c>Application_Start</c> handlers made. It is only
/// registered when the host is not running on mock data.
/// </summary>
public sealed class CatalogDatabaseInitializerHostedService : IHostedService
{
    private readonly IServiceProvider _services;

    public CatalogDatabaseInitializerHostedService(IServiceProvider services) => _services = services;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<ICatalogDatabaseInitializer>();
        await initializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
