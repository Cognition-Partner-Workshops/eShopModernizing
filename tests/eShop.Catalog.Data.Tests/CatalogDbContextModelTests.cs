using eShop.Catalog.Data;
using eShop.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace eShop.Catalog.Data.Tests;

/// <summary>
/// Asserts that the EF Core model reproduces the legacy EF6 mapping (the shape the InitialCreate
/// migration is generated from). The SQL Server provider is used for metadata only; no connection
/// is opened.
/// </summary>
public class CatalogDbContextModelTests
{
    private readonly IModel _model = new CatalogDbContext(
        new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlServer("Server=none;Database=none;Trusted_Connection=True;")
            .Options).Model;

    [Theory]
    [InlineData(typeof(CatalogItem), "Catalog")]
    [InlineData(typeof(CatalogBrand), "CatalogBrand")]
    [InlineData(typeof(CatalogType), "CatalogType")]
    [InlineData(typeof(CatalogItemsStock), "CatalogItemsStock")]
    [InlineData(typeof(DiscountItem), "DiscountItems")]
    public void Entities_MapToTheLegacyTableNames(Type clrType, string tableName) =>
        Assert.Equal(tableName, _model.FindEntityType(clrType)!.GetTableName());

    [Fact]
    public void CatalogItemId_IsNeverStoreGenerated()
    {
        var id = _model.FindEntityType(typeof(CatalogItem))!.FindProperty(nameof(CatalogItem.Id))!;

        Assert.Equal(ValueGenerated.Never, id.ValueGenerated);
    }

    [Fact]
    public void CatalogItemName_IsRequiredWithMaxLengthFifty()
    {
        var name = _model.FindEntityType(typeof(CatalogItem))!.FindProperty(nameof(CatalogItem.Name))!;

        Assert.False(name.IsNullable);
        Assert.Equal(50, name.GetMaxLength());
    }

    [Fact]
    public void CatalogItemPrice_UsesTheLegacyDecimalShape()
    {
        var price = _model.FindEntityType(typeof(CatalogItem))!.FindProperty(nameof(CatalogItem.Price))!;

        Assert.Equal("decimal(18,2)", price.GetColumnType());
    }

    [Fact]
    public void CatalogItem_HasRequiredForeignKeysToBrandAndType()
    {
        var foreignKeys = _model.FindEntityType(typeof(CatalogItem))!.GetForeignKeys().ToList();

        Assert.Equal(2, foreignKeys.Count);
        Assert.All(foreignKeys, fk => Assert.True(fk.IsRequired));
        Assert.Contains(foreignKeys, fk => fk.PrincipalEntityType.ClrType == typeof(CatalogBrand)
            && fk.Properties.Single().Name == nameof(CatalogItem.CatalogBrandId));
        Assert.Contains(foreignKeys, fk => fk.PrincipalEntityType.ClrType == typeof(CatalogType)
            && fk.Properties.Single().Name == nameof(CatalogItem.CatalogTypeId));
    }

    [Fact]
    public void BrandAndType_AreRequiredWithMaxLengthOneHundred()
    {
        var brand = _model.FindEntityType(typeof(CatalogBrand))!.FindProperty(nameof(CatalogBrand.Brand))!;
        var type = _model.FindEntityType(typeof(CatalogType))!.FindProperty(nameof(CatalogType.Type))!;

        Assert.False(brand.IsNullable);
        Assert.Equal(100, brand.GetMaxLength());
        Assert.False(type.IsNullable);
        Assert.Equal(100, type.GetMaxLength());
    }

    [Fact]
    public void WcfOnlyEntities_KeepTheirLegacyKeyAndDateShapes()
    {
        var stock = _model.FindEntityType(typeof(CatalogItemsStock))!;
        Assert.Equal(nameof(CatalogItemsStock.StockId), stock.FindPrimaryKey()!.Properties.Single().Name);
        Assert.Equal(ValueGenerated.Never, stock.FindProperty(nameof(CatalogItemsStock.StockId))!.ValueGenerated);
        Assert.Equal("date", stock.FindProperty(nameof(CatalogItemsStock.Date))!.GetColumnType());

        var discount = _model.FindEntityType(typeof(DiscountItem))!;
        Assert.Equal("date", discount.FindProperty(nameof(DiscountItem.Start))!.GetColumnType());
        Assert.Equal("date", discount.FindProperty(nameof(DiscountItem.End))!.GetColumnType());
    }
}
