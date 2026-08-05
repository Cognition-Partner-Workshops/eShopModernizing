using eShop.Catalog.Data;
using eShop.Catalog.Grpc.Mapping;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using DomainCatalogService = eShop.Catalog.Domain.Abstractions.ICatalogService;
using DomainItem = eShop.Catalog.Domain.Entities.CatalogItem;
using DomainStock = eShop.Catalog.Domain.Entities.CatalogItemsStock;

namespace eShop.Catalog.Grpc.Services;

/// <summary>
/// gRPC port of the legacy WCF <c>eShopWCFService.CatalogService</c>. Each RPC follows the legacy
/// implementation operation-by-operation; the deltas are the async EF Core paths, the DI-provided
/// <see cref="CatalogDbContext" /> (the legacy service newed up an <c>EntityModel</c> per instance)
/// and the null-to-<see cref="StatusCode.NotFound" /> mapping agreed in decision D-04.
/// </summary>
public sealed class CatalogGrpcService : CatalogService.CatalogServiceBase
{
    private readonly DomainCatalogService _catalog;
    private readonly CatalogDbContext _db;

    public CatalogGrpcService(DomainCatalogService catalog, CatalogDbContext db)
    {
        _catalog = catalog;
        _db = db;
    }

    public override async Task<CatalogItem> FindCatalogItem(FindCatalogItemRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var item = await _catalog.FindCatalogItemAsync(request.Id, context.CancellationToken).ConfigureAwait(false);

        return item is null ? throw NotFound($"Catalog item {request.Id} was not found.") : item.ToProto();
    }

    public override async Task<GetCatalogBrandsResponse> GetCatalogBrands(Empty request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var brands = await _catalog.GetCatalogBrandsAsync(context.CancellationToken).ConfigureAwait(false);

        var response = new GetCatalogBrandsResponse();
        response.Brands.AddRange(brands.Select(brand => brand.ToProto()));
        return response;
    }

    public override async Task<GetCatalogTypesResponse> GetCatalogTypes(Empty request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var types = await _catalog.GetCatalogTypesAsync(context.CancellationToken).ConfigureAwait(false);

        var response = new GetCatalogTypesResponse();
        response.CatalogTypes.AddRange(types.Select(type => type.ToProto()));
        return response;
    }

    /// <summary>
    /// Legacy semantics: a filter value of 0 means "no filter", and the navigation properties are
    /// <em>not</em> populated for the list operation (only <c>FindCatalogItem</c> loads them).
    /// </summary>
    public override async Task<GetCatalogItemsResponse> GetCatalogItems(GetCatalogItemsRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var query = _db.CatalogItems.AsNoTracking();

        if (request.BrandIdFilter != 0)
        {
            query = query.Where(ci => ci.CatalogBrandId == request.BrandIdFilter);
        }

        if (request.TypeIdFilter != 0)
        {
            query = query.Where(ci => ci.CatalogTypeId == request.TypeIdFilter);
        }

        var items = await query
            .OrderBy(ci => ci.Id)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);

