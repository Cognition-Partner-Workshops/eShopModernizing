namespace eShop.Web.Configuration;

/// <summary>
/// Settings owned by the MVC front end only, bound from the <c>CatalogWeb</c> configuration
/// section.
/// </summary>
public sealed class CatalogWebOptions
{
    public const string SectionName = "CatalogWeb";

    /// <summary>
    /// Absolute base address of the service that serves <c>items/{id}/pic</c> (the catalog API).
    /// Empty means "the current request's scheme and host", which reproduces the legacy behaviour
    /// of <c>Url.RouteUrl(PicController.GetPicRouteName, …, Request.Url.Scheme)</c>.
    /// </summary>
    public string PicturesBaseUrl { get; set; } = string.Empty;
}
