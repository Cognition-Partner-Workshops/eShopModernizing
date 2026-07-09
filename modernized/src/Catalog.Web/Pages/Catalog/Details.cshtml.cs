using Catalog.Domain;
using Catalog.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Web.Pages.Catalog;

/// <summary>
/// Read-only catalog item view, ported from the legacy Web Forms
/// <c>Catalog/Details.aspx</c>. Missing ids return 404, matching the behavioral
/// baseline ("missing items return 404").
/// </summary>
public class DetailsModel : PageModel
{
    private readonly CatalogDbContext _db;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(CatalogDbContext db, ILogger<DetailsModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    public CatalogItem Product { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        _logger.LogInformation("Now loading... /Catalog/Details?id={ProductId}", id);

        var product = await _db.CatalogItems
            .Include(c => c.CatalogBrand)
            .Include(c => c.CatalogType)
            .FirstOrDefaultAsync(ci => ci.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        Product = product;
        return Page();
    }
}
