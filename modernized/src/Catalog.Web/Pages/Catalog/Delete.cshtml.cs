using Catalog.Domain;
using Catalog.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Web.Pages.Catalog;

/// <summary>
/// Delete confirmation page, ported from the legacy Web Forms
/// <c>Catalog/Delete.aspx</c> and its <c>Delete_Click</c> code-behind.
/// </summary>
public class DeleteModel : PageModel
{
    private readonly CatalogDbContext _db;
    private readonly ILogger<DeleteModel> _logger;

    public DeleteModel(CatalogDbContext db, ILogger<DeleteModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    public CatalogItem ProductToDelete { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        _logger.LogInformation("Now loading... /Catalog/Delete?id={ProductId}", id);

        var product = await _db.CatalogItems
            .Include(c => c.CatalogBrand)
            .Include(c => c.CatalogType)
            .FirstOrDefaultAsync(ci => ci.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        ProductToDelete = product;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var product = await _db.CatalogItems.FirstOrDefaultAsync(ci => ci.Id == id);
        if (product is null)
        {
            return NotFound();
        }

        _db.CatalogItems.Remove(product);
        await _db.SaveChangesAsync();

        return RedirectToPage("Index");
    }
}
