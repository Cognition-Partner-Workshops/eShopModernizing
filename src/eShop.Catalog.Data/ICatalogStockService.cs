using eShop.Catalog.Domain;

namespace eShop.Catalog.Data;

/// <summary>
/// The three catalog operations that only the legacy WCF service ever exposed
/// (<c>GetAvailableStock</c>, <c>CreateAvailableStock</c>, <c>GetDiscount</c>).
/// </summary>
/// <remarks>
/// They live on their own interface rather than on <see cref="ICatalogService"/> for two reasons:
/// <see cref="ICatalogService"/> is the contract the MVC/Web Forms front ends share and has no use
/// for per-day stock or discounts, and keeping the addition separate makes NET-66 additive with
/// respect to the tickets touching the data layer in parallel. Unlike <see cref="ICatalogService"/>
/// (synchronous, ported as-is in NET-64) this interface is asynchronous, which is what the gRPC
/// host wants.
/// </remarks>
public interface ICatalogStockService
{
    /// <summary>
    /// Available stock recorded for <paramref name="date"/> (date component only). Returns 0 when
    /// no row exists, exactly like the legacy <c>CatalogService.GetAvailableStock</c>.
    /// </summary>
    Task<int> GetAvailableStockAsync(DateTime date, int catalogItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a stock row, or overwrites the existing row for the same item and date. Mirrors the
    /// legacy <c>CreateAvailableStock</c>, including its "max(StockId) + 1" identifier strategy.
    /// </summary>
    Task CreateAvailableStockAsync(CatalogItemsStock catalogItemsStock, CancellationToken cancellationToken = default);

    /// <summary>
    /// First discount whose date range contains <paramref name="day"/> (date component only), or
    /// <see langword="null"/> when there is none.
    /// </summary>
    Task<DiscountItem?> GetDiscountAsync(DateTime day, CancellationToken cancellationToken = default);
}
