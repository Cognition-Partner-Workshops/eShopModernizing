using System.Diagnostics;
using Catalog.Mvc.Models;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Mvc.Controllers;

/// <summary>
/// Hosts the shared error page used by the exception handler
/// (<c>UseExceptionHandler("/Home/Error")</c>).
/// </summary>
public class HomeController : Controller
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
        });
    }
}
