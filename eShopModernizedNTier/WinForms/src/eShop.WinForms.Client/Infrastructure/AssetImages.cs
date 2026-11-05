using System.Drawing;

namespace eShop.WinForms.Client.Infrastructure;

/// <summary>
/// Loads the images that ship next to the executable under <c>Assets/</c>. The legacy client
/// embedded them through <c>Properties/Resources.resx</c>, which cannot be compiled outside
/// Windows (the ResX image entries go through System.Drawing at build time); loading them from
/// disk keeps the modernized project compiling on the Linux CI agent.
/// </summary>
public static class AssetImages
{
    public static string AssetsRoot { get; } = Path.Combine(AppContext.BaseDirectory, "Assets");

    public static string CatalogRoot { get; } = Path.Combine(AssetsRoot, "Catalog");

    public static Image Load(string fileName) => Image.FromFile(Path.Combine(AssetsRoot, fileName));

    /// <summary>
    /// Picture for a catalog item, resolved by the file name the service returns. Returns
    /// <see langword="null"/> when the file is not deployed, so a missing picture cannot take the
    /// whole grid down (the legacy client threw <see cref="FileNotFoundException"/>).
    /// </summary>
    public static Image? LoadCatalogPicture(string? pictureFileName)
    {
        if (string.IsNullOrWhiteSpace(pictureFileName))
        {
            return null;
        }

        var path = Path.Combine(CatalogRoot, pictureFileName);

        return File.Exists(path) ? Image.FromFile(path) : null;
    }
}
