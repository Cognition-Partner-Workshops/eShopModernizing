namespace eShop.Catalog.Data;

/// <summary>
/// Startup seam for the legacy <c>CatalogDBInitializer</c>, which the <c>Global.asax</c>
/// <c>Application_Start</c> handlers installed via <c>Database.SetInitializer</c> whenever the
/// application was not running on mock data.
/// </summary>
public interface ICatalogDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Placeholder implementation so hosts boot with <c>UseMockData=false</c> before the EF Core
/// migration lands.
/// </summary>
// TODO(NET-64): replace with the EF Core initializer that creates and seeds the catalog database
// (honouring Catalog:UseCustomizationData, as the legacy initializer did).
public sealed class NoOpCatalogDatabaseInitializer : ICatalogDatabaseInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
