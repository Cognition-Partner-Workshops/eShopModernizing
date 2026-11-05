using eShopWinForms.Grpc.Client.Models;
using Grpc.Core;
using Grpc.Net.Client;
using Proto = eShopModernizedWinForms.Grpc.Protos;

namespace eShopWinForms.Grpc.Client;

public sealed class CatalogServiceGrpcClient : ICatalogServiceClient, IDisposable
{
    private readonly Proto.Catalog.CatalogClient _client;
    private readonly GrpcChannel? _ownedChannel;

    public CatalogServiceGrpcClient(CatalogServiceClientOptions options)
    {
        _ownedChannel = GrpcChannel.ForAddress(options.Address);
        _client = new Proto.Catalog.CatalogClient(_ownedChannel);
    }

    public CatalogServiceGrpcClient(Proto.Catalog.CatalogClient client) => _client = client;

    public CatalogItem? FindCatalogItem(int id)
    {
        var response = NullOnNotFound(() => _client.FindCatalogItem(new Proto.FindCatalogItemRequest { Id = id }));
        return response?.Item is null ? null : CatalogClientMapper.ToDomain(response.Item);
    }

    public IReadOnlyList<CatalogBrand> GetCatalogBrands() =>
        _client.GetCatalogBrands(new Proto.GetCatalogBrandsRequest()).Brands.Select(CatalogClientMapper.ToDomain).ToList();

    public IReadOnlyList<CatalogType> GetCatalogTypes() =>
        _client.GetCatalogTypes(new Proto.GetCatalogTypesRequest()).Types_.Select(CatalogClientMapper.ToDomain).ToList();

    public IReadOnlyList<CatalogItem> GetCatalogItems(int brandIdFilter, int typeIdFilter) =>
        _client.GetCatalogItems(new Proto.GetCatalogItemsRequest { BrandIdFilter = brandIdFilter, TypeIdFilter = typeIdFilter })
            .Items.Select(CatalogClientMapper.ToDomain).ToList();

    public int GetAvailableStock(DateTime date, int catalogItemId) =>
        _client.GetAvailableStock(new Proto.GetAvailableStockRequest
        {
            Date = CatalogClientMapper.ToTimestamp(date),
            CatalogItemId = catalogItemId
        }).AvailableStock;

    public void CreateAvailableStock(CatalogItemsStock stock) =>
        _client.CreateAvailableStock(new Proto.CreateAvailableStockRequest { CatalogItemsStock = CatalogClientMapper.ToProto(stock) });

    public void CreateCatalogItem(CatalogItem item) =>
        _client.CreateCatalogItem(new Proto.CreateCatalogItemRequest { CatalogItem = CatalogClientMapper.ToProto(item) });

    public void UpdateCatalogItem(CatalogItem item) =>
        _client.UpdateCatalogItem(new Proto.UpdateCatalogItemRequest { CatalogItem = CatalogClientMapper.ToProto(item) });

    public void RemoveCatalogItem(CatalogItem item) =>
        _client.RemoveCatalogItem(new Proto.RemoveCatalogItemRequest { CatalogItem = CatalogClientMapper.ToProto(item) });

    public DiscountItem? GetDiscount(DateTime day)
    {
        var response = NullOnNotFound(() => _client.GetDiscount(new Proto.GetDiscountRequest { Day = CatalogClientMapper.ToTimestamp(day) }));
        return response?.Discount is null ? null : CatalogClientMapper.ToDomain(response.Discount);
    }

    private static T? NullOnNotFound<T>(Func<T> call) where T : class
    {
        try { return call(); }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound) { return null; }
    }

    public void Dispose() => _ownedChannel?.Dispose();
}
