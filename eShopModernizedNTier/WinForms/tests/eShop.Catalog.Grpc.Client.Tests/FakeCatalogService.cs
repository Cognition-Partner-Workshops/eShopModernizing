using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Proto = eShop.Catalog.Grpc.Protos;

namespace eShop.Catalog.Grpc.Client.Tests;

// An isolated wire-level fake, not evidence of parity with the production service.
public sealed class FakeCatalogService
{
    private readonly List<Proto.CatalogBrand> _brands = [
        new() { Id = 1, Brand = "Azure" }, new() { Id = 2, Brand = ".NET" },
        new() { Id = 3, Brand = "Visual Studio" }, new() { Id = 4, Brand = "SQL Server" }, new() { Id = 5, Brand = "Other" }];
    private readonly List<Proto.CatalogType> _types = [
        new() { Id = 1, Type = "Mug" }, new() { Id = 2, Type = "T-Shirt" },
        new() { Id = 3, Type = "Sheet" }, new() { Id = 4, Type = "USB Memory Stick" }];
    private readonly List<Proto.CatalogItem> _items;
    private readonly Dictionary<(int, DateTime), int> _stock = new() { [(1, new DateTime(2017, 9, 21))] = 120 };

    public FakeCatalogService()
    {
        string[] names = [".NET Bot Black Hoodie", ".NET Black & White Mug", "Prism White T-Shirt", ".NET Foundation T-shirt", "Roslyn Red Sheet", ".NET Blue Hoodie", "Roslyn Red T-Shirt", "Kudu Purple Hoodie", "Cup<T> White Mug", ".NET Foundation Sheet", "Cup<T> Sheet", "Prism White TShirt"];
        int[] brands = [2, 2, 5, 2, 5, 2, 5, 5, 5, 2, 2, 5];
        int[] types = [2, 1, 2, 2, 3, 2, 2, 2, 1, 3, 3, 2];
        decimal[] prices = [19.5M, 8.5M, 12, 12, 8.5M, 12, 12, 8.5M, 12, 12, 8.5M, 12];
        _items = names.Select((name, i) => new Proto.CatalogItem
        {
            Id = i + 1,
            Name = name,
            Description = name,
            PictureFilename = $"{i + 1}.png",
            Price = CatalogClientMapper.ToDecimalValue(prices[i]),
            CatalogBrandId = brands[i],
            CatalogTypeId = types[i],
            CatalogBrand = _brands.Single(b => b.Id == brands[i]),
            CatalogType = _types.Single(t => t.Id == types[i])
        }).ToList();
    }

    public static void BindService(ServiceBinderBase binder, FakeCatalogService? service)
    {
        Add<Proto.FindCatalogItemRequest, Proto.FindCatalogItemResponse>(binder, "FindCatalogItem", request =>
        {
            if (request.Id <= 0) throw new RpcException(new Status(StatusCode.InvalidArgument, "id"));
            return new() { Item = service!._items.Find(i => i.Id == request.Id) ?? throw new RpcException(new Status(StatusCode.NotFound, "item")) };
        });
        Add<Proto.GetCatalogBrandsRequest, Proto.GetCatalogBrandsResponse>(binder, "GetCatalogBrands", _ => { var response = new Proto.GetCatalogBrandsResponse(); response.Brands.AddRange(service!._brands); return response; });
        Add<Proto.GetCatalogTypesRequest, Proto.GetCatalogTypesResponse>(binder, "GetCatalogTypes", _ => { var response = new Proto.GetCatalogTypesResponse(); response.Types_.AddRange(service!._types); return response; });
        Add<Proto.GetCatalogItemsRequest, Proto.GetCatalogItemsResponse>(binder, "GetCatalogItems", request =>
        {
            var response = new Proto.GetCatalogItemsResponse();
            response.Items.AddRange(service!._items.Where(i => (request.BrandIdFilter == 0 || i.CatalogBrandId == request.BrandIdFilter) && (request.TypeIdFilter == 0 || i.CatalogTypeId == request.TypeIdFilter)));
            return response;
        });
        Add<Proto.CreateCatalogItemRequest, Proto.CreateCatalogItemResponse>(binder, "CreateCatalogItem", request => { var item = request.CatalogItem.Clone(); item.Id = service!._items.Max(i => i.Id) + 1; service._items.Add(item); return new(); });
        Add<Proto.UpdateCatalogItemRequest, Proto.UpdateCatalogItemResponse>(binder, "UpdateCatalogItem", request => { service!._items.RemoveAll(i => i.Id == request.CatalogItem.Id); service._items.Add(request.CatalogItem); return new(); });
        Add<Proto.RemoveCatalogItemRequest, Proto.RemoveCatalogItemResponse>(binder, "RemoveCatalogItem", request => { service!._items.RemoveAll(i => i.Id == request.CatalogItem.Id); return new(); });
        Add<Proto.GetAvailableStockRequest, Proto.GetAvailableStockResponse>(binder, "GetAvailableStock", request => new() { AvailableStock = service!._stock.GetValueOrDefault((request.CatalogItemId, request.Date.ToDateTime().Date)) });
        Add<Proto.CreateAvailableStockRequest, Proto.CreateAvailableStockResponse>(binder, "CreateAvailableStock", request => { var stock = request.CatalogItemsStock; service!._stock[(stock.CatalogItemId, stock.Date.ToDateTime().Date)] = stock.AvailableStock; return new(); });
        Add<Proto.GetDiscountRequest, Proto.GetDiscountResponse>(binder, "GetDiscount", request =>
        {
            var day = request.Day.ToDateTime().Date;
            return day >= new DateTime(2017, 9, 18) && day <= new DateTime(2017, 9, 21)
                ? new() { Discount = new() { Size = 0.3, Start = Timestamp.FromDateTime(new DateTime(2017, 9, 18, 0, 0, 0, DateTimeKind.Utc)), End = Timestamp.FromDateTime(new DateTime(2017, 9, 21, 0, 0, 0, DateTimeKind.Utc)) } }
                : new();
        });
    }

    private static void Add<TRequest, TResponse>(ServiceBinderBase binder, string name, Func<TRequest, TResponse> handler)
        where TRequest : class, IMessage<TRequest>, new()
        where TResponse : class, IMessage<TResponse>, new()
    {
        var requestMarshaller = Marshallers.Create<TRequest>(message => message.ToByteArray(), bytes => new MessageParser<TRequest>(() => new()).ParseFrom(bytes));
        var responseMarshaller = Marshallers.Create<TResponse>(message => message.ToByteArray(), bytes => new MessageParser<TResponse>(() => new()).ParseFrom(bytes));
        binder.AddMethod(new Method<TRequest, TResponse>(MethodType.Unary, "eshop.catalog.v1.Catalog", name, requestMarshaller, responseMarshaller), (request, _) => Task.FromResult(handler(request)));
    }
}
