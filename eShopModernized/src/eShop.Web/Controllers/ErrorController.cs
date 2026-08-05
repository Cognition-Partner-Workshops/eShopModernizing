using System.Diagnostics;
using eShop.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace eShop.Web.Controllers;

/// <summary>
/// Unhandled exception page, replacing the legacy <c>customErrors</c> redirect to
/// <c>Views/Shared/Error.cshtml</c>.
/// </summary>
public class ErrorController : Controller
{
    /// <summary>Renders the shared error view.</summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index() =>
        View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
