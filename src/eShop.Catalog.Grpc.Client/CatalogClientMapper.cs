using Google.Protobuf.WellKnownTypes;
using Proto = eShop.Catalog.Grpc.Protos;
using DomainBrand = eShop.Catalog.Domain.CatalogBrand;
using DomainDiscount = eShop.Catalog.Domain.DiscountItem;
using DomainItem = eShop.Catalog.Domain.CatalogItem;
using DomainStock = eShop.Catalog.Domain.CatalogItemsStock;
using DomainType = eShop.Catalog.Domain.CatalogType;

namespace eShop.Catalog.Grpc.Client;

/// <summary>
/// Client-side counterpart of the service mapper: protobuf messages in, domain objects out (and
/// back again for the mutating operations). The conversions mirror
/// <c>eShop.Catalog.Grpc.Mapping.CatalogProtoMapper</c> exactly, including the exact-decimal
/// <c>DecimalValue</c> encoding and the UTC timestamp convention.
/// </summary>
public static class CatalogClientMapper
{
    private const decimal NanoFactor = 1_000_000_000M;

    public static Proto.DecimalValue ToDecimalValue(decimal value)
    {
        var units = decimal.ToInt64(decimal.Truncate(value));
        var nanos = decimal.ToInt32((value - units) * NanoFactor);

        return new Proto.DecimalValue { Units = units, Nanos = nanos };
    }

    public static decimal ToDecimal(Proto.DecimalValue? value) =>
        value is null ? 0M : value.Units + (value.Nanos / NanoFactor);

    public static Timestamp ToTimestamp(DateTime value) =>
        Timestamp.FromDateTime(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    public static DateTime ToDateTime(Timestamp? value) =>
        value is null ? default : DateTime.SpecifyKind(value.ToDateTime(), DateTimeKind.Unspecified);

    public static DomainBrand ToDomain(Proto.CatalogBrand brand) =>
        new() { Id = brand.Id, Brand = brand.Brand };

    public static DomainType ToDomain(Proto.CatalogType type) =>
        new() { Id = type.Id, Type = type.Type };

    public static DomainItem ToDomain(Proto.CatalogItem item) =>
        new()
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Price = ToDecimal(item.Price),
            PictureFileName = item.PictureFilename,
            CatalogBrandId = item.CatalogBrandId,
            CatalogTypeId = item.CatalogTypeId,
            CatalogBrand = item.CatalogBrand is null ? null : ToDomain(item.CatalogBrand),
            CatalogType = item.CatalogType is null ? null : ToDomain(item.CatalogType),
        };

    public static DomainStock ToDomain(Proto.CatalogItemsStock stock) =>
        new()
        {
            StockId = stock.StockId,
            Date = ToDateTime(stock.Date),
            CatalogItemId = stock.CatalogItemId,
            AvailableStock = stock.AvailableStock,
        };

    public static DomainDiscount ToDomain(Proto.DiscountItem discount) =>
        new()
        {
            Id = discount.Id,
            Size = discount.Size,
            Start = ToDateTime(discount.Start),
            End = ToDateTime(discount.End),
        };

    public static Proto.CatalogItem ToProto(DomainItem item)
    {
        var message = new Proto.CatalogItem
        {
            Id = item.Id,
            Description = item.Description ?? string.Empty,
            Name = item.Name,
            Price = ToDecimalValue(item.Price),
            PictureFilename = item.PictureFileName,
            CatalogBrandId = item.CatalogBrandId,
            CatalogTypeId = item.CatalogTypeId,
        };

        if (item.CatalogBrand is not null)
        {
            message.CatalogBrand = new Proto.CatalogBrand { Id = item.CatalogBrand.Id, Brand = item.CatalogBrand.Brand };
        }

        if (item.CatalogType is not null)
        {
            message.CatalogType = new Proto.CatalogType { Id = item.CatalogType.Id, Type = item.CatalogType.Type };
        }

        return message;
    }

    public static Proto.CatalogItemsStock ToProto(DomainStock stock) =>
        new()
        {
            StockId = stock.StockId,
            Date = ToTimestamp(stock.Date),
            CatalogItemId = stock.CatalogItemId,
            AvailableStock = stock.AvailableStock,
        };
}
