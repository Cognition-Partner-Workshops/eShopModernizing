using eShop.Catalog.Data;
using eShop.Catalog.Grpc.Mapping;
using eShop.Catalog.Grpc.Protos;
using Grpc.Core;
using DomainItem = eShop.Catalog.Domain.CatalogItem;

namespace eShop.Catalog.Grpc.Services;

/// <summary>
/// gRPC port of the legacy WCF service <c>eShopWCFService.CatalogService</c>
/// (<c>CatalogService.svc.cs</c>), implemented on top of the EF Core 8 data layer from NET-64.
/// Every operation of the SOAP contract is ported one-for-one; the SOAP fault behaviour is mapped
/// onto gRPC status codes as documented in <c>modernization/grpc-contract.md</c>.
/// </summary>
public class CatalogGrpcService : Protos.Catalog.CatalogBase
{
    private readonly ICatalogService _catalog;
    private readonly ICatalogStockService _stock;
    private readonly ILogger<CatalogGrpcService> _logger;

    public CatalogGrpcService(ICatalogService catalog, ICatalogStockService stock, ILogger<CatalogGrpcService> logger)
    {
        _catalog = catalog;
        _stock = stock;
        _logger = logger;
    }

    public override Task<FindCatalogItemResponse> FindCatalogItem(FindCatalogItemRequest request, ServerCallContext context)
    {
        RequireId(request.Id, nameof(request.Id));

        var item = _catalog.FindCatalogItem(request.Id)
            ?? throw NotFound($"Catalog item {request.Id} was not found.");

        // The legacy operation attached the brand and the type to the returned item; the EF Core
        // implementation Includes them, the in-memory one does not, so fill the gap here.
        item.CatalogBrand ??= _catalog.GetCatalogBrands().FirstOrDefault(b => b.Id == item.CatalogBrandId);
        item.CatalogType ??= _catalog.GetCatalogTypes().FirstOrDefault(t => t.Id == item.CatalogTypeId);

        return Task.FromResult(new FindCatalogItemResponse { Item = CatalogProtoMapper.ToProto(item) });
    }

    public override Task<GetCatalogBrandsResponse> GetCatalogBrands(GetCatalogBrandsRequest request, ServerCallContext context)
    {
        var response = new GetCatalogBrandsResponse();
        response.Brands.AddRange(_catalog.GetCatalogBrands().Select(CatalogProtoMapper.ToProto));

        return Task.FromResult(response);
    }

    public override Task<GetCatalogTypesResponse> GetCatalogTypes(GetCatalogTypesRequest request, ServerCallContext context)
    {
        var response = new GetCatalogTypesResponse();
        response.Types_.AddRange(_catalog.GetCatalogTypes().Select(CatalogProtoMapper.ToProto));

        return Task.FromResult(response);
    }

    public override Task<GetCatalogItemsResponse> GetCatalogItems(GetCatalogItemsRequest request, ServerCallContext context)
    {
        // 0 means "no filter", exactly as in the legacy service; a negative filter is meaningless.
        if (request.BrandIdFilter < 0 || request.TypeIdFilter < 0)
        {
            throw InvalidArgument("Filters must be zero (no filter) or a positive identifier.");
        }

        var response = new GetCatalogItemsResponse();
        response.Items.AddRange(
            _catalog.GetCatalogItems(request.BrandIdFilter, request.TypeIdFilter).Select(CatalogProtoMapper.ToProto));

        return Task.FromResult(response);
    }

    public override Task<CreateCatalogItemResponse> CreateCatalogItem(CreateCatalogItemRequest request, ServerCallContext context)
    {
        var message = request.CatalogItem ?? throw InvalidArgument("catalog_item is required.");
        if (string.IsNullOrWhiteSpace(message.Name))
        {
            throw InvalidArgument("catalog_item.name is required.");
        }

        var item = CatalogProtoMapper.ToDomain(message);

        // The identifier is assigned by the service (legacy: max(Id) + 1), never by the caller.
        _catalog.CreateCatalogItem(item);
        _logger.LogInformation("Created catalog item {CatalogItemId}", item.Id);

        return Task.FromResult(new CreateCatalogItemResponse());
    }

