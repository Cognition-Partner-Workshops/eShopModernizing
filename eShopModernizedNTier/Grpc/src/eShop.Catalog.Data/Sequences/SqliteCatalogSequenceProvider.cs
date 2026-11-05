using Microsoft.EntityFrameworkCore;

namespace eShop.Catalog.Data.Sequences;

/// <summary>
/// SQLite stand-in for the SQL Server sequence objects, used by the unattended Linux test suite.
/// SQLite has no <c>CREATE SEQUENCE</c>, so the three sequences are emulated by a single table
/// whose rows are advanced inside a transaction. The observable contract is identical: strictly
/// increasing values, starting at <see cref="CatalogSequences.StartValue"/> and advancing by
/// <see cref="CatalogSequences.Increment"/>.
/// </summary>
public sealed class SqliteCatalogSequenceProvider : ICatalogSequenceProvider
{
    /// <summary>Table that stands in for the three sequence objects.</summary>
    public const string TableName = "catalog_sequence";

    private readonly object _gate = new();

    public long GetNextSequenceValue(CatalogDbContext db, string sequenceName)
    {
        ArgumentNullException.ThrowIfNull(db);

        var sequence = CatalogSequences.Validate(sequenceName);

        lock (_gate)
        {
            db.Database.ExecuteSqlRaw(
                $"CREATE TABLE IF NOT EXISTS {TableName} (name TEXT NOT NULL PRIMARY KEY, current_value INTEGER NOT NULL)");

            db.Database.ExecuteSqlRaw(
                $"INSERT OR IGNORE INTO {TableName} (name, current_value) VALUES ({{0}}, {{1}})",
                sequence,
                CatalogSequences.StartValue);

            var current = db.Database
                .SqlQueryRaw<long>($"SELECT current_value AS \"Value\" FROM {TableName} WHERE name = {{0}}", sequence)
                .AsEnumerable()
                .Single();

            db.Database.ExecuteSqlRaw(
                $"UPDATE {TableName} SET current_value = current_value + {{0}} WHERE name = {{1}}",
                CatalogSequences.Increment,
                sequence);

            return current;
        }
    }
}
