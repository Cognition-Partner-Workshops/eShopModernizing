using eShop.Catalog.Data.Seeding;
using Microsoft.EntityFrameworkCore;

namespace eShop.Catalog.Data.Tests.Seeding;

/// <summary>
/// Parity tests for the port of the legacy <c>CatalogDBInitializer</c>. The golden baseline is the
/// MVC seed set (decision-log contradiction C-04), which is also the one the runtime behaviour was
/// captured against.
/// </summary>
public class CatalogSeederTests : IDisposable
{
    private readonly SeedingTestHarness _harness = new();

    public void Dispose()
    {
        _harness.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Seeds_the_golden_baseline_row_counts()
    {
        await SeedAsync();

        using var context = _harness.CreateContext();

        Assert.Equal(5, await context.CatalogBrands.CountAsync());
        Assert.Equal(4, await context.CatalogTypes.CountAsync());
        Assert.Equal(12, await context.CatalogItems.CountAsync());
    }

    [Fact]
    public async Task Seeds_the_golden_baseline_brands_and_types()
    {
        await SeedAsync();

        using var context = _harness.CreateContext();

        Assert.Equal(
            ["Azure", ".NET", "Visual Studio", "SQL Server", "Other"],
            await context.CatalogBrands.OrderBy(b => b.Id).Select(b => b.Brand).ToListAsync());

        Assert.Equal(
            ["Mug", "T-Shirt", "Sheet", "USB Memory Stick"],
            await context.CatalogTypes.OrderBy(t => t.Id).Select(t => t.Type).ToListAsync());
    }

    [Fact]
    public async Task Seeds_the_golden_baseline_catalog_items()
    {
        await SeedAsync();

        using var context = _harness.CreateContext();
        var items = await context.CatalogItems
            .Include(i => i.CatalogBrand)
            .Include(i => i.CatalogType)
            .OrderBy(i => i.Id)
            .ToListAsync();

        Assert.Equal(Enumerable.Range(1, 12), items.Select(i => i.Id));
        Assert.Equal(
            Enumerable.Range(1, 12).Select(i => $"{i}.png"),
            items.Select(i => i.PictureFileName));

        var hoodie = items[0];
        Assert.Equal(".NET Bot Black Hoodie", hoodie.Name);
        Assert.Equal(19.5m, hoodie.Price);
        Assert.Equal(".NET", hoodie.CatalogBrand?.Brand);
        Assert.Equal("T-Shirt", hoodie.CatalogType?.Type);

        var mug = items[1];
        Assert.Equal(".NET Black & White Mug", mug.Name);
        Assert.Equal(8.50m, mug.Price);
        Assert.Equal("Mug", mug.CatalogType?.Type);

        // The WCF seed set called item 12 "Prism White T-Shirt" and pointed item 1 at 2.png; the
        // MVC set wins (C-04).
        Assert.Equal("Prism White TShirt", items[11].Name);
        Assert.Equal("Other", items[11].CatalogBrand?.Brand);
    }

    [Fact]
    public async Task Allocates_catalog_item_ids_in_blocks_of_ten_starting_at_one()
    {
        await SeedAsync();

        using var context = _harness.CreateContext();

        Assert.Equal(
            Enumerable.Range(1, 12),
            await context.CatalogItems.OrderBy(i => i.Id).Select(i => i.Id).ToListAsync());

        // 12 ids come out of two reads of catalog_hilo, not twelve.
        Assert.Equal(2, _harness.Sequence.ReadCount);
    }

    [Fact]
    public async Task Running_the_seeder_twice_does_not_duplicate_data()
    {
        await SeedAsync();
        await SeedAsync();

        using var context = _harness.CreateContext();

        Assert.Equal(5, await context.CatalogBrands.CountAsync());
        Assert.Equal(4, await context.CatalogTypes.CountAsync());
        Assert.Equal(12, await context.CatalogItems.CountAsync());
        Assert.Equal(2, _harness.Sequence.ReadCount);
    }

    [Fact]
    public async Task Customization_data_replaces_the_default_seed()
    {
        _harness.CopySetupAsset(CatalogCsvSeedReader.CatalogBrandsFileName);
        _harness.CopySetupAsset(CatalogCsvSeedReader.CatalogTypesFileName);
        _harness.CopySetupAsset(CatalogCsvSeedReader.CatalogItemsFileName);

        await SeedAsync(useCustomizationData: true);

        using var context = _harness.CreateContext();

        Assert.Equal(7, await context.CatalogBrands.CountAsync());
        Assert.Equal(6, await context.CatalogTypes.CountAsync());
        Assert.Equal(13, await context.CatalogItems.CountAsync());

        Assert.Contains("CatalogBrandTestTwo", await context.CatalogBrands.Select(b => b.Brand).ToListAsync());
        Assert.Contains("CatalogTypeTestTwo", await context.CatalogTypes.Select(t => t.Type).ToListAsync());

        var mug = await context.CatalogItems
            .Include(i => i.CatalogBrand)
            .SingleAsync(i => i.Name == ".NET Black & White Mug");
        Assert.Equal(89, mug.AvailableStock);
        Assert.True(mug.OnReorder);
        Assert.Equal(".NET", mug.CatalogBrand?.Brand);

        // Row 13 exists only in the customization file.
        var pepito = await context.CatalogItems.SingleAsync(i => i.Name == "pepito");
        Assert.Equal("De los Palotes", pepito.Description);
        Assert.Equal(12m, pepito.Price);
    }

    [Fact]
    public async Task Customization_falls_back_to_the_default_seed_when_the_files_are_missing()
    {
        await SeedAsync(useCustomizationData: true);

        using var context = _harness.CreateContext();

        Assert.Equal(5, await context.CatalogBrands.CountAsync());
        Assert.Equal(4, await context.CatalogTypes.CountAsync());
        Assert.Equal(12, await context.CatalogItems.CountAsync());
    }

    [Fact]
    public async Task Customization_rejects_a_file_whose_headers_do_not_match_the_contract()
    {
        _harness.CopySetupAsset(CatalogCsvSeedReader.CatalogTypesFileName, "NotACatalogType\nMug\n");

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => SeedAsync(useCustomizationData: true));

        Assert.Equal("does not contain required header 'catalogtype'", exception.Message);
    }

