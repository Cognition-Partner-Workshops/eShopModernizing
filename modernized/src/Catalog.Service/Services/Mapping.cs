using Catalog.Service.Protos;
using Google.Protobuf.WellKnownTypes;
using Domain = Catalog.Domain;

namespace Catalog.Service.Services;

/// <summary>
/// Translates between EF Core domain entities (<see cref="Catalog.Domain"/>) and
/// the protobuf message types generated from <c>catalog.proto</c>.
/// </summary>
internal static class Mapping
{
    public static CatalogItem ToProto(Domain.CatalogItem item) =>
        new()
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            PictureFileName = item.PictureFileName,
            CatalogTypeId = item.CatalogTypeId,
            CatalogType = item.CatalogType is null ? null : ToProto(item.CatalogType),
            CatalogBrandId = item.CatalogBrandId,
            CatalogBrand = item.CatalogBrand is null ? null : ToProto(item.CatalogBrand),
            AvailableStock = item.AvailableStock,
            RestockThreshold = item.RestockThreshold,
            MaxStockThreshold = item.MaxStockThreshold,
            OnReorder = item.OnReorder,
        };

    public static CatalogBrand ToProto(Domain.CatalogBrand brand) =>
        new()
        {
            Id = brand.Id,
            Brand = brand.Brand,
        };

    public static CatalogType ToProto(Domain.CatalogType type) =>
        new()
        {
            Id = type.Id,
            Type = type.Type,
        };

    public static DiscountItem ToProto(Domain.DiscountItem discount) =>
        new()
        {
            Id = discount.Id,
            Size = discount.Size,
            Start = ToTimestamp(discount.Start),
            End = ToTimestamp(discount.End),
        };

    // Only scalar fields are mapped for writes; nested brand/type navigations are
    // ignored so EF Core does not attempt to insert/update related aggregates.
    public static Domain.CatalogItem ToDomain(CatalogItem item) =>
        new()
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price is null ? 0m : (decimal)item.Price,
            PictureFileName = string.IsNullOrEmpty(item.PictureFileName)
                ? Domain.CatalogItem.DefaultPictureName
                : item.PictureFileName,
            CatalogTypeId = item.CatalogTypeId,
            CatalogBrandId = item.CatalogBrandId,
            AvailableStock = item.AvailableStock,
            RestockThreshold = item.RestockThreshold,
            MaxStockThreshold = item.MaxStockThreshold,
            OnReorder = item.OnReorder,
        };

    public static DateTime ToDateTime(Timestamp? timestamp) =>
        timestamp?.ToDateTime() ?? default;

    public static Timestamp ToTimestamp(DateTime value) =>
        Timestamp.FromDateTime(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
