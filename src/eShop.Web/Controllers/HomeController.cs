using Microsoft.AspNetCore.Mvc;

namespace eShop.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();

    /// <summary>Target of the exception handler middleware that replaces HandleErrorAttribute.</summary>
    public IActionResult Error() => View();
}
