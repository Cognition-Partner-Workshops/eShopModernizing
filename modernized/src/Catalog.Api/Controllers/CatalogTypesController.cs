using Catalog.Domain;
using Catalog.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Controllers;

/// <summary>
/// Exposes catalog types, mirroring the legacy <c>ICatalogService.GetCatalogTypes()</c>
/// operation used to populate the MVC/WebForms dropdowns.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CatalogTypesController : ControllerBase
{
    private readonly CatalogDbContext _dbContext;

    public CatalogTypesController(CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>Returns all catalog types ordered by id.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CatalogType>>> Get(CancellationToken cancellationToken)
    {
        var types = await _dbContext.CatalogTypes
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .ToListAsync(cancellationToken);

        return Ok(types);
    }
}
