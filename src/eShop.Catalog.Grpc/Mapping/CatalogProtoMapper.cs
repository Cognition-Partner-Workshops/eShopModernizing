using eShop.Catalog.Grpc.Protos;
using Google.Protobuf.WellKnownTypes;
using DomainBrand = eShop.Catalog.Domain.CatalogBrand;
using DomainDiscount = eShop.Catalog.Domain.DiscountItem;
using DomainItem = eShop.Catalog.Domain.CatalogItem;
using DomainStock = eShop.Catalog.Domain.CatalogItemsStock;
using DomainType = eShop.Catalog.Domain.CatalogType;

namespace eShop.Catalog.Grpc.Mapping;

/// <summary>
/// Translates between the domain model (<c>eShop.Catalog.Domain</c>) and the protobuf messages.
/// The protobuf side carries exactly the members the legacy <c>[DataContract]</c> types exposed —
/// no more, no less — so a gRPC response is field-for-field equivalent to the SOAP response.
/// </summary>
public static class CatalogProtoMapper
{
    private const decimal NanoFactor = 1_000_000_000M;

    public static DecimalValue ToDecimalValue(decimal value)
    {
        var units = decimal.ToInt64(decimal.Truncate(value));
        var nanos = decimal.ToInt32((value - units) * NanoFactor);

        return new DecimalValue { Units = units, Nanos = nanos };
    }

    public static decimal ToDecimal(DecimalValue? value) =>
        value is null ? 0M : value.Units + (value.Nanos / NanoFactor);

    /// <summary>
    /// The legacy columns are SQL <c>date</c>/<c>datetime</c> values with no offset. They are
    /// projected as UTC timestamps so the wire format is unambiguous.
    /// </summary>
    public static Timestamp ToTimestamp(DateTime value) =>
        Timestamp.FromDateTime(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    public static DateTime ToDateTime(Timestamp? value) =>
        value is null ? default : DateTime.SpecifyKind(value.ToDateTime(), DateTimeKind.Unspecified);

    public static CatalogBrand ToProto(DomainBrand brand) =>
        new() { Id = brand.Id, Brand = brand.Brand ?? string.Empty };

    public static CatalogType ToProto(DomainType type) =>
        new() { Id = type.Id, Type = type.Type ?? string.Empty };

    public static CatalogItem ToProto(DomainItem item)
    {
        var message = new CatalogItem
        {
            Id = item.Id,
            Description = item.Description ?? string.Empty,
            Name = item.Name ?? string.Empty,
            Price = ToDecimalValue(item.Price),
            PictureFilename = item.PictureFileName ?? string.Empty,
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

    public static DomainItem ToDomain(CatalogItem message) =>
        new()
        {
            Id = message.Id,
            Description = message.Description,
            Name = message.Name,
            Price = ToDecimal(message.Price),
            PictureFileName = string.IsNullOrEmpty(message.PictureFilename)
                ? DomainItem.DefaultPictureName
                : message.PictureFilename,
            CatalogBrandId = message.CatalogBrandId,
            CatalogTypeId = message.CatalogTypeId,
        };

    public static CatalogItemsStock ToProto(DomainStock stock) =>
        new()
        {
            Date = ToTimestamp(stock.Date),
            CatalogItemId = stock.CatalogItemId,
            AvailableStock = stock.AvailableStock,
            StockId = stock.StockId,
        };

    public static DomainStock ToDomain(CatalogItemsStock message) =>
        new()
        {
            Date = ToDateTime(message.Date),
            CatalogItemId = message.CatalogItemId,
            AvailableStock = message.AvailableStock,
            StockId = message.StockId,
        };

    public static DiscountItem ToProto(DomainDiscount discount) =>
        new()
        {
            Size = discount.Size,
            Start = ToTimestamp(discount.Start),
            End = ToTimestamp(discount.End),
            Id = discount.Id,
        };
}
