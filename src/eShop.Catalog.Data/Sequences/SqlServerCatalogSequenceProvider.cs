using Microsoft.EntityFrameworkCore;

namespace eShop.Catalog.Data.Sequences;

/// <summary>
/// Port of the legacy <c>context.Database.SqlQuery&lt;Int64&gt;("SELECT NEXT VALUE FOR …")</c>
/// calls in <c>CatalogItemHiLoGenerator</c> and <c>CatalogDBInitializer</c>.
/// </summary>
public sealed class SqlServerCatalogSequenceProvider : ICatalogSequenceProvider
{
    public long GetNextSequenceValue(CatalogDbContext db, string sequenceName)
    {
        ArgumentNullException.ThrowIfNull(db);

        // A sequence name cannot be a SQL parameter, so it is restricted to the three known
        // sequences before it is concatenated into the statement.
        var sequence = CatalogSequences.Validate(sequenceName);
        var sql = "SELECT NEXT VALUE FOR [" + CatalogSequences.Schema + "].[" + sequence + "] AS \"Value\"";

        return db.Database.SqlQueryRaw<long>(sql).AsEnumerable().Single();
    }
}
