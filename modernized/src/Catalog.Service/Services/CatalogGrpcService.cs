using Catalog.Infrastructure;
using Catalog.Service.Protos;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Domain = Catalog.Domain;

namespace Catalog.Service.Services;

/// <summary>
/// gRPC implementation of the catalog service. Ports the legacy WCF
/// <c>eShopWCFService.CatalogService</c> business logic (method-by-method) onto
/// EF Core 8 (<see cref="CatalogDbContext"/>), replacing SOAP/WCF transport.
/// </summary>
public class CatalogGrpcService : Protos.CatalogService.CatalogServiceBase
{
    private readonly CatalogDbContext _context;

    public CatalogGrpcService(CatalogDbContext context)
    {
        _context = context;
    }

    public override async Task<FindCatalogItemResponse> FindCatalogItem(
        FindCatalogItemRequest request, ServerCallContext context)
    {
        var item = await _context.CatalogItems
            .Include(x => x.CatalogBrand)
            .Include(x => x.CatalogType)
            .FirstOrDefaultAsync(x => x.Id == request.Id);

        var response = new FindCatalogItemResponse();
        if (item != null)
        {
            response.Item = Mapping.ToProto(item);
        }

        return response;
    }

    public override async Task<GetCatalogBrandsResponse> GetCatalogBrands(
        Empty request, ServerCallContext context)
    {
        var brands = await _context.CatalogBrands.ToListAsync();

        var response = new GetCatalogBrandsResponse();
        response.Brands.AddRange(brands.Select(Mapping.ToProto));
        return response;
    }

    public override async Task<GetCatalogItemsResponse> GetCatalogItems(
        GetCatalogItemsRequest request, ServerCallContext context)
    {
        var brandFilterIsNull = request.BrandIdFilter == 0;
        var typeFilterIsNull = request.TypeIdFilter == 0;

        var items = await _context.CatalogItems
            .Where(x =>
                (brandFilterIsNull || x.CatalogBrandId == request.BrandIdFilter) &&
                (typeFilterIsNull || x.CatalogTypeId == request.TypeIdFilter))
            .ToListAsync();

        var response = new GetCatalogItemsResponse();
        response.Items.AddRange(items.Select(Mapping.ToProto));
        return response;
    }

    public override async Task<GetCatalogTypesResponse> GetCatalogTypes(
        Empty request, ServerCallContext context)
    {
        var types = await _context.CatalogTypes.ToListAsync();

        var response = new GetCatalogTypesResponse();
        response.CatalogTypes.AddRange(types.Select(Mapping.ToProto));
        return response;
    }

    public override async Task<GetAvailableStockResponse> GetAvailableStock(
        GetAvailableStockRequest request, ServerCallContext context)
    {
        var date = Mapping.ToDateTime(request.Date);

        var stocks = await _context.CatalogItemsStocks
            .Where(x => x.CatalogItemId == request.CatalogItemId)
            .ToListAsync();

        var stock = stocks.FirstOrDefault(y => y.Date.Date == date.Date);

        return new GetAvailableStockResponse
        {
            AvailableStock = stock?.AvailableStock ?? 0,
        };
    }

    public override async Task<Empty> CreateAvailableStock(
        CatalogItemsStock request, ServerCallContext context)
    {
        var date = Mapping.ToDateTime(request.Date);

        var existing = (await _context.CatalogItemsStocks
                .Where(x => x.CatalogItemId == request.CatalogItemId)
                .ToListAsync())
            .FirstOrDefault(y => y.Date.Date == date.Date);

        // Overwrite the stock for that date if one already exists, otherwise insert.
        if (existing != null)
        {
            existing.AvailableStock = request.AvailableStock;
            _context.Update(existing);
        }
        else
        {
            _context.CatalogItemsStocks.Add(new Domain.CatalogItemsStock
            {
                CatalogItemId = request.CatalogItemId,
                Date = date,
                AvailableStock = request.AvailableStock,
            });
        }

        await _context.SaveChangesAsync();
        return new Empty();
    }

    public override async Task<Empty> CreateCatalogItem(
        CatalogItem request, ServerCallContext context)
    {
        _context.CatalogItems.Add(Mapping.ToDomain(request));
        await _context.SaveChangesAsync();
        return new Empty();
    }

    public override async Task<Empty> UpdateCatalogItem(
        CatalogItem request, ServerCallContext context)
    {
        _context.Update(Mapping.ToDomain(request));
        await _context.SaveChangesAsync();
        return new Empty();
    }

    public override async Task<Empty> RemoveCatalogItem(
        CatalogItem request, ServerCallContext context)
    {
        var existing = await _context.CatalogItems.FindAsync(request.Id);
        if (existing != null)
        {
            _context.CatalogItems.Remove(existing);
            await _context.SaveChangesAsync();
        }

        return new Empty();
    }

    public override async Task<GetDiscountResponse> GetDiscount(
        GetDiscountRequest request, ServerCallContext context)
    {
        var day = Mapping.ToDateTime(request.Day);

        var discount = (await _context.DiscountItems.ToListAsync())
            .FirstOrDefault(y => y.Start.Date <= day.Date && y.End.Date >= day.Date);

        var response = new GetDiscountResponse();
        if (discount != null)
        {
            response.Discount = Mapping.ToProto(discount);
        }

        return response;
    }
}
