using System.Web.Mvc;
using System.Web.Routing;

namespace eShopLegacyMVC
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.MapMvcAttributeRoutes();
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Let the /health IHttpHandler serve the request instead of MVC routing.
            routes.IgnoreRoute("health");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Catalog", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