    public override Task<UpdateCatalogItemResponse> UpdateCatalogItem(UpdateCatalogItemRequest request, ServerCallContext context)
    {
        var message = request.CatalogItem ?? throw InvalidArgument("catalog_item is required.");
        RequireId(message.Id, "catalog_item.id");

        // The legacy implementation marked the detached entity Modified, which faulted when the row
        // did not exist. Loading it first turns that into an explicit NOT_FOUND and keeps the
        // update working through the shared (change-tracking) ICatalogService.
        var existing = _catalog.FindCatalogItem(message.Id)
            ?? throw NotFound($"Catalog item {message.Id} was not found.");

        Apply(message, existing);
        _catalog.UpdateCatalogItem(existing);

        return Task.FromResult(new UpdateCatalogItemResponse());
    }

    public override Task<RemoveCatalogItemResponse> RemoveCatalogItem(RemoveCatalogItemRequest request, ServerCallContext context)
    {
        var message = request.CatalogItem ?? throw InvalidArgument("catalog_item is required.");
        RequireId(message.Id, "catalog_item.id");

        var existing = _catalog.FindCatalogItem(message.Id)
            ?? throw NotFound($"Catalog item {message.Id} was not found.");

        _catalog.RemoveCatalogItem(existing);

        return Task.FromResult(new RemoveCatalogItemResponse());
    }

    public override async Task<GetAvailableStockResponse> GetAvailableStock(GetAvailableStockRequest request, ServerCallContext context)
    {
        RequireId(request.CatalogItemId, nameof(request.CatalogItemId));
        var date = RequireDate(request.Date, "date");

        // Legacy behaviour: no stock row for that day means zero, not a fault.
        var available = await _stock.GetAvailableStockAsync(date, request.CatalogItemId, context.CancellationToken)
            .ConfigureAwait(false);

        return new GetAvailableStockResponse { AvailableStock = available };
    }

    public override async Task<CreateAvailableStockResponse> CreateAvailableStock(CreateAvailableStockRequest request, ServerCallContext context)
    {
        var message = request.CatalogItemsStock ?? throw InvalidArgument("catalog_items_stock is required.");
        RequireId(message.CatalogItemId, "catalog_items_stock.catalog_item_id");
        RequireDate(message.Date, "catalog_items_stock.date");

        if (message.AvailableStock < 0)
        {
            throw InvalidArgument("catalog_items_stock.available_stock must not be negative.");
        }

        await _stock.CreateAvailableStockAsync(CatalogProtoMapper.ToDomain(message), context.CancellationToken)
            .ConfigureAwait(false);

        return new CreateAvailableStockResponse();
    }

    public override async Task<GetDiscountResponse> GetDiscount(GetDiscountRequest request, ServerCallContext context)
    {
        var day = RequireDate(request.Day, "day");

        var discount = await _stock.GetDiscountAsync(day, context.CancellationToken).ConfigureAwait(false)
            ?? throw NotFound($"No discount is configured for {day:yyyy-MM-dd}.");

        return new GetDiscountResponse { Discount = CatalogProtoMapper.ToProto(discount) };
    }

    private static void Apply(CatalogItem message, DomainItem target)
    {
        target.Name = message.Name;
        target.Description = message.Description;
        target.Price = CatalogProtoMapper.ToDecimal(message.Price);

        // Drop a navigation whose foreign key changed, otherwise the next read would report the
        // new identifier next to the previous brand/type.
        if (target.CatalogBrandId != message.CatalogBrandId)
        {
            target.CatalogBrandId = message.CatalogBrandId;
            target.CatalogBrand = null;
        }

        if (target.CatalogTypeId != message.CatalogTypeId)
        {
            target.CatalogTypeId = message.CatalogTypeId;
            target.CatalogType = null;
        }

        if (!string.IsNullOrEmpty(message.PictureFilename))
        {
            target.PictureFileName = message.PictureFilename;
        }
    }

    private static void RequireId(int id, string field)
    {
        if (id <= 0)
        {
            throw InvalidArgument($"{field} must be a positive identifier.");
        }
    }

    private static DateTime RequireDate(Google.Protobuf.WellKnownTypes.Timestamp? value, string field)
    {
        if (value is null)
        {
            throw InvalidArgument($"{field} is required.");
        }

        return CatalogProtoMapper.ToDateTime(value).Date;
    }

    private static RpcException InvalidArgument(string detail) =>
        new(new Status(StatusCode.InvalidArgument, detail));

    private static RpcException NotFound(string detail) =>
        new(new Status(StatusCode.NotFound, detail));
}
