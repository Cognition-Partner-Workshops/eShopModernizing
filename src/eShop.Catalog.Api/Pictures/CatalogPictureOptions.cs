namespace eShop.Catalog.Api.Pictures;

/// <summary>
/// Where the catalog item pictures live. Replaces the legacy <c>Server.MapPath("~/Pics")</c>
/// lookup, which depended on <c>System.Web</c>; a relative path is resolved against the
/// application content root so a container can mount the folder elsewhere.
/// </summary>
public sealed class CatalogPictureOptions
{
    public const string SectionName = "Pictures";

    public string RootPath { get; set; } = "Pics";
}
