using eShop.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace eShop.Catalog.Data.Tests;

public class CatalogModelConfigurationTests : IClassFixture<SqliteCatalogDatabase>
{
    private readonly SqliteCatalogDatabase _database;

    public CatalogModelConfigurationTests(SqliteCatalogDatabase database)
    {
        _database = database;
    }

    [Theory]
    [InlineData(typeof(CatalogItem), "Catalog")]
    [InlineData(typeof(CatalogBrand), "CatalogBrand")]
    [InlineData(typeof(CatalogType), "CatalogType")]
    [InlineData(typeof(CatalogItemsStock), "CatalogItemsStock")]
    [InlineData(typeof(DiscountItem), "DiscountItems")]
    public void Entities_MapToLegacyTableNames(Type clrType, string tableName)
    {
        using var context = _database.CreateContext();

        Assert.Equal(tableName, context.Model.FindEntityType(clrType)!.GetTableName());
    }

    [Fact]
    public void CatalogItem_Name_IsRequiredWithMaxLength50()
    {
        using var context = _database.CreateContext();

        var name = context.Model.FindEntityType(typeof(CatalogItem))!.FindProperty(nameof(CatalogItem.Name))!;

        Assert.False(name.IsNullable);
        Assert.Equal(50, name.GetMaxLength());
    }

    [Fact]
    public void CatalogItem_PriceAndPictureFileName_AreRequired()
    {
        using var context = _database.CreateContext();

        var entity = context.Model.FindEntityType(typeof(CatalogItem))!;

        Assert.False(entity.FindProperty(nameof(CatalogItem.Price))!.IsNullable);
        Assert.False(entity.FindProperty(nameof(CatalogItem.PictureFileName))!.IsNullable);
    }

    [Fact]
    public void CatalogItem_PictureUri_IsNotMapped()
    {
        using var context = _database.CreateContext();

        Assert.Null(context.Model.FindEntityType(typeof(CatalogItem))!.FindProperty(nameof(CatalogItem.PictureUri)));
    }

    [Fact]
    public void CatalogItem_ForeignKeys_AreRequiredAndCarryNavigations()
    {
        using var context = _database.CreateContext();

        var foreignKeys = context.Model.FindEntityType(typeof(CatalogItem))!.GetForeignKeys().ToList();

        Assert.Equal(2, foreignKeys.Count);
        Assert.All(foreignKeys, fk => Assert.True(fk.IsRequired));

        var brandFk = Assert.Single(foreignKeys, fk => fk.PrincipalEntityType.ClrType == typeof(CatalogBrand));
        Assert.Equal(nameof(CatalogItem.CatalogBrandId), Assert.Single(brandFk.Properties).Name);
        Assert.Equal(nameof(CatalogItem.CatalogBrand), brandFk.DependentToPrincipal!.Name);

        var typeFk = Assert.Single(foreignKeys, fk => fk.PrincipalEntityType.ClrType == typeof(CatalogType));
        Assert.Equal(nameof(CatalogItem.CatalogTypeId), Assert.Single(typeFk.Properties).Name);
        Assert.Equal(nameof(CatalogItem.CatalogType), typeFk.DependentToPrincipal!.Name);
    }

    [Fact]
    public void CatalogItem_Id_IsNeverGeneratedByTheDatabase()
    {
        using var context = _database.CreateContext();

        var id = context.Model.FindEntityType(typeof(CatalogItem))!.FindProperty(nameof(CatalogItem.Id))!;

        Assert.Equal(ValueGenerated.Never, id.ValueGenerated);
    }

    [Theory]
    [InlineData(typeof(CatalogBrand))]
    [InlineData(typeof(CatalogType))]
    public void BrandAndTypeKeys_KeepIdentityGeneration(Type clrType)
    {
        using var context = _database.CreateContext();

        var id = context.Model.FindEntityType(clrType)!.FindProperty("Id")!;

        Assert.Equal(ValueGenerated.OnAdd, id.ValueGenerated);
    }

    [Fact]
    public void Model_DeclaresTheLegacyCatalogHiLoSequence()
    {
        using var context = SqlServerContext.Create();

        var sequence = context.Model.FindSequence(CatalogDbContext.CatalogItemHiLoSequenceName);

        Assert.NotNull(sequence);
        Assert.Equal(typeof(long), sequence.Type);
        Assert.Equal(1, sequence.StartValue);
        Assert.Equal(10, sequence.IncrementBy);
    }

    [Fact]
    public void Model_DeclaresNoHiLoSequenceForBrandsOrTypes()
    {
        using var context = SqlServerContext.Create();

        var sequenceNames = context.Model.GetSequences().Select(s => s.Name).ToList();

        Assert.DoesNotContain("catalog_brand_hilo", sequenceNames);
        Assert.DoesNotContain("catalog_type_hilo", sequenceNames);
    }
}
