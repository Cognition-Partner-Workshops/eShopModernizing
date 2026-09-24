using eShop.Catalog.Api.Dtos;
using eShop.Catalog.Api.Infrastructure;
using eShop.Catalog.Api.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eShop.Catalog.Api.Controllers;

[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    private readonly CatalogContext _context;

    public CatalogController(CatalogContext context)
    {
        _context = context;
    }

    /// <summary>Zero-based paged list of items, optionally filtered by brand and/or type.</summary>
    [HttpGet("items")]
    [ProducesResponseType(typeof(PaginatedItemsDto<CatalogItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedItemsDto<CatalogItemDto>>> Items(
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int? brandId = null,
        [FromQuery] int? typeId = null)
    {
        if (pageSize <= 0 || pageIndex < 0)
        {
            return BadRequest("pageSize must be > 0 and pageIndex must be >= 0");
        }

        IQueryable<CatalogItem> query = _context.CatalogItems;
        if (brandId.HasValue) query = query.Where(i => i.CatalogBrandId == brandId.Value);
        if (typeId.HasValue) query = query.Where(i => i.CatalogTypeId == typeId.Value);

        var total = await query.LongCountAsync();
        var items = await query
            .Include(i => i.CatalogBrand)
            .Include(i => i.CatalogType)
            .OrderBy(i => i.Id)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .Select(i => ToDto(i))
            .ToListAsync();

        return new PaginatedItemsDto<CatalogItemDto>(pageIndex, pageSize, total, items);
    }

    [HttpGet("items/{id:int}")]
    [ProducesResponseType(typeof(CatalogItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CatalogItemDto>> ItemById(int id)
    {
        var item = await _context.CatalogItems
            .Include(i => i.CatalogBrand)
            .Include(i => i.CatalogType)
            .FirstOrDefaultAsync(i => i.Id == id);

        return item is null ? NotFound() : ToDto(item);
    }

    [HttpPost("items")]
    [ProducesResponseType(typeof(CatalogItemDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CatalogItemDto>> CreateItem([FromBody] CatalogItemWriteDto dto)
    {
        var item = new CatalogItem();
        Apply(dto, item);
        _context.CatalogItems.Add(item);
        await _context.SaveChangesAsync();
        await _context.Entry(item).Reference(i => i.CatalogBrand).LoadAsync();
        await _context.Entry(item).Reference(i => i.CatalogType).LoadAsync();
        return CreatedAtAction(nameof(ItemById), new { id = item.Id }, ToDto(item));
    }

    [HttpPut("items/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] CatalogItemWriteDto dto)
    {
        var item = await _context.CatalogItems.FindAsync(id);
        if (item is null) return NotFound();
        Apply(dto, item);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("items/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var item = await _context.CatalogItems.FindAsync(id);
        if (item is null) return NotFound();
        _context.CatalogItems.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("brands")]
    public async Task<ActionResult<IReadOnlyList<CatalogBrandDto>>> Brands() =>
        await _context.CatalogBrands.OrderBy(b => b.Id).Select(b => new CatalogBrandDto(b.Id, b.Brand)).ToListAsync();

    [HttpGet("types")]
    public async Task<ActionResult<IReadOnlyList<CatalogTypeDto>>> Types() =>
        await _context.CatalogTypes.OrderBy(t => t.Id).Select(t => new CatalogTypeDto(t.Id, t.Type)).ToListAsync();

    private static void Apply(CatalogItemWriteDto dto, CatalogItem item)
    {
        item.Name = dto.Name;
        item.Description = dto.Description;
        item.Price = dto.Price;
        item.PictureFileName = dto.PictureFileName;
        item.CatalogTypeId = dto.CatalogTypeId;
        item.CatalogBrandId = dto.CatalogBrandId;
        item.AvailableStock = dto.AvailableStock;
        item.RestockThreshold = dto.RestockThreshold;
        item.MaxStockThreshold = dto.MaxStockThreshold;
        item.OnReorder = dto.OnReorder;
    }

    private static CatalogItemDto ToDto(CatalogItem i) => new(
        i.Id, i.Name, i.Description, i.Price, i.PictureFileName,
        i.CatalogTypeId, i.CatalogType?.Type,
        i.CatalogBrandId, i.CatalogBrand?.Brand,
        i.AvailableStock, i.RestockThreshold, i.MaxStockThreshold, i.OnReorder);
}
