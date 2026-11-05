using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace eShopModernizedMVC.Controllers.Api
{
    [Route("api")]
    public class CatalogController2 : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Json(new { Message = "Hello World!" });
        }
    }
}
