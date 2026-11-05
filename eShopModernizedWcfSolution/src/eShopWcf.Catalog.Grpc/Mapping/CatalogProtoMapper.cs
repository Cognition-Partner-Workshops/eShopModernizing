using eShop.Catalog.Grpc.Protos;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using DomainBrand = eShopWcf.Catalog.Data.CatalogBrand;
using DomainDiscount = eShopWcf.Catalog.Data.DiscountItem;
using DomainItem = eShopWcf.Catalog.Data.CatalogItem;
using DomainStock = eShopWcf.Catalog.Data.CatalogItemsStock;
using DomainType = eShopWcf.Catalog.Data.CatalogType;

namespace eShopWcf.Catalog.Grpc.Mapping;

public static class CatalogProtoMapper
{
    private const decimal NanoFactor = 1_000_000_000M;

    public static DecimalValue ToDecimalValue(decimal value)
    {
        var units = decimal.ToInt64(decimal.Truncate(value));
        var nanos = decimal.ToInt32((value - units) * NanoFactor);
        return new DecimalValue { Units = units, Nanos = nanos };
    }

    public static decimal ToDecimal(DecimalValue? value)
    {
        if (value is null)
        {
            return 0M;
        }

        if (value.Nanos is <= -1_000_000_000 or >= 1_000_000_000
            || (value.Units > 0 && value.Nanos < 0)
            || (value.Units < 0 && value.Nanos > 0))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid decimal units/nanos."));
        }

        return value.Units + (value.Nanos / NanoFactor);
    }

    public static Timestamp ToTimestamp(DateTime value) =>
        Timestamp.FromDateTime(DateTime.SpecifyKind(value.Date, DateTimeKind.Utc));

    public static DateTime ToDateTime(Timestamp? value)
    {
        if (value is null)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Timestamp is required."));
        }

        try
        {
            return DateTime.SpecifyKind(value.ToDateTime().Date, DateTimeKind.Unspecified);
        }
        catch (InvalidOperationException)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Timestamp is invalid."));
        }
    }

    public static CatalogBrand ToProto(DomainBrand brand)
    {
        ArgumentNullException.ThrowIfNull(brand);
        return new() { Id = brand.Id, Brand = brand.Brand ?? string.Empty };
    }

    public static CatalogType ToProto(DomainType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return new() { Id = type.Id, Type = type.Type ?? string.Empty };
    }

    public static CatalogItem ToProto(DomainItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var message = new CatalogItem
        {
            Id = item.Id,
            Description = item.Description ?? string.Empty,
            Name = item.Name ?? string.Empty,
            Price = ToDecimalValue(item.Price),
            PictureFilename = item.Picturefilename ?? string.Empty,
            CatalogBrandId = item.CatalogBrandId,
            CatalogTypeId = item.CatalogTypeId,
        };
        if (item.CatalogBrand is not null)
        {
            message.CatalogBrand = ToProto(item.CatalogBrand);
        }

        if (item.CatalogType is not null)
        {
            message.CatalogType = ToProto(item.CatalogType);
        }

        return message;
    }

    public static DomainItem ToDomain(CatalogItem message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return new()
        {
            Id = message.Id,
            Description = message.Description,
            Name = message.Name,
            Price = ToDecimal(message.Price),
            Picturefilename = message.PictureFilename,
            CatalogBrandId = message.CatalogBrandId,
            CatalogTypeId = message.CatalogTypeId,
        };
    }

    public static CatalogItemsStock ToProto(DomainStock stock)
    {
        ArgumentNullException.ThrowIfNull(stock);
        return new()
        {
            Date = ToTimestamp(stock.Date),
            CatalogItemId = stock.CatalogItemId,
            AvailableStock = stock.AvailableStock,
            StockId = stock.StockId,
        };
    }

    public static DomainStock ToDomain(CatalogItemsStock message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return new()
        {
            Date = ToDateTime(message.Date),
            CatalogItemId = message.CatalogItemId,
            AvailableStock = message.AvailableStock,
            StockId = message.StockId,
        };
    }

    public static DiscountItem ToProto(DomainDiscount discount)
    {
        ArgumentNullException.ThrowIfNull(discount);
        return new()
        {
            Id = discount.Id,
            Size = discount.Size,
            Start = ToTimestamp(discount.Start),
            End = ToTimestamp(discount.End),
        };
    }
}
