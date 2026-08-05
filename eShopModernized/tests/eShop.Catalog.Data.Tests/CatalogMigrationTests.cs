using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace eShop.Catalog.Data.Tests;

/// <summary>
/// Script generation runs the SQL Server migration pipeline without needing a server, which is
/// the same code path as <c>dotnet ef migrations script</c>.
/// </summary>
public class CatalogMigrationTests
{
    private static CatalogDbContext CreateSqlServerContext() => SqlServerContext.Create();

    [Fact]
    public void InitialCreate_IsTheOnlyMigrationAndMatchesTheModelSnapshot()
    {
        using var context = CreateSqlServerContext();

        var migration = Assert.Single(context.Database.GetMigrations());

        Assert.EndsWith("_InitialCreate", migration, StringComparison.Ordinal);
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    public void MigrationScript_CreatesTheLegacySchema()
    {
        using var context = CreateSqlServerContext();

        var script = context.GetService<IMigrator>().GenerateScript();

        Assert.Contains("CREATE SEQUENCE [catalog_hilo] START WITH 1 INCREMENT BY 10", script, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE [Catalog] (", script, StringComparison.Ordinal);
        Assert.Contains("[Name] nvarchar(50) NOT NULL", script, StringComparison.Ordinal);
        Assert.Contains("[Price] decimal(18,2) NOT NULL", script, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE [CatalogBrand] (", script, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE [CatalogType] (", script, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE [CatalogItemsStock] (", script, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE [DiscountItems] (", script, StringComparison.Ordinal);
        Assert.Contains("FOREIGN KEY ([CatalogBrandId]) REFERENCES [CatalogBrand] ([Id])", script, StringComparison.Ordinal);
        Assert.Contains("FOREIGN KEY ([CatalogTypeId]) REFERENCES [CatalogType] ([Id])", script, StringComparison.Ordinal);
    }

    [Fact]
    public void MigrationScript_DoesNotMakeTheCatalogKeyAnIdentityColumn()
    {
        using var context = CreateSqlServerContext();

        var script = context.GetService<IMigrator>().GenerateScript();
        var catalogTable = script[script.IndexOf("CREATE TABLE [Catalog] (", StringComparison.Ordinal)..];
        catalogTable = catalogTable[..catalogTable.IndexOf(");", StringComparison.Ordinal)];

        Assert.Contains("[Id] int NOT NULL,", catalogTable, StringComparison.Ordinal);
        Assert.DoesNotContain("IDENTITY", catalogTable, StringComparison.Ordinal);
    }
}
