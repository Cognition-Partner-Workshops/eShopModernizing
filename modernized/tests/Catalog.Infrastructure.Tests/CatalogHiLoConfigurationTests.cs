using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Catalog.Infrastructure.Tests;

/// <summary>
/// Verifies that the catalog entities use SQL Server HiLo id generation
/// (matching the legacy EF6 sequences) when the SQL Server provider is used,
/// and that providers without sequence support fall back to a working strategy.
/// The model is built offline — no database connection is opened.
/// </summary>
public class CatalogHiLoConfigurationTests
{
    private static IModel BuildSqlServerModel()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=CatalogDb;Trusted_Connection=True;")
            .Options;

        using var context = new CatalogDbContext(options);
        return context.Model;
    }

    private static IModel BuildSqliteModel()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        using var context = new CatalogDbContext(options);
        return context.Model;
    }

    [Theory]
    [InlineData(typeof(CatalogItem), "catalog_hilo")]
    [InlineData(typeof(CatalogBrand), "catalog_brand_hilo")]
    [InlineData(typeof(CatalogType), "catalog_type_hilo")]
    public void SqlServer_Key_Uses_HiLo_With_Legacy_Sequence_Name(Type entityType, string sequenceName)
    {
        var model = BuildSqlServerModel();
        var key = model.FindEntityType(entityType)!.FindProperty("Id")!;

        Assert.Equal(SqlServerValueGenerationStrategy.SequenceHiLo, key.GetValueGenerationStrategy());
        Assert.Equal(sequenceName, key.GetHiLoSequenceName());
    }

    [Fact]
    public void SqlServer_Model_Defines_Legacy_HiLo_Sequences()
    {
        var model = BuildSqlServerModel();

        var sequenceNames = model.GetSequences().Select(s => s.Name).ToArray();

        Assert.Contains("catalog_hilo", sequenceNames);
        Assert.Contains("catalog_brand_hilo", sequenceNames);
        Assert.Contains("catalog_type_hilo", sequenceNames);
    }

    [Theory]
    // Each HiLo sequence must start past the highest explicitly-seeded id
    // (items 1-12, brands 1-5, types 1-4) so generated ids never collide with
    // the HasData seed rows (which previously caused a PRIMARY KEY violation on
    // the first insert against SQL Server).
    [InlineData("catalog_hilo", 12)]
    [InlineData("catalog_brand_hilo", 5)]
    [InlineData("catalog_type_hilo", 4)]
    public void SqlServer_HiLo_Sequences_Start_Past_Seeded_Ids(string sequenceName, int maxSeededId)
    {
        var model = BuildSqlServerModel();

        var sequence = model.GetSequences().Single(s => s.Name == sequenceName);

        Assert.True(
            sequence.StartValue > maxSeededId,
            $"Sequence '{sequenceName}' starts at {sequence.StartValue}, which is not past the max seeded id {maxSeededId}.");
    }

    [Fact]
    public void Sqlite_Key_Does_Not_Use_HiLo()
    {
        var model = BuildSqliteModel();
        var key = model.FindEntityType(typeof(CatalogItem))!.FindProperty("Id")!;

        Assert.NotEqual(SqlServerValueGenerationStrategy.SequenceHiLo, key.GetValueGenerationStrategy());
        Assert.True(key.ValueGenerated == ValueGenerated.OnAdd);
        Assert.Empty(model.GetSequences());
    }
}
