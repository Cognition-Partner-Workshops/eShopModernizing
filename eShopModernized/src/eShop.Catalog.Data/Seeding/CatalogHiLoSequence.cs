using Microsoft.EntityFrameworkCore;

namespace eShop.Catalog.Data.Seeding;

/// <summary>
/// Runs <c>SELECT NEXT VALUE FOR catalog_hilo</c>, the statement the legacy
/// <c>CatalogItemHiLoGenerator</c> issued through <c>Database.SqlQuery&lt;Int64&gt;</c>.
/// </summary>
internal sealed class CatalogHiLoSequence : ICatalogHiLoSequence
{
    private const string NextValueSql =
        "SELECT NEXT VALUE FOR " + CatalogDbContext.CatalogItemHiLoSequenceName + " AS Value";

    public async Task<long> GetNextValueAsync(CatalogDbContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var values = await context.Database
            .SqlQueryRaw<long>(NextValueSql)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return values.Single();
    }
}
