using eShop.Catalog.Grpc.Protos;
using eShopWcf.Catalog.Data;
using eShopWcf.Catalog.Grpc.Mapping;
using Grpc.Core;

namespace eShopWcf.Catalog.Grpc.Services;

public sealed class CatalogGrpcService(CatalogService catalog) : eShop.Catalog.Grpc.Protos.Catalog.CatalogBase
{
    public override async Task<FindCatalogItemResponse> FindCatalogItem(FindCatalogItemRequest request, ServerCallContext context)
    {
        var item = await catalog.FindCatalogItem(request.Id, context.CancellationToken).ConfigureAwait(false)
            ?? throw NotFound();
        return new FindCatalogItemResponse { Item = CatalogProtoMapper.ToProto(item) };
    }

    public override async Task<GetCatalogBrandsResponse> GetCatalogBrands(GetCatalogBrandsRequest request, ServerCallContext context)
    {
        var response = new GetCatalogBrandsResponse();
        response.Brands.AddRange((await catalog.GetCatalogBrands(context.CancellationToken).ConfigureAwait(false)).Select(CatalogProtoMapper.ToProto));
        return response;
    }

    public override async Task<GetCatalogTypesResponse> GetCatalogTypes(GetCatalogTypesRequest request, ServerCallContext context)
    {
        var response = new GetCatalogTypesResponse();
        response.Types_.AddRange((await catalog.GetCatalogTypes(context.CancellationToken).ConfigureAwait(false)).Select(CatalogProtoMapper.ToProto));
        return response;
    }

    public override async Task<GetCatalogItemsResponse> GetCatalogItems(GetCatalogItemsRequest request, ServerCallContext context)
    {
        var response = new GetCatalogItemsResponse();
        response.Items.AddRange((await catalog.GetCatalogItems(request.BrandIdFilter, request.TypeIdFilter, context.CancellationToken).ConfigureAwait(false))
            .Select(CatalogProtoMapper.ToProto));
        return response;
    }

    public override async Task<CreateCatalogItemResponse> CreateCatalogItem(CreateCatalogItemRequest request, ServerCallContext context)
    {
        var item = CatalogProtoMapper.ToDomain(request.CatalogItem ?? throw MissingPayload());
        await catalog.CreateCatalogItem(item, context.CancellationToken).ConfigureAwait(false);
        return new CreateCatalogItemResponse();
    }

    public override async Task<UpdateCatalogItemResponse> UpdateCatalogItem(UpdateCatalogItemRequest request, ServerCallContext context)
    {
        var item = CatalogProtoMapper.ToDomain(request.CatalogItem ?? throw MissingPayload());
        if (!await catalog.UpdateCatalogItem(item, context.CancellationToken).ConfigureAwait(false))
        {
            throw NotFound();
        }

        return new UpdateCatalogItemResponse();
    }

    public override async Task<RemoveCatalogItemResponse> RemoveCatalogItem(RemoveCatalogItemRequest request, ServerCallContext context)
    {
        var item = request.CatalogItem ?? throw MissingPayload();
        if (!await catalog.RemoveCatalogItem(item.Id, context.CancellationToken).ConfigureAwait(false))
        {
            throw NotFound();
        }

        return new RemoveCatalogItemResponse();
    }

    public override async Task<GetAvailableStockResponse> GetAvailableStock(GetAvailableStockRequest request, ServerCallContext context) =>
        new()
        {
            AvailableStock = await catalog.GetAvailableStock(CatalogProtoMapper.ToDateTime(request.Date), request.CatalogItemId, context.CancellationToken).ConfigureAwait(false),
        };

    public override async Task<CreateAvailableStockResponse> CreateAvailableStock(CreateAvailableStockRequest request, ServerCallContext context)
    {
        var stock = CatalogProtoMapper.ToDomain(request.CatalogItemsStock ?? throw MissingPayload());
        await catalog.CreateAvailableStock(stock, context.CancellationToken).ConfigureAwait(false);
        return new CreateAvailableStockResponse();
    }

    public override async Task<GetDiscountResponse> GetDiscount(GetDiscountRequest request, ServerCallContext context)
    {
        var discount = await catalog.GetDiscount(CatalogProtoMapper.ToDateTime(request.Day), context.CancellationToken).ConfigureAwait(false)
            ?? throw NotFound();
        return new GetDiscountResponse { Discount = CatalogProtoMapper.ToProto(discount) };
    }

    private static RpcException MissingPayload() =>
        new(new Status(StatusCode.InvalidArgument, "Request payload is required."));

    private static RpcException NotFound() =>
        new(new Status(StatusCode.NotFound, "No matching catalog row."));
}
