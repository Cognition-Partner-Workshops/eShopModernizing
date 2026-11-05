using eShop.Catalog.Grpc.Protos;
using eShopWcf.Catalog.Data;
using eShopWcf.Catalog.Grpc.Mapping;
using Microsoft.EntityFrameworkCore;

namespace eShopWcf.Catalog.Grpc.Tests;

public sealed class CsvSeedingTests : IDisposable
{
    private readonly CatalogGrpcTestHost _host = new("Csv");

    [Fact]
    public async Task CanonicalCsvIsTrimmedAndQuotedFieldsAreParsed()
    {
        var client = _host.Client;
        Assert.Equal(7, (await client.GetCatalogBrandsAsync(new GetCatalogBrandsRequest())).Brands.Count);
        Assert.Equal(6, (await client.GetCatalogTypesAsync(new GetCatalogTypesRequest())).Types_.Count);
        Assert.Equal(13, (await client.GetCatalogItemsAsync(new GetCatalogItemsRequest())).Items.Count);
        var first = (await client.FindCatalogItemAsync(new FindCatalogItemRequest { Id = 1 })).Item;
        Assert.Equal(".NET Bot Black Hoodie, and more", first.Description);
        Assert.Equal("1.png", first.PictureFilename);
        Assert.Equal(19.5M, CatalogProtoMapper.ToDecimal(first.Price));
        var last = (await client.FindCatalogItemAsync(new FindCatalogItemRequest { Id = 13 })).Item;
        Assert.Equal("pepito", last.Name);
        Assert.Equal("De los Palotes", last.Description);
        Assert.Equal(12M, CatalogProtoMapper.ToDecimal(last.Price));
        Assert.Equal("12.png", last.PictureFilename);
        Assert.Equal("Other", last.CatalogBrand.Brand);
        Assert.Equal("Mug", last.CatalogType.Type);
        await using var db = _host.CreateContext();
        var stock = await db.CatalogItemsStocks.OrderBy(x => x.StockId).ToListAsync();
        Assert.Equal(13, stock.Count);
        Assert.Equal(Enumerable.Range(1, 13), stock.Select(x => x.StockId));
        Assert.All(stock, x => { Assert.Equal(x.StockId, x.CatalogItemId); Assert.Equal(new DateTime(2017, 9, 20), x.Date); });
        Assert.Equal(89, stock[1].AvailableStock);
        Assert.Equal(0, stock[12].AvailableStock);
        var discounts = await db.DiscountItems.OrderBy(x => x.Id).ToListAsync();
        Assert.Equal(6, discounts.Count);
        Assert.Equal((double)0.3f, discounts[0].Size);
    }

    public void Dispose() => _host.Dispose();
}

public sealed class InitializerTests(CatalogGrpcTestHost host) : IClassFixture<CatalogGrpcTestHost>
{
    [Fact]
    public async Task ExistingDatabaseIsNeitherRecreatedNorReseeded()
    {
        await using var db = host.CreateContext();
        var item = await db.CatalogItems.SingleAsync(x => x.Id == 1);
        item.Name = "existing database sentinel";
        await db.SaveChangesAsync();
        await CatalogDatabaseInitializer.InitializeAsync(db, "Csv");
        await CatalogDatabaseInitializer.InitializeAsync(db, "invalid-but-unused-for-existing-db");
        db.ChangeTracker.Clear();
        Assert.Equal("existing database sentinel", (await db.CatalogItems.SingleAsync(x => x.Id == 1)).Name);
        Assert.Equal(12, await db.CatalogItems.CountAsync());
        Assert.Equal(5, await db.CatalogBrands.CountAsync());
    }

    [Fact]
    public async Task EmptyCatalogReturnsEmptyListsAndMissingValues()
    {
        using var empty = new CatalogGrpcTestHost();
        await using var db = empty.CreateContext();
        await db.CatalogItemsStocks.ExecuteDeleteAsync();
        await db.CatalogItems.ExecuteDeleteAsync();
        await db.CatalogBrands.ExecuteDeleteAsync();
        await db.CatalogTypes.ExecuteDeleteAsync();
        await db.DiscountItems.ExecuteDeleteAsync();
        Assert.Empty((await empty.Client.GetCatalogItemsAsync(new())).Items);
        Assert.Empty((await empty.Client.GetCatalogBrandsAsync(new())).Brands);
        Assert.Empty((await empty.Client.GetCatalogTypesAsync(new())).Types_);
    }
}