        var response = new GetCatalogItemsResponse();
        response.Items.AddRange(items.Select(item => item.ToProto()));
        return response;
    }

    /// <summary>Legacy semantics: a missing stock row yields 0, not an error.</summary>
    public override async Task<GetAvailableStockResponse> GetAvailableStock(GetAvailableStockRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var date = request.Date.ToDate("date");

        var stock = await FindStockAsync(request.CatalogItemId, date, context.CancellationToken).ConfigureAwait(false);

        return new GetAvailableStockResponse { AvailableStock = stock?.AvailableStock ?? 0 };
    }

    /// <summary>
    /// Legacy semantics: overwrite the stock row for that item and date if one exists, otherwise
    /// insert a new row with <c>MAX(StockId) + 1</c>.
    /// </summary>
    public override async Task<Empty> CreateAvailableStock(CatalogItemsStock request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var date = request.Date.ToDate("date");

        if (request.CatalogItemId <= 0)
        {
            throw InvalidArgument("'catalog_item_id' must be a positive catalog item id.");
        }

        var existing = await FindStockAsync(request.CatalogItemId, date, context.CancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            existing.AvailableStock = request.AvailableStock;
            _db.Update(existing);
        }
        else
        {
            var maxStockId = await _db.CatalogItemsStocks
                .MaxAsync(stock => (int?)stock.StockId, context.CancellationToken)
                .ConfigureAwait(false) ?? 0;

            _db.CatalogItemsStocks.Add(new DomainStock
            {
                StockId = maxStockId + 1,
                CatalogItemId = request.CatalogItemId,
                AvailableStock = request.AvailableStock,
                Date = date,
            });
        }

        await _db.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

        return new Empty();
    }

    /// <summary>
    /// Legacy semantics: the server allocates the id as <c>MAX(Id) + 1</c> and ignores any id sent
    /// by the caller.
    /// </summary>
    public override async Task<Empty> CreateCatalogItem(CatalogItem request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        RequireName(request);

        var maxId = await _db.CatalogItems
            .MaxAsync(item => (int?)item.Id, context.CancellationToken)
            .ConfigureAwait(false) ?? 0;

        var entity = new DomainItem { Id = maxId + 1 };
        request.CopyContractFieldsTo(entity);

        await _catalog.CreateCatalogItemAsync(entity, context.CancellationToken).ConfigureAwait(false);

        return new Empty();
    }

    /// <summary>
    /// The legacy implementation attached the detached data contract and marked it modified. Here
    /// the stored row is loaded first so that the columns outside the legacy contract
    /// (<c>AvailableStock</c>, <c>RestockThreshold</c>, <c>MaxStockThreshold</c>, <c>OnReorder</c>)
    /// survive the update.
    /// </summary>
    public override async Task<Empty> UpdateCatalogItem(CatalogItem request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        RequireName(request);

        var entity = await _db.CatalogItems
            .FirstOrDefaultAsync(item => item.Id == request.Id, context.CancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFound($"Catalog item {request.Id} was not found.");

        request.CopyContractFieldsTo(entity);

        _db.Update(entity);
        await _db.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

        return new Empty();
    }

    public override async Task<Empty> RemoveCatalogItem(CatalogItem request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var entity = await _db.CatalogItems
            .FirstOrDefaultAsync(item => item.Id == request.Id, context.CancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFound($"Catalog item {request.Id} was not found.");

        await _catalog.RemoveCatalogItemAsync(entity, context.CancellationToken).ConfigureAwait(false);

        return new Empty();
    }

    /// <summary>
    /// Legacy semantics: the first discount whose inclusive date range covers the requested day.
    /// The legacy null response becomes <see cref="StatusCode.NotFound" /> (D-04).
    /// </summary>
    public override async Task<DiscountItem> GetDiscount(GetDiscountRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var day = request.Day.ToDate("day");

        var discount = await _db.DiscountItems
            .AsNoTracking()
            .Where(item => item.Start <= day && item.End >= day)
            .OrderBy(item => item.Id)
            .FirstOrDefaultAsync(context.CancellationToken)
            .ConfigureAwait(false);

        return discount is null
            ? throw NotFound($"No discount is running on {day:yyyy-MM-dd}.")
            : discount.ToProto();
    }

    private Task<DomainStock?> FindStockAsync(int catalogItemId, DateTime date, CancellationToken cancellationToken)
        => _db.CatalogItemsStocks
            .Where(stock => stock.CatalogItemId == catalogItemId && stock.Date == date)
            .OrderBy(stock => stock.StockId)
            .FirstOrDefaultAsync(cancellationToken);

    private static void RequireName(CatalogItem request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw InvalidArgument("'name' is required.");
        }
    }

    private static RpcException NotFound(string detail) => new(new Status(StatusCode.NotFound, detail));

    private static RpcException InvalidArgument(string detail) => new(new Status(StatusCode.InvalidArgument, detail));
}
