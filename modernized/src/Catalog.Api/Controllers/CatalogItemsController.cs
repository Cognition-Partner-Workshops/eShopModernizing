using Catalog.Domain;
using Catalog.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Controllers;

/// <summary>
/// REST surface for catalog items. Combines the legacy MVC <c>CatalogController</c>
/// CRUD actions and the WCF <c>GetCatalogItems(brandIdFilter, typeIdFilter)</c>
/// filtered listing into a single <c>[ApiController]</c>. Items are always
/// ordered by id to reproduce the deterministic legacy ordering.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CatalogItemsController : ControllerBase
{
    private readonly CatalogDbContext _dbContext;

    public CatalogItemsController(CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Lists catalog items, optionally filtered by brand and/or type and paged
    /// with <paramref name="skip"/>/<paramref name="take"/> (mirroring the
    /// legacy <c>Skip(pageSize * pageIndex).Take(pageSize)</c> pagination).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CatalogItem>>> Get(
        [FromQuery] int? brandId,
        [FromQuery] int? typeId,
        [FromQuery] int? skip,
        [FromQuery] int? take,
        CancellationToken cancellationToken)
    {
        IQueryable<CatalogItem> query = _dbContext.CatalogItems
            .AsNoTracking()
            .Include(ci => ci.CatalogBrand)
            .Include(ci => ci.CatalogType);

        if (brandId.HasValue)
        {
            query = query.Where(ci => ci.CatalogBrandId == brandId.Value);
        }

        if (typeId.HasValue)
        {
            query = query.Where(ci => ci.CatalogTypeId == typeId.Value);
        }

        query = query.OrderBy(ci => ci.Id);

        if (skip.HasValue)
        {
            query = query.Skip(skip.Value);
        }

        if (take.HasValue)
        {
            query = query.Take(take.Value);
        }

        var items = await query.ToListAsync(cancellationToken);

        return Ok(items);
    }

    /// <summary>Returns a single catalog item by id, or 404 when it does not exist.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CatalogItem>> Get(int id, CancellationToken cancellationToken)
    {
        var item = await _dbContext.CatalogItems
            .AsNoTracking()
            .Include(ci => ci.CatalogBrand)
            .Include(ci => ci.CatalogType)
            .FirstOrDefaultAsync(ci => ci.Id == id, cancellationToken);

        if (item is null)
        {
            return NotFound();
        }

        return Ok(item);
    }

    /// <summary>Creates a new catalog item and returns 201 with its location.</summary>
    [HttpPost]
    public async Task<ActionResult<CatalogItem>> Create(
        [FromBody] CatalogItem catalogItem,
        CancellationToken cancellationToken)
    {
        var created = new CatalogItem
        {
            Name = catalogItem.Name,
            Description = catalogItem.Description,
            Price = catalogItem.Price,
            PictureFileName = catalogItem.PictureFileName,
            CatalogTypeId = catalogItem.CatalogTypeId,
            CatalogBrandId = catalogItem.CatalogBrandId,
            AvailableStock = catalogItem.AvailableStock,
            RestockThreshold = catalogItem.RestockThreshold,
            MaxStockThreshold = catalogItem.MaxStockThreshold,
            OnReorder = catalogItem.OnReorder,
        };

        _dbContext.CatalogItems.Add(created);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Updates an existing catalog item, or returns 404 when it does not exist.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] CatalogItem catalogItem,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.CatalogItems
            .FirstOrDefaultAsync(ci => ci.Id == id, cancellationToken);

        if (existing is null)
        {
            return NotFound();
        }

        existing.Name = catalogItem.Name;
        existing.Description = catalogItem.Description;
        existing.Price = catalogItem.Price;
        existing.PictureFileName = catalogItem.PictureFileName;
        existing.CatalogTypeId = catalogItem.CatalogTypeId;
        existing.CatalogBrandId = catalogItem.CatalogBrandId;
        existing.AvailableStock = catalogItem.AvailableStock;
        existing.RestockThreshold = catalogItem.RestockThreshold;
        existing.MaxStockThreshold = catalogItem.MaxStockThreshold;
        existing.OnReorder = catalogItem.OnReorder;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>Deletes a catalog item, or returns 404 when it does not exist.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.CatalogItems
            .FirstOrDefaultAsync(ci => ci.Id == id, cancellationToken);

        if (existing is null)
        {
            return NotFound();
        }

        _dbContext.CatalogItems.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
