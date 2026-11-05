using System.IO.Compression;
using eShop.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace eShop.Catalog.Data.Seeding;

/// <summary>
/// EF Core port of the legacy <c>CatalogDBInitializer</c> (<c>CreateDatabaseIfNotExists</c>): it
/// brings the schema up to date and seeds the catalog with the same data, in the same order.
/// </summary>
/// <remarks>
/// Catalog item ids come from the <c>catalog_hilo</c> sequence through
/// <see cref="ICatalogItemIdGenerator"/>, because <c>Catalog.Id</c> is not store-generated. Types
/// and brands are identity columns, so their ids come from the store: the legacy initializer did
/// read <c>catalog_type_hilo</c> / <c>catalog_brand_hilo</c> and assign the values, but EF6 dropped
/// them on the way to an identity column, and SQL Server rejects them outright. The seeded ids are
/// the same either way (1..n on a fresh database).
///
/// Unlike the legacy initializer — which only ran when it had just created the database — this one
/// is idempotent and safe to run on every start: each table is seeded only when it is empty, so
/// restarts and multiple hosts converge on the same data set instead of duplicating it.
/// </remarks>
public sealed class CatalogDatabaseInitializer : ICatalogDatabaseInitializer
{
    private readonly CatalogDbContext _db;
    private readonly ICatalogItemIdGenerator _idGenerator;
    private readonly CatalogSeedOptions _options;
    private readonly ILogger<CatalogDatabaseInitializer> _logger;

    public CatalogDatabaseInitializer(
        CatalogDbContext db,
        ICatalogItemIdGenerator idGenerator,
        IOptions<CatalogSeedOptions> options,
        ILogger<CatalogDatabaseInitializer> logger)
    {
        _db = db;
        _idGenerator = idGenerator;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await MigrateAsync(cancellationToken).ConfigureAwait(false);
        await SeedAsync(cancellationToken).ConfigureAwait(false);
        ExtractCatalogItemPictures();
    }

    /// <summary>
    /// Seeds the catalog types, brands and items. Public so tests (and tooling) can seed a database
    /// that was created by something other than the migrations.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedCatalogTypesAsync(cancellationToken).ConfigureAwait(false);
        await SeedCatalogBrandsAsync(cancellationToken).ConfigureAwait(false);
        await SeedCatalogItemsAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if (_db.Database.IsSqlServer())
        {
            await _db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        // Providers without the SQL Server migrations (the SQLite test databases) get the schema
        // straight from the model.
        await _db.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedCatalogTypesAsync(CancellationToken cancellationToken)
    {
        if (await _db.CatalogTypes.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var types = _options.UseCustomizationData
            ? CatalogCsvSeedData.GetCatalogTypes(_options.ResolveSetupDirectory())
            : PreconfiguredData.GetPreconfiguredCatalogTypes();

        foreach (var type in types)
        {
            // Identity column: let the store number the rows (see the remarks above).
            type.Id = 0;
            _db.CatalogTypes.Add(type);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedCatalogBrandsAsync(CancellationToken cancellationToken)
    {
        if (await _db.CatalogBrands.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var brands = _options.UseCustomizationData
            ? CatalogCsvSeedData.GetCatalogBrands(_options.ResolveSetupDirectory())
            : PreconfiguredData.GetPreconfiguredCatalogBrands();

        foreach (var brand in brands)
        {
            // Identity column: let the store number the rows (see the remarks above).
            brand.Id = 0;
            _db.CatalogBrands.Add(brand);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedCatalogItemsAsync(CancellationToken cancellationToken)
    {
        if (await _db.CatalogItems.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var items = _options.UseCustomizationData
            ? CatalogCsvSeedData.GetCatalogItems(
                _options.ResolveSetupDirectory(),
                await _db.CatalogTypes.ToDictionaryAsync(t => t.Type, t => t.Id, cancellationToken).ConfigureAwait(false),
                await _db.CatalogBrands.ToDictionaryAsync(b => b.Brand, b => b.Id, cancellationToken).ConfigureAwait(false))
            : PreconfiguredData.GetPreconfiguredCatalogItems();

        foreach (var item in items)
        {
            item.Id = _idGenerator.GetNextId(_db);
            _db.CatalogItems.Add(item);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Port of <c>AddCatalogItemPictures</c>: replace the contents of the pictures directory with
    /// the images shipped in <c>Setup/CatalogItems.zip</c>. Only the customization data set ships
    /// pictures, so the archive is optional and a missing one is logged and skipped rather than
    /// failing start-up.
    /// </summary>
    private void ExtractCatalogItemPictures()
    {
        if (!_options.UseCustomizationData)
        {
            return;
        }

        var archive = Path.Combine(_options.ResolveSetupDirectory(), CatalogCsvSeedData.CatalogItemPicturesFileName);
        if (!File.Exists(archive))
        {
            _logger.LogWarning(
                "Catalog item picture archive {Archive} not found; catalog item pictures were not extracted.",
                archive);
            return;
        }

        var pictures = Directory.CreateDirectory(_options.ResolvePicturesDirectory());
        foreach (var file in pictures.GetFiles())
        {
            file.Delete();
        }

        ZipFile.ExtractToDirectory(archive, pictures.FullName, overwriteFiles: true);
    }
}
