using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace eShop.Catalog.Api.Pictures;

/// <summary>Reads catalog item pictures from the configured pictures folder.</summary>
public sealed class CatalogPictureStore
{
    private static readonly Dictionary<string, string> MimeTypesByExtension = new(StringComparer.Ordinal)
    {
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".bmp"] = "image/bmp",
        [".tiff"] = "image/tiff",
        [".wmf"] = "image/wmf",
        [".jp2"] = "image/jp2",
        [".svg"] = "image/svg+xml",
    };

    private readonly string _rootPath;

    public CatalogPictureStore(IOptions<CatalogPictureOptions> options, IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);

        _rootPath = Path.IsPathRooted(options.Value.RootPath)
            ? options.Value.RootPath
            : Path.Combine(environment.ContentRootPath, options.Value.RootPath);
    }

    /// <summary>
    /// Returns the picture bytes and its content type, or <c>null</c> when the file does not exist.
    /// Unknown extensions fall back to <c>application/octet-stream</c>, as in the legacy controller.
    /// </summary>
    public CatalogPicture? Find(string pictureFileName)
    {
        if (string.IsNullOrWhiteSpace(pictureFileName))
        {
            return null;
        }

        var path = Path.Combine(_rootPath, Path.GetFileName(pictureFileName));
        if (!System.IO.File.Exists(path))
        {
            return null;
        }

        var extension = Path.GetExtension(pictureFileName);
        var contentType = MimeTypesByExtension.TryGetValue(extension, out var mimeType)
            ? mimeType
            : "application/octet-stream";

        return new CatalogPicture(System.IO.File.ReadAllBytes(path), contentType);
    }
}

public sealed record CatalogPicture(byte[] Content, string ContentType);
