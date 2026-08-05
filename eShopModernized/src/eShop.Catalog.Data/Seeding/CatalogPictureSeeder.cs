using System.IO.Compression;

namespace eShop.Catalog.Data.Seeding;

/// <summary>
/// Port of the legacy <c>CatalogDBInitializer.AddCatalogItemPictures</c>: when customization data
/// is enabled the pictures folder is emptied and repopulated from <c>Setup/CatalogItems.zip</c>.
/// Only files are removed (as in the legacy code), and the folder is a writable runtime location
/// rather than an image layer.
/// </summary>
internal static class CatalogPictureSeeder
{
    public const string CatalogItemPicturesFileName = "CatalogItems.zip";

    public static void ExtractCatalogItemPictures(CatalogSeedFolders folders)
    {
        var zipFileCatalogItemPictures = Path.Combine(folders.SetupFolder, CatalogItemPicturesFileName);
        if (!File.Exists(zipFileCatalogItemPictures))
        {
            return;
        }

        var picturePath = Directory.CreateDirectory(folders.PicsFolder);
        foreach (var file in picturePath.GetFiles())
        {
            file.Delete();
        }

        ZipFile.ExtractToDirectory(zipFileCatalogItemPictures, picturePath.FullName, overwriteFiles: true);
    }
}
