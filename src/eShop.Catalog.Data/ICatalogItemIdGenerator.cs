namespace eShop.Catalog.Data;

/// <summary>
/// Produces the identifier for a new catalog item. <c>Catalog.Id</c> is not store-generated in the
/// legacy schema, so the application has to supply it.
///
/// Implemented by <see cref="HiLoCatalogItemIdGenerator"/>, which draws blocks of ids from the
/// <c>catalog_hilo</c> sequence exactly as the legacy <c>CatalogItemHiLoGenerator</c> did.
/// </summary>
public interface ICatalogItemIdGenerator
{
    int GetNextId(CatalogDbContext db);
}
