using eShop.Catalog.Domain;
using Grpc.Core;
using Proto = eShop.Catalog.Grpc.Protos;

namespace eShop.Catalog.Grpc.Client;

/// <summary>
/// <see cref="ICatalogServiceClient"/> over the generated gRPC stub. This is the service layer the
/// WinForms presenter talks to; it is the replacement for the generated WCF proxy
/// (<c>Connected Services\eShopServiceReference</c>) that the legacy client used.
/// </summary>
public sealed class CatalogServiceGrpcClient : ICatalogServiceClient
{
    private readonly Proto.Catalog.CatalogClient _client;

    public CatalogServiceGrpcClient(Proto.Catalog.CatalogClient client) => _client = client;

    public async Task<CatalogItem?> FindCatalogItemAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await NullOnNotFoundAsync(
            () => _client.FindCatalogItemAsync(new Proto.FindCatalogItemRequest { Id = id }, cancellationToken: cancellationToken).ResponseAsync);

        return response?.Item is null ? null : CatalogClientMapper.ToDomain(response.Item);
    }

    public async Task<IReadOnlyList<CatalogBrand>> GetCatalogBrandsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _client
            .GetCatalogBrandsAsync(new Proto.GetCatalogBrandsRequest(), cancellationToken: cancellationToken);

        return response.Brands.Select(CatalogClientMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<CatalogType>> GetCatalogTypesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _client
            .GetCatalogTypesAsync(new Proto.GetCatalogTypesRequest(), cancellationToken: cancellationToken);

        return response.Types_.Select(CatalogClientMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<CatalogItem>> GetCatalogItemsAsync(
        int brandIdFilter,
        int typeIdFilter,
        CancellationToken cancellationToken = default)
    {
        var request = new Proto.GetCatalogItemsRequest
        {
            BrandIdFilter = brandIdFilter,
            TypeIdFilter = typeIdFilter,
        };

        var response = await _client.GetCatalogItemsAsync(request, cancellationToken: cancellationToken);

        return response.Items.Select(CatalogClientMapper.ToDomain).ToList();
    }

    public async Task<int> GetAvailableStockAsync(
        DateTime date,
        int catalogItemId,
        CancellationToken cancellationToken = default)
    {
        var request = new Proto.GetAvailableStockRequest
        {
            Date = CatalogClientMapper.ToTimestamp(date),
            CatalogItemId = catalogItemId,
        };

        var response = await _client.GetAvailableStockAsync(request, cancellationToken: cancellationToken);

        return response.AvailableStock;
    }

    public async Task CreateAvailableStockAsync(CatalogItemsStock stock, CancellationToken cancellationToken = default)
    {
        var request = new Proto.CreateAvailableStockRequest
        {
            CatalogItemsStock = CatalogClientMapper.ToProto(stock),
        };

        await _client.CreateAvailableStockAsync(request, cancellationToken: cancellationToken);
    }

    public async Task CreateCatalogItemAsync(CatalogItem item, CancellationToken cancellationToken = default)
    {
        var request = new Proto.CreateCatalogItemRequest { CatalogItem = CatalogClientMapper.ToProto(item) };

        await _client.CreateCatalogItemAsync(request, cancellationToken: cancellationToken);
    }

    public async Task UpdateCatalogItemAsync(CatalogItem item, CancellationToken cancellationToken = default)
    {
        var request = new Proto.UpdateCatalogItemRequest { CatalogItem = CatalogClientMapper.ToProto(item) };

        await _client.UpdateCatalogItemAsync(request, cancellationToken: cancellationToken);
    }

    public async Task RemoveCatalogItemAsync(CatalogItem item, CancellationToken cancellationToken = default)
    {
        var request = new Proto.RemoveCatalogItemRequest { CatalogItem = CatalogClientMapper.ToProto(item) };

        await _client.RemoveCatalogItemAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<DiscountItem?> GetDiscountAsync(DateTime day, CancellationToken cancellationToken = default)
    {
        var request = new Proto.GetDiscountRequest { Day = CatalogClientMapper.ToTimestamp(day) };

        var response = await NullOnNotFoundAsync(
            () => _client.GetDiscountAsync(request, cancellationToken: cancellationToken).ResponseAsync);

        return response?.Discount is null ? null : CatalogClientMapper.ToDomain(response.Discount);
    }

    private static async Task<TResponse?> NullOnNotFoundAsync<TResponse>(Func<Task<TResponse>> call)
        where TResponse : class
    {
        try
        {
            return await call();
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }
}
