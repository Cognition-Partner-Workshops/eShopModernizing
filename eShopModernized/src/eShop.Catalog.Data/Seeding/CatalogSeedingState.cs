namespace eShop.Catalog.Data.Seeding;

/// <summary>
/// Tracks whether the catalog database has been migrated and seeded, so readiness only reports
/// healthy once the startup initializer has finished.
/// </summary>
public sealed class CatalogSeedingState
{
    private int _initialized;

    /// <summary>True once the catalog database has been migrated and seeded.</summary>
    public bool IsInitialized => Volatile.Read(ref _initialized) == 1;

    /// <summary>Records that migration and seeding completed successfully.</summary>
    public void MarkInitialized() => Volatile.Write(ref _initialized, 1);
}
