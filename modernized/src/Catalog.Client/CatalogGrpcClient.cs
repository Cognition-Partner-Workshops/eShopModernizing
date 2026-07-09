using Catalog.Client.Protos;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using GrpcCatalogService = Catalog.Client.Protos.CatalogService;

namespace Catalog.Client;

/// <summary>
/// gRPC implementation of <see cref="ICatalogClient"/>. Wraps a
/// <see cref="GrpcChannel"/> and the protobuf-generated
/// <see cref="GrpcCatalogService.CatalogServiceClient"/>, replacing the legacy WCF
/// <c>CatalogServiceClient</c>. When constructed from
/// <see cref="CatalogGrpcClientOptions"/> the channel is created and owned by this
/// instance; when handed an existing channel (e.g. one backed by an in-process
/// test host) the caller retains ownership.
/// </summary>
public sealed class CatalogGrpcClient : ICatalogClient, IDisposable
{
    private readonly GrpcChannel? _ownedChannel;
    private readonly GrpcCatalogService.CatalogServiceClient _client;

    /// <summary>
    /// Creates a client that owns a channel opened against
    /// <see cref="CatalogGrpcClientOptions.Address"/>.
    /// </summary>
    public CatalogGrpcClient(CatalogGrpcClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _ownedChannel = GrpcChannel.ForAddress(options.Address);
        _client = new GrpcCatalogService.CatalogServiceClient(_ownedChannel);
    }

    /// <summary>
    /// Creates a client over an existing channel. The channel is not disposed by
    /// this instance (used for DI-managed channels and in-process test hosts).
    /// </summary>
    public CatalogGrpcClient(GrpcChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        _client = new GrpcCatalogService.CatalogServiceClient(channel);
    }

    public CatalogItem? FindCatalogItem(int id)
    {
        var response = _client.FindCatalogItem(new FindCatalogItemRequest { Id = id });
        return response.Item;
    }

    public IReadOnlyList<CatalogBrand> GetCatalogBrands()
    {
        var response = _client.GetCatalogBrands(new Empty());
        return response.Brands;
    }

    public IReadOnlyList<CatalogItem> GetCatalogItems(int brandIdFilter, int typeIdFilter)
    {
        var response = _client.GetCatalogItems(new GetCatalogItemsRequest
        {
            BrandIdFilter = brandIdFilter,
            TypeIdFilter = typeIdFilter,
        });
        return response.Items;
    }

    public IReadOnlyList<CatalogType> GetCatalogTypes()
    {
        var response = _client.GetCatalogTypes(new Empty());
        return response.CatalogTypes;
    }

    public int GetAvailableStock(DateTime date, int catalogItemId)
    {
        var response = _client.GetAvailableStock(new GetAvailableStockRequest
        {
            Date = ToTimestamp(date),
            CatalogItemId = catalogItemId,
        });
        return response.AvailableStock;
    }

    public void CreateAvailableStock(CatalogItemsStock stock)
    {
        ArgumentNullException.ThrowIfNull(stock);
        _client.CreateAvailableStock(stock);
    }

    public void CreateCatalogItem(CatalogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _client.CreateCatalogItem(item);
    }

    public void UpdateCatalogItem(CatalogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _client.UpdateCatalogItem(item);
    }

    public void RemoveCatalogItem(CatalogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _client.RemoveCatalogItem(item);
    }

    public DiscountItem? GetDiscount(DateTime day)
    {
        var response = _client.GetDiscount(new GetDiscountRequest { Day = ToTimestamp(day) });
        return response.Discount;
    }

    private static Timestamp ToTimestamp(DateTime value) =>
        Timestamp.FromDateTime(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    public void Dispose() => _ownedChannel?.Dispose();
}