    [Fact]
    public async Task Customization_rejects_an_item_row_with_the_wrong_column_count()
    {
        _harness.CopySetupAsset(
            CatalogCsvSeedReader.CatalogItemsFileName,
            "CatalogTypeName,CatalogBrandName,Description,Name,Price,PictureFileName\nMug,.NET,a mug,a mug,1.5\n");

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => SeedAsync(useCustomizationData: true));

        Assert.Equal("column count '5' not the same as headers count'6'", exception.Message);
    }

    [Fact]
    public async Task Customization_rejects_an_item_row_referencing_an_unknown_brand()
    {
        _harness.CopySetupAsset(
            CatalogCsvSeedReader.CatalogItemsFileName,
            "CatalogTypeName,CatalogBrandName,Description,Name,Price,PictureFileName\nMug,Nope,a mug,a mug,1.5,1.png\n");

        await Assert.ThrowsAsync<InvalidDataException>(() => SeedAsync(useCustomizationData: true));
    }

    [Fact]
    public async Task Customization_extracts_the_catalog_item_pictures()
    {
        _harness.CopySetupAsset(CatalogPictureSeeder.CatalogItemPicturesFileName);

        await SeedAsync(useCustomizationData: true);

        var pictures = Directory.GetFiles(_harness.Folders.PicsFolder).Select(Path.GetFileName).ToList();

        Assert.Equal(13, pictures.Count);
        Assert.Contains("1.png", pictures);
        Assert.Contains("12.png", pictures);
        Assert.Contains("dummy.png", pictures);
    }

    [Fact]
    public async Task Pictures_are_left_alone_when_customization_is_disabled()
    {
        _harness.CopySetupAsset(CatalogPictureSeeder.CatalogItemPicturesFileName);

        await SeedAsync();

        Assert.False(Directory.Exists(_harness.Folders.PicsFolder));
    }

    private async Task SeedAsync(bool useCustomizationData = false)
    {
        using var context = _harness.CreateContext();
        await _harness.CreateSeeder(context, useCustomizationData).SeedAsync();
    }
}
