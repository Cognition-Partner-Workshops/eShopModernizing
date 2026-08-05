namespace eShop.Catalog.Data.Seeding;

/// <summary>
/// Absolute locations of the seed assets. The legacy initializer resolved them from the ASP.NET
/// hosting environment's physical application path; here they come from the host content root.
/// </summary>
/// <param name="SetupFolder">Folder holding <c>CatalogBrands.csv</c>, <c>CatalogTypes.csv</c>, <c>CatalogItems.csv</c> and <c>CatalogItems.zip</c>.</param>
/// <param name="PicsFolder">Folder the catalog item pictures are served from.</param>
public sealed record CatalogSeedFolders(string SetupFolder, string PicsFolder)
{
    /// <summary>
    /// Resolves the configured folders against the content root, falling back to the folder the
    /// assembly was deployed to — the seed assets travel with the build output, which is not the
    /// content root when the host runs from its project directory.
    /// </summary>
    public static CatalogSeedFolders Resolve(string contentRootPath, string setupFolder, string picsFolder) =>
        new(ResolveFolder(contentRootPath, setupFolder), ResolveFolder(contentRootPath, picsFolder));

    private static string ResolveFolder(string contentRootPath, string relativeFolder)
    {
        if (Path.IsPathRooted(relativeFolder))
        {
            return relativeFolder;
        }

        var underContentRoot = Path.Combine(contentRootPath, relativeFolder);

        return Directory.Exists(underContentRoot)
            ? underContentRoot
            : Path.Combine(AppContext.BaseDirectory, relativeFolder);
    }
}
