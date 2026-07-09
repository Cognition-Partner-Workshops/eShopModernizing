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
