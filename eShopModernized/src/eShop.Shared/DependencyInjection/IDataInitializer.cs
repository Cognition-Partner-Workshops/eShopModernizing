namespace eShop.Shared.DependencyInjection;

/// <summary>
/// Startup hook replacing the legacy <c>Database.SetInitializer&lt;CatalogDBContext&gt;(...)</c>
/// call made from <c>Global.asax</c> <c>Application_Start</c>. Implementations are resolved from a
/// scoped service provider and run once, before the host starts serving requests, and only when
/// <see cref="Configuration.CatalogOptions.UseMockData"/> is <see langword="false"/>.
/// </summary>
public interface IDataInitializer
{
    /// <summary>Creates and/or seeds the catalog database.</summary>
    Task InitializeAsync(CancellationToken cancellationToken);
}
