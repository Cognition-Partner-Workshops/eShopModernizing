using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace eShop.Catalog.Data.Tests.Seeding;

/// <summary>
/// Skips a test when no container runtime is reachable, so the suite still runs on agents without
/// Docker (the SQLite tests cover the same behaviour there).
/// </summary>
public sealed class RequiresDockerFactAttribute : FactAttribute
{
    public RequiresDockerFactAttribute()
    {
        if (!SqlServerSeedingFixture.IsDockerAvailable)
        {
            Skip = "No container runtime available; SQL Server integration tests are skipped.";
        }
    }
}

/// <summary>
/// Starts a throwaway SQL Server so the migrations, <c>dbo.catalog_hilo</c> and the IDENTITY
/// columns are exercised against the real provider rather than SQLite.
/// </summary>
public sealed class SqlServerSeedingFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;

    /// <summary>True when a container runtime is reachable.</summary>
    public static bool IsDockerAvailable { get; } =
        File.Exists("/var/run/docker.sock") ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOCKER_HOST"));

    public async Task InitializeAsync()
    {
        if (!IsDockerAvailable)
        {
            return;
        }

        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

        await _container.StartAsync();
    }

    /// <summary>Connection string for a database name unique to the calling test.</summary>
    public string ConnectionStringFor(string databaseName)
    {
        ArgumentNullException.ThrowIfNull(_container);

        return new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = databaseName,
        }.ConnectionString;
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
