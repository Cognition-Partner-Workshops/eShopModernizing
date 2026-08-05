using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace eShop.Web.Tests.Fakes;

/// <summary>
/// Minimal <see cref="IUrlHelper" /> returning the picture URL the legacy tests stubbed on their
/// mocked <c>UrlHelper</c>, so <c>CatalogController</c> can fill <c>CatalogItem.PictureUri</c>
/// outside a request pipeline.
/// </summary>
public sealed class StubUrlHelper : IUrlHelper
{
    public StubUrlHelper(ActionContext actionContext) => ActionContext = actionContext;

    public ActionContext ActionContext { get; }

    public string? Action(UrlActionContext actionContext) => "/";

    public string? Content(string? contentPath) => contentPath;

    public bool IsLocalUrl(string? url) => true;

    public string? Link(string? routeName, object? values) => RouteUrl(new UrlRouteContext { RouteName = routeName, Values = values });

    public string? RouteUrl(UrlRouteContext routeContext)
    {
        ArgumentNullException.ThrowIfNull(routeContext);

        var catalogItemId = new RouteValueDictionary(routeContext.Values)["catalogItemId"];

        return $"http://localhost/items/{catalogItemId}/pic";
    }
}
