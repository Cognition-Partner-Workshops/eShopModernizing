using Microsoft.AspNetCore.Mvc;

namespace eShop.Web.Controllers;

/// <summary>
/// Only hosts the error page. The catalog is the application root, exactly as it was under the
/// legacy default route (<c>controller = Catalog, action = Index</c>).
/// </summary>
public class HomeController : Controller
{
    /// <summary>Target of the exception handler middleware that replaces HandleErrorAttribute.</summary>
    public IActionResult Error() => View("Error");
}
