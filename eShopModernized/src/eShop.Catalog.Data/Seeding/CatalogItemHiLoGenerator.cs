namespace eShop.Catalog.Data.Seeding;

/// <summary>
/// Port of the legacy <c>eShopLegacyMVC.Models.CatalogItemHiLoGenerator</c>. One trip to
/// <c>dbo.catalog_hilo</c> hands out a block of <see cref="HiLoIncrement"/> consecutive ids, which
/// is why <see cref="Domain.Entities.CatalogItem.Id"/> is store-generated-never.
/// Registered as a singleton, so allocation is serialized across requests.
/// </summary>
public sealed class CatalogItemHiLoGenerator : IDisposable
{
    /// <summary>Size of the id block handed out per sequence read; matches the sequence increment.</summary>
    public const int HiLoIncrement = 10;

    private readonly ICatalogHiLoSequence _sequence;
    private readonly SemaphoreSlim _sequenceLock = new(1, 1);

    private int _sequenceId = -1;
    private int _remainingLoIds;

    public CatalogItemHiLoGenerator(ICatalogHiLoSequence sequence)
    {
        _sequence = sequence;
    }

    /// <summary>Allocates the next catalog item id.</summary>
    public async Task<int> GetNextSequenceValueAsync(CatalogDbContext context, CancellationToken cancellationToken = default)
    {
        await _sequenceLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_remainingLoIds == 0)
            {
                _sequenceId = (int)await _sequence.GetNextValueAsync(context, cancellationToken).ConfigureAwait(false);
                _remainingLoIds = HiLoIncrement - 1;
                return _sequenceId;
            }

            _remainingLoIds--;
            return ++_sequenceId;
        }
        finally
        {
            _sequenceLock.Release();
        }
    }

    public void Dispose() => _sequenceLock.Dispose();
}
