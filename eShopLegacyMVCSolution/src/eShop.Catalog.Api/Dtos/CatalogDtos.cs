using System.ComponentModel.DataAnnotations;

namespace eShop.Catalog.Api.Dtos;

public record CatalogBrandDto(int Id, string Brand);

public record CatalogTypeDto(int Id, string Type);

public record CatalogItemDto(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    string? PictureFileName,
    int CatalogTypeId,
    string? CatalogType,
    int CatalogBrandId,
    string? CatalogBrand,
    int AvailableStock,
    int RestockThreshold,
    int MaxStockThreshold,
    bool OnReorder);

public class CatalogItemWriteDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Range(0, 1000000)]
    public decimal Price { get; set; }
    public string? PictureFileName { get; set; }
    public int CatalogTypeId { get; set; }
    public int CatalogBrandId { get; set; }
    [Range(0, 10000000)]
    public int AvailableStock { get; set; }
    [Range(0, 10000000)]
    public int RestockThreshold { get; set; }
    [Range(0, 10000000)]
    public int MaxStockThreshold { get; set; }
    public bool OnReorder { get; set; }
}

/// <summary>Zero-based page of items: <see cref="PageIndex"/> 0 is the first page.</summary>
public record PaginatedItemsDto<T>(int PageIndex, int PageSize, long Count, IReadOnlyList<T> Data);
