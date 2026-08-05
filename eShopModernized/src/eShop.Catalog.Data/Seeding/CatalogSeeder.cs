using eShop.Catalog.Data.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace eShop.Catalog.Data.Seeding;

/// <summary>
/// Port of the legacy <c>CatalogDBInitializer.Seed</c>: catalog types, then brands, then items,
/// then the item pictures. The two legacy databases are consolidated here — the MVC/Web Forms seed
/// set is the canonical one (the WCF set pointed item 1 at <c>2.png</c> and named item 12
/// differently; see decision-log contradiction C-04).
/// </summary>
/// <remarks>
/// Unlike <c>CreateDatabaseIfNotExists</c>, which only ran on a freshly created database, this
/// seeder is idempotent: each table is populated only when it is empty, so a container restart
/// against an existing database is a no-op.
/// </remarks>
public sealed class CatalogSeeder
{
    private readonly CatalogDbContext _context;
    private readonly CatalogItemHiLoGenerator _idGenerator;
    private readonly CatalogSeedFolders _folders;
    private readonly bool _useCustomizationData;
    private readonly ILogger<CatalogSeeder> _logger;

    public CatalogSeeder(
        CatalogDbContext context,
        CatalogItemHiLoGenerator idGenerator,
        CatalogSeedFolders folders,
        bool useCustomizationData,
        ILogger<CatalogSeeder>? logger = null)
    {
        _context = context;
        _idGenerator = idGenerator;
        _folders = folders;
        _useCustomizationData = useCustomizationData;
        _logger = logger ?? NullLogger<CatalogSeeder>.Instance;
    }

    /// <summary>Seeds the catalog tables and the pictures folder if they are not populated yet.</summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await AddCatalogTypesAsync(cancellationToken).ConfigureAwait(false);
        await AddCatalogBrandsAsync(cancellationToken).ConfigureAwait(false);
        await AddCatalogItemsAsync(cancellationToken).ConfigureAwait(false);

        AddCatalogItemPictures();
    }

    private async Task AddCatalogTypesAsync(CancellationToken cancellationToken)
    {
        if (await _context.CatalogTypes.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation("Catalog types are already seeded; skipping.");
            return;
        }

        var preconfiguredTypes = _useCustomizationData
            ? CatalogCsvSeedReader.GetCatalogTypes(_folders.SetupFolder)
            : PreconfiguredData.GetPreconfiguredCatalogTypes();

        foreach (var type in preconfiguredTypes)
        {
            // CatalogType.Id is IDENTITY (legacy EF6 convention), so the ids the legacy
            // initializer read from catalog_type_hilo were discarded on insert (C-05).
            type.Id = 0;
            _context.CatalogTypes.Add(type);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task AddCatalogBrandsAsync(CancellationToken cancellationToken)
    {
        if (await _context.CatalogBrands.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation("Catalog brands are already seeded; skipping.");
            return;
        }

        var preconfiguredBrands = _useCustomizationData
            ? CatalogCsvSeedReader.GetCatalogBrands(_folders.SetupFolder)
            : PreconfiguredData.GetPreconfiguredCatalogBrands();

        foreach (var brand in preconfiguredBrands)
        {
            brand.Id = 0;
            _context.CatalogBrands.Add(brand);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task AddCatalogItemsAsync(CancellationToken cancellationToken)
    {
        if (await _context.CatalogItems.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation("Catalog items are already seeded; skipping.");
            return;
        }

        var preconfiguredItems = _useCustomizationData
            ? CatalogCsvSeedReader.GetCatalogItems(
                _folders.SetupFolder,
                await _context.CatalogTypes
                    .ToDictionaryAsync(t => t.Type, t => t.Id, StringComparer.Ordinal, cancellationToken)
                    .ConfigureAwait(false),
                await _context.CatalogBrands
                    .ToDictionaryAsync(b => b.Brand, b => b.Id, StringComparer.Ordinal, cancellationToken)
                    .ConfigureAwait(false))
            : PreconfiguredData.GetPreconfiguredCatalogItems();

        foreach (var item in preconfiguredItems)
        {
            item.Id = await _idGenerator.GetNextSequenceValueAsync(_context, cancellationToken).ConfigureAwait(false);
            _context.CatalogItems.Add(item);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private void AddCatalogItemPictures()
    {
        if (!_useCustomizationData)
        {
            return;
        }

        CatalogPictureSeeder.ExtractCatalogItemPictures(_folders);
    }
}
