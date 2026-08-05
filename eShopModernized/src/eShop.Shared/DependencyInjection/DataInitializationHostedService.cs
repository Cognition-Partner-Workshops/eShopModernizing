using eShop.Shared.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace eShop.Shared.DependencyInjection;

/// <summary>
/// Runs every registered <see cref="IDataInitializer"/> at startup, mirroring the legacy
/// <c>ConfigDataBase()</c> hook which installed the EF6 database initializer only when mock data
/// was disabled.
/// </summary>
internal sealed class DataInitializationHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<CatalogOptions> _catalogOptions;
    private readonly ILogger<DataInitializationHostedService> _logger;

    public DataInitializationHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<CatalogOptions> catalogOptions,
        ILogger<DataInitializationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _catalogOptions = catalogOptions;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_catalogOptions.Value.UseMockData)
        {
            _logger.LogInformation("Mock data is enabled; skipping catalog database initialization.");
            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();

        foreach (var initializer in scope.ServiceProvider.GetServices<IDataInitializer>())
        {
            _logger.LogInformation(
                "Running catalog data initializer {Initializer}.", initializer.GetType().Name);
            await initializer.InitializeAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
