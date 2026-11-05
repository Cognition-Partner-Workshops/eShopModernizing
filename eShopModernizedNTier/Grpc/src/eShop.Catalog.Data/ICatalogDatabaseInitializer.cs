namespace eShop.Catalog.Data;

/// <summary>
/// Startup seam for the legacy <c>CatalogDBInitializer</c>, which the <c>Global.asax</c>
/// <c>Application_Start</c> handlers installed via <c>Database.SetInitializer</c> whenever the
/// application was not running on mock data.
/// </summary>
/// <remarks>
/// Implemented by <see cref="Seeding.CatalogDatabaseInitializer"/> (NET-65).
/// </remarks>
public interface ICatalogDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}
