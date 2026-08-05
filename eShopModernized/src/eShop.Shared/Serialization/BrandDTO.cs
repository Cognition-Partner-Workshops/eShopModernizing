namespace eShop.Shared.Serialization;

/// <summary>
/// Wire shape of a catalog brand as returned by <c>GET /api/files</c>.
/// Mirrors the legacy <c>FilesController.BrandDTO</c> (<c>Id</c>, <c>Brand</c>) so the JSON payload is
/// logically identical to the legacy binary-formatter stream: <c>[{"Id":1,"Brand":"Azure"}, …]</c>.
/// NET-67 (I-08) maps <c>CatalogBrand</c> onto this type; the property names must stay PascalCase.
/// </summary>
public sealed class BrandDTO
{
    /// <summary>Catalog brand identifier.</summary>
    public int Id { get; set; }

    /// <summary>Catalog brand name.</summary>
    public string? Brand { get; set; }
}
