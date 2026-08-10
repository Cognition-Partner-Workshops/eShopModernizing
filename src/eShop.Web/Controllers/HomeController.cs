using Microsoft.AspNetCore.Mvc;

namespace eShop.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}
