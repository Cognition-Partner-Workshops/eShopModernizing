namespace eShop.Catalog.Data;

/// <summary>
/// Produces the identifier for a new catalog item. <c>Catalog.Id</c> is not store-generated in the
/// legacy schema, so the application has to supply it.
///
/// SEAM (NET-65): the legacy MVC/Web Forms apps get this value from the <c>catalog_hilo</c> SQL
/// sequence via <c>CatalogItemHiLoGenerator</c>. That sequence (and the seeding that depends on it)
/// is NET-65's scope; until then <see cref="MaxCatalogItemIdGenerator"/> reproduces the WCF
/// service's simpler "max + 1" behaviour.
/// </summary>
public interface ICatalogItemIdGenerator
{
    int GetNextId(CatalogDbContext db);
}
