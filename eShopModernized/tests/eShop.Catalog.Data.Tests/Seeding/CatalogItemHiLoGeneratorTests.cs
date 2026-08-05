using eShop.Catalog.Data.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace eShop.Catalog.Data.Tests.Seeding;

/// <summary>
/// Covers the HiLo id allocation ported from <c>eShopLegacyMVC.Models.CatalogItemHiLoGenerator</c>
/// and the schema contract behind it (decision-log contradictions C-05 and C-06).
/// </summary>
public class CatalogItemHiLoGeneratorTests
{
    [Fact]
    public async Task Allocates_ten_ids_per_sequence_read()
    {
        var sequence = new FakeCatalogHiLoSequence();
        using var generator = new CatalogItemHiLoGenerator(sequence);
        using var database = new SeedingTestHarness();
        using var context = database.CreateContext();

        var ids = new List<int>();
        for (var i = 0; i < 25; i++)
        {
            ids.Add(await generator.GetNextSequenceValueAsync(context));
        }

        Assert.Equal(Enumerable.Range(1, 25), ids);
        Assert.Equal(3, sequence.ReadCount);
    }

    [Fact]
    public void Sequence_increment_matches_the_hi_lo_block_size()
    {
        // The SQLite test model has no sequences, so assert against the SQL Server model instead.
        using var context = SqlServerContext.Create();
        var catalogHiLo = Assert.Single(context.Model.GetSequences().ToList());

        Assert.Equal(CatalogDbContext.CatalogItemHiLoSequenceName, catalogHiLo.Name);
        Assert.Equal(1, catalogHiLo.StartValue);
        Assert.Equal(CatalogItemHiLoGenerator.HiLoIncrement, catalogHiLo.IncrementBy);
    }

    [Fact]
    public void Brand_and_type_hi_lo_sequences_are_not_created()
    {
        using var context = SqlServerContext.Create();
        var script = context.Database.GenerateCreateScript();

        Assert.Contains("CREATE SEQUENCE [catalog_hilo] START WITH 1 INCREMENT BY 10", script, StringComparison.Ordinal);
        Assert.DoesNotContain("catalog_brand_hilo", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("catalog_type_hilo", script, StringComparison.OrdinalIgnoreCase);

        // The legacy scripts opened with USE [Microsoft.eShopOnContainers.Services.CatalogDb]; the
        // modernized estate has a single database chosen by the connection string (C-06).
        Assert.DoesNotContain("USE [", script, StringComparison.Ordinal);
        Assert.DoesNotContain("eShopDatabase", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Catalog_items_are_store_generated_never_while_brands_and_types_stay_identity()
    {
        using var context = SqlServerContext.Create();
        var model = context.Model;

        Assert.Equal(
            ValueGenerated.Never,
            model.FindEntityType(typeof(Domain.Entities.CatalogItem))!.FindProperty("Id")!.ValueGenerated);

        foreach (var identityType in new[] { typeof(Domain.Entities.CatalogBrand), typeof(Domain.Entities.CatalogType) })
        {
            Assert.Equal(
                ValueGenerated.OnAdd,
                model.FindEntityType(identityType)!.FindProperty("Id")!.ValueGenerated);
        }
    }
}
