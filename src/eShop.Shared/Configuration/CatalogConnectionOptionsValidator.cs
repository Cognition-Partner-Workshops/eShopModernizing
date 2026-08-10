using Microsoft.Extensions.Options;

namespace eShop.Shared.Configuration;

/// <summary>
/// A catalog connection string is required as soon as the host is not running on mock data,
/// mirroring the legacy behaviour where a missing <c>&lt;connectionStrings&gt;</c> entry only broke
/// the database-backed configurations.
/// </summary>
public sealed class CatalogConnectionOptionsValidator : IValidateOptions<CatalogConnectionOptions>
{
    private readonly IOptions<CatalogOptions> _catalogOptions;

    public CatalogConnectionOptionsValidator(IOptions<CatalogOptions> catalogOptions)
        => _catalogOptions = catalogOptions;

    public ValidateOptionsResult Validate(string? name, CatalogConnectionOptions options)
    {
        if (_catalogOptions.Value.UseMockData)
        {
            return ValidateOptionsResult.Success;
        }

        return string.IsNullOrWhiteSpace(options.Catalog)
            ? ValidateOptionsResult.Fail(
                "No catalog connection string configured. Set the 'ConnectionStrings__Catalog' " +
                "environment variable, or set 'Catalog:UseMockData' to true.")
            : ValidateOptionsResult.Success;
    }
}
