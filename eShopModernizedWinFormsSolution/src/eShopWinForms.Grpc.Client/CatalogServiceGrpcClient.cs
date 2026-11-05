using eShopWinForms.Grpc.Client.Models;
using Grpc.Core;
using Grpc.Net.Client;
using Proto = eShopModernizedWinForms.Grpc.Protos;

namespace eShopWinForms.Grpc.Client;

public sealed class CatalogServiceGrpcClient : ICatalogServiceClient, IDisposable
{
    private readonly Proto.Catalog.CatalogClient _client;
    private readonly GrpcChannel? _ownedChannel;
    private readonly TimeSpan _callTimeout;
    private DateTime Deadline => DateTime.UtcNow.Add(_callTimeout);

    public CatalogServiceGrpcClient(CatalogServiceClientOptions options)
    {
        _callTimeout = ValidateTimeout(options.CallTimeout);
        _ownedChannel = GrpcChannel.ForAddress(options.Address);
        _client = new Proto.Catalog.CatalogClient(_ownedChannel);
    }

    public CatalogServiceGrpcClient(Proto.Catalog.CatalogClient client, CatalogServiceClientOptions? options = null)
    {
        _callTimeout = ValidateTimeout((options ?? new CatalogServiceClientOptions()).CallTimeout);
        _client = client;
    }

    private static TimeSpan ValidateTimeout(TimeSpan callTimeout) => callTimeout > TimeSpan.Zero
        ? callTimeout : throw new ArgumentOutOfRangeException(nameof(callTimeout), "Call timeout must be positive.");

    public CatalogItem? FindCatalogItem(int id)
    {
        var response = NullOnNotFound(() => _client.FindCatalogItem(new Proto.FindCatalogItemRequest { Id = id }, deadline: Deadline));
        return response?.Item is null ? null : CatalogClientMapper.ToDomain(response.Item);
    }

    public IReadOnlyList<CatalogBrand> GetCatalogBrands() =>
        _client.GetCatalogBrands(new Proto.GetCatalogBrandsRequest(), deadline: Deadline).Brands.Select(CatalogClientMapper.ToDomain).ToList();

    public IReadOnlyList<CatalogType> GetCatalogTypes() =>
        _client.GetCatalogTypes(new Proto.GetCatalogTypesRequest(), deadline: Deadline).Types_.Select(CatalogClientMapper.ToDomain).ToList();

    public IReadOnlyList<CatalogItem> GetCatalogItems(int brandIdFilter, int typeIdFilter) =>
        _client.GetCatalogItems(new Proto.GetCatalogItemsRequest { BrandIdFilter = brandIdFilter, TypeIdFilter = typeIdFilter }, deadline: Deadline)
            .Items.Select(CatalogClientMapper.ToDomain).ToList();

    public int GetAvailableStock(DateTime date, int catalogItemId) =>
        _client.GetAvailableStock(new Proto.GetAvailableStockRequest
        {
            Date = CatalogClientMapper.ToTimestamp(date),
            CatalogItemId = catalogItemId
        }, deadline: Deadline).AvailableStock;

    public void CreateAvailableStock(CatalogItemsStock stock) =>
        _client.CreateAvailableStock(new Proto.CreateAvailableStockRequest { CatalogItemsStock = CatalogClientMapper.ToProto(stock) }, deadline: Deadline);

    public void CreateCatalogItem(CatalogItem item) =>
        _client.CreateCatalogItem(new Proto.CreateCatalogItemRequest { CatalogItem = CatalogClientMapper.ToProto(item) }, deadline: Deadline);

    public void UpdateCatalogItem(CatalogItem item) =>
        _client.UpdateCatalogItem(new Proto.UpdateCatalogItemRequest { CatalogItem = CatalogClientMapper.ToProto(item) }, deadline: Deadline);

    public void RemoveCatalogItem(CatalogItem item) =>
        _client.RemoveCatalogItem(new Proto.RemoveCatalogItemRequest { CatalogItem = CatalogClientMapper.ToProto(item) }, deadline: Deadline);

    public DiscountItem? GetDiscount(DateTime day)
    {
        var response = NullOnNotFound(() => _client.GetDiscount(new Proto.GetDiscountRequest { Day = CatalogClientMapper.ToTimestamp(day) }, deadline: Deadline));
        return response?.Discount is null ? null : CatalogClientMapper.ToDomain(response.Discount);
    }

    private static T? NullOnNotFound<T>(Func<T> call) where T : class
    {
        try { return call(); }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound) { return null; }
    }

    public void Dispose() => _ownedChannel?.Dispose();
}
