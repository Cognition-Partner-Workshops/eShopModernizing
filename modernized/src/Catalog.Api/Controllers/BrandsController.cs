using Catalog.Domain;
using Catalog.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Controllers;

/// <summary>
/// REST replacement for the legacy MVC Web API <c>BrandsController</c>
/// (<c>/api/brands</c>). Mirrors the observed baseline surface: list all brands
/// and look up a single brand by id.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BrandsController : ControllerBase
{
    private readonly CatalogDbContext _dbContext;

    public BrandsController(CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>Returns all catalog brands ordered by id.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CatalogBrand>>> Get(CancellationToken cancellationToken)
    {
        var brands = await _dbContext.CatalogBrands
            .AsNoTracking()
            .OrderBy(b => b.Id)
            .ToListAsync(cancellationToken);

        return Ok(brands);
    }

    /// <summary>Returns a single brand by id, or 404 when it does not exist.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CatalogBrand>> Get(int id, CancellationToken cancellationToken)
    {
        var brand = await _dbContext.CatalogBrands
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (brand is null)
        {
            return NotFound();
        }

        return Ok(brand);
    }
}
