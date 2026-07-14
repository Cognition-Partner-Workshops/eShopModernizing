using System.Web.Routing;

namespace eShopLegacyWebForms
{
    public static class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            // Let the /health IHttpHandler serve the request instead of Web Forms routing.
            routes.Add("HealthCheck", new Route("health", new System.Web.Routing.StopRoutingHandler()));
            routes.MapPageRoute(
                "",
                "Default",
                "~/Default.aspx"
                );
            routes.MapPageRoute(
                "ProductsByPageRoute",
                "Default/index/{index}/size/{size}",
                "~/Default.aspx"
                );
            routes.MapPageRoute(
                "CreateProductRoute",
                "Catalog/Create",
                "~/Catalog/Create.aspx"
                );
            routes.MapPageRoute(
                "EditProductRoute",
                "Catalog/Edit/{id}",
                "~/Catalog/Edit.aspx"
                );
            routes.MapPageRoute(
                "ProductDetailsRoute",
                "Catalog/Details/{id}",
                "~/Catalog/Details.aspx"
                );
            routes.MapPageRoute(
                "DeleteProductRoute",
                "Catalog/Delete/{id}",
                "~/Catalog/Delete.aspx"
                );
        }
    }
}
