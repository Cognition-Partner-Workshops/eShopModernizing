using System.Globalization;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using DomainBrand = eShop.Catalog.Domain.Entities.CatalogBrand;
using DomainItem = eShop.Catalog.Domain.Entities.CatalogItem;
using DomainStock = eShop.Catalog.Domain.Entities.CatalogItemsStock;
using DomainType = eShop.Catalog.Domain.Entities.CatalogType;
using DomainDiscount = eShop.Catalog.Domain.Entities.DiscountItem;

namespace eShop.Catalog.Grpc.Mapping;

/// <summary>
/// Conversions between the canonical domain entities and the <c>catalog.proto</c> messages.
/// </summary>
internal static class ProtoMapping
{
    /// <summary>
    /// Group separators are rejected on purpose: "19,5" is a decimal comma in most cultures, and
    /// silently reading it as 195 would be worse than failing the call.
    /// </summary>
    private const NumberStyles DecimalStyles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    public static DecimalValue ToDecimalValue(decimal value)
        => new() { Value = value.ToString(CultureInfo.InvariantCulture) };

    public static decimal ToDecimal(this DecimalValue? value, string fieldName)
    {
        if (value is null)
        {
            return 0m;
        }

        return decimal.TryParse(value.Value, DecimalStyles, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                $"'{fieldName}' must be a decimal in the invariant culture, but was '{value.Value}'."));
    }

    public static Timestamp ToTimestamp(DateTime value)
        => Timestamp.FromDateTime(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    /// <summary>
    /// Reads the date component of a timestamp. The legacy service compared <c>DateTime.Date</c>
    /// values against SQL <c>date</c> columns, so the time of day is deliberately discarded.
    /// </summary>
    public static DateTime ToDate(this Timestamp? value, string fieldName)
        => value is null
            ? throw new RpcException(new Status(StatusCode.InvalidArgument, $"'{fieldName}' is required."))
            : value.ToDateTime().Date;

    public static CatalogBrand ToProto(this DomainBrand brand)
        => new() { Id = brand.Id, Brand = brand.Brand };

    public static CatalogType ToProto(this DomainType type)
        => new() { Id = type.Id, Type = type.Type };

    public static CatalogItem ToProto(this DomainItem item)
    {
        var message = new CatalogItem
        {
            Id = item.Id,
            Description = item.Description ?? string.Empty,
            Name = item.Name,
            Price = ToDecimalValue(item.Price),
            // WCF `Picturefilename` -> domain `PictureFileName`.
            PictureFileName = item.PictureFileName,
            CatalogBrandId = item.CatalogBrandId,
            CatalogTypeId = item.CatalogTypeId,
        };

        if (item.CatalogBrand is not null)
        {
            message.CatalogBrand = item.CatalogBrand.ToProto();
        }

        if (item.CatalogType is not null)
        {
            message.CatalogType = item.CatalogType.ToProto();
        }

        return message;
    }

    public static CatalogItemsStock ToProto(this DomainStock stock)
        => new()
        {
            Date = ToTimestamp(stock.Date),
            CatalogItemId = stock.CatalogItemId,
            AvailableStock = stock.AvailableStock,
            StockId = stock.StockId,
        };

    public static DiscountItem ToProto(this DomainDiscount discount)
        => new()
        {
            Id = discount.Id,
            Size = discount.Size,
            Start = ToTimestamp(discount.Start),
            End = ToTimestamp(discount.End),
        };

    /// <summary>
    /// Copies the fields of the legacy <c>CatalogItem</c> data contract onto a domain entity.
    /// Columns the WCF contract never carried (stock thresholds, <c>OnReorder</c>) are left alone
    /// so an update cannot silently reset them.
    /// </summary>
    public static void CopyContractFieldsTo(this CatalogItem message, DomainItem entity)
    {
        entity.Name = message.Name;
        entity.Description = message.Description;
        entity.Price = message.Price.ToDecimal("price");
        entity.PictureFileName = string.IsNullOrEmpty(message.PictureFileName)
            ? DomainItem.DefaultPictureName
            : message.PictureFileName;
        entity.CatalogBrandId = message.CatalogBrandId;
        entity.CatalogTypeId = message.CatalogTypeId;
    }
}
