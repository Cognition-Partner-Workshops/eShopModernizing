using eShop.Catalog.Data.Sequences;

namespace eShop.Catalog.Data;

/// <summary>
/// Port of the legacy <c>CatalogItemHiLoGenerator</c> (MVC / Web Forms), which Autofac registered
/// as a singleton: one <c>NEXT VALUE FOR catalog_hilo</c> round trip reserves a block of
/// <see cref="CatalogSequences.Increment"/> ids that are then handed out from memory.
/// </summary>
/// <remarks>
/// Blocks are never reused, so ids are strictly increasing but not necessarily contiguous: a host
/// restart (or a second host) abandons the unused tail of its block. That gap tolerance is the
/// legacy behaviour and the reason <c>Catalog.Id</c> is not store-generated.
/// </remarks>
public sealed class HiLoCatalogItemIdGenerator : ICatalogItemIdGenerator
{
    private readonly ICatalogSequenceProvider _sequences;
    private readonly object _sequenceLock = new();

    private int _sequenceId = -1;
    private int _remainingLoIds;

    public HiLoCatalogItemIdGenerator(ICatalogSequenceProvider sequences) => _sequences = sequences;

    public int GetNextId(CatalogDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);

        lock (_sequenceLock)
        {
            if (_remainingLoIds == 0)
            {
                _sequenceId = checked((int)_sequences.GetNextSequenceValue(db, CatalogSequences.CatalogItem));
                _remainingLoIds = CatalogSequences.Increment - 1;
                return _sequenceId;
            }

            _remainingLoIds--;
            return ++_sequenceId;
        }
    }
}
